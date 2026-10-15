using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCreateExternalPatientCmd(
    string UserId,
    string? EmrOrderId,
    string? RegId,
    string? PatientId,
    string PatientName,
    string? BirthDateYmd,
    string? Gender,
    List<LabOrderTarifItemInput> Items,
    string? RegExternalId = null,
    string? ExternalMrNo = null)
    : IRequest<LabOrderCreateResponse>
{
    public string EffectiveRegId => !string.IsNullOrWhiteSpace(RegExternalId) ? RegExternalId : (RegId ?? string.Empty);
    public string EffectivePatientId => !string.IsNullOrWhiteSpace(ExternalMrNo) ? ExternalMrNo : (PatientId ?? string.Empty);
}

public record LabOrderCreateResponse(string OrderId, string OrderNo);

public class LabOrderCreateExternalPatientHandler
    : IRequestHandler<LabOrderCreateExternalPatientCmd, LabOrderCreateResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ILabTestResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderCreateExternalPatientHandler(
        ILabOrderRepo labOrderRepo,
        ISequencer sequencer,
        ILabTestResolutionService resolutionService,
        ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabOrderCreateResponse> Handle(
        LabOrderCreateExternalPatientCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var regId = request.EffectiveRegId;
        var patientId = request.EffectivePatientId;

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var snapshot = LabOrderCreateHelper.BuildSnapshot(
            regId,
            patientId,
            request.PatientName,
            request.BirthDateYmd ?? "",
            request.Gender ?? "",
            audit.Timestamp);
        var lines = LabOrderCreateHelper.MapResolvedLines(_resolutionService.ResolveByTarifItems(request.Items));
        var orderNo = LabOrderCreateHelper.NextOrderNo(_sequencer);

        var order = LabOrderModel.CreateExternal(request.EmrOrderId, snapshot, lines, orderNo, audit);

        LabOrderCreateResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderCreateResponse(order.OrderId, order.OrderNo);
        }

        return Task.FromResult(response);
    }
}
