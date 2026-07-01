using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCreateExternalPatientCmd(
    string UserId,
    string? EmrOrderId,
    string? RegId,
    string? PatientId,
    string PatientName,
    string? BirthDateYmd,
    string? Gender,
    List<LabOrderTarifItemInput> Items)
    : IRequest<LabOrderCreateResponse>;

public record LabOrderCreateResponse(string OrderId, string OrderNo);

public class LabOrderCreateExternalPatientHandler
    : IRequestHandler<LabOrderCreateExternalPatientCmd, LabOrderCreateResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ILabTestResolutionService _resolutionService;

    public LabOrderCreateExternalPatientHandler(
        ILabOrderRepo labOrderRepo,
        ISequencer sequencer,
        ILabTestResolutionService resolutionService)
    {
        _labOrderRepo = labOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
    }

    public Task<LabOrderCreateResponse> Handle(
        LabOrderCreateExternalPatientCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var audit = new AuditInfoType(request.UserId, DateTime.Now);
        var snapshot = LabOrderCreateHelper.BuildSnapshot(
            request.RegId ?? "",
            request.PatientId ?? "",
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
