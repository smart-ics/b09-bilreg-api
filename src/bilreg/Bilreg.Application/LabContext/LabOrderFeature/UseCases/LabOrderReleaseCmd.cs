using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.LabContext.LabResultFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.LabContext.LabResultFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderReleaseCmd(string OrderId, string UserId, string ReleaseNote)
    : IRequest<LabOrderReleaseResponse>, ILabOrderKey;

public record LabOrderReleaseResponse(
    bool Released,
    string BillingStatus,
    string Message);

public class LabOrderReleaseHandler : IRequestHandler<LabOrderReleaseCmd, LabOrderReleaseResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabBillingIntegration _labBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILabResultDocumentRepo? _labResultDocumentRepo;

    public LabOrderReleaseHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider)
        : this(labOrderRepo, labBillingIntegration, tglJamProvider, null)
    {
    }

    public LabOrderReleaseHandler(
        ILabOrderRepo labOrderRepo,
        ILabBillingIntegration labBillingIntegration,
        ITglJamProvider tglJamProvider,
        ILabResultDocumentRepo? labResultDocumentRepo)
    {
        _labOrderRepo = labOrderRepo;
        _labBillingIntegration = labBillingIntegration;
        _tglJamProvider = tglJamProvider;
        _labResultDocumentRepo = labResultDocumentRepo;
    }

    public Task<LabOrderReleaseResponse> Handle(LabOrderReleaseCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.Null(request.ReleaseNote, nameof(request.ReleaseNote));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        var occurredAt = _tglJamProvider.Now;

        var validation = _labBillingIntegration.ValidateReleaseEligibility(
            new LabBillingReleaseValidationRequest(
                order.OrderId,
                order.OrderNo,
                order.BillingTindakanId));

        var traceStatus = validation.Code switch
        {
            BillingReleaseValidationCode.Clear => BillingReleaseValidationStatusEnum.Clear,
            BillingReleaseValidationCode.Blocked => BillingReleaseValidationStatusEnum.Blocked,
            _ => throw new InvalidOperationException($"Unknown billing validation code: {validation.Code}")
        };

        order.RecordLastBillingReleaseValidation(traceStatus, validation.Message, request.UserId, occurredAt);

        LabOrderReleaseResponse response;
        if (validation.Code == BillingReleaseValidationCode.Blocked)
        {
            using (var trans = TransHelper.NewScope())
            {
                _labOrderRepo.SaveChanges(order);
                trans.Complete();
            }

            response = new LabOrderReleaseResponse(
                Released: false,
                BillingStatus: "BLOCKED",
                Message: validation.Message);
            return Task.FromResult(response);
        }

        order.Release(request.UserId, request.ReleaseNote, occurredAt);

        LabResultDocumentModel? resultDoc = null;
        if (_labResultDocumentRepo is not null)
        {
            var maybeResult = _labResultDocumentRepo.LoadByOrderId(order.OrderId);
            if (maybeResult.HasValue)
            {
                var doc = maybeResult.Value;
                if (doc.ResultStatus == LabResultStatusEnum.Verified)
                {
                    doc.MarkReleased(request.UserId, occurredAt);
                    resultDoc = doc;
                }
            }
        }

        using (var releaseTrans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            if (resultDoc is not null)
            {
                _labResultDocumentRepo?.SaveChanges(resultDoc);
            }
            releaseTrans.Complete();
        }

        response = new LabOrderReleaseResponse(
            Released: true,
            BillingStatus: "CLEAR",
            Message: validation.Message);

        return Task.FromResult(response);
    }
}
