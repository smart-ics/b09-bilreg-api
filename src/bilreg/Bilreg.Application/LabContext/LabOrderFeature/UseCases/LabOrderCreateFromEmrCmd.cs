using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCreateFromEmrCmd(
    string UserId,
    string EmrOrderId,
    string RegId,
    string PatientId,
    string PatientName,
    string BirthDateYmd,
    string Gender,
    List<LabOrderTarifItemInput> Items)
    : IRequest<LabOrderCreateFromEmrResponse>;

public record LabOrderCreateFromEmrResponse(
    string EmrOrderId,
    int LabOrderStatus,
    string OrderNo);

public class LabOrderCreateFromEmrHandler : IRequestHandler<LabOrderCreateFromEmrCmd, LabOrderCreateFromEmrResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ILabTestResolutionService _resolutionService;

    public LabOrderCreateFromEmrHandler(
        ILabOrderRepo labOrderRepo,
        ISequencer sequencer,
        ILabTestResolutionService resolutionService)
    {
        _labOrderRepo = labOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
    }

    public Task<LabOrderCreateFromEmrResponse> Handle(LabOrderCreateFromEmrCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.EmrOrderId, nameof(request.EmrOrderId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.PatientId, nameof(request.PatientId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        var snapshot = LabOrderCreateHelper.BuildSnapshot(
            request.RegId,
            request.PatientId,
            request.PatientName,
            request.BirthDateYmd,
            request.Gender,
            audit.Timestamp);
        var lines = LabOrderCreateHelper.MapResolvedLines(_resolutionService.ResolveByTarifItems(request.Items));
        var orderNo = LabOrderCreateHelper.NextOrderNo(_sequencer);

        var order = LabOrderModel.CreateFromEmr(request.EmrOrderId, snapshot, lines, orderNo, audit);

        LabOrderCreateFromEmrResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderCreateFromEmrResponse(
                order.EmrOrderId,
                (int)order.LabOrderStatus,
                order.OrderNo);
        }

        return Task.FromResult(response);
    }
}
