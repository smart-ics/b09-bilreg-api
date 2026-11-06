using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmMaintainReservationCmd(
    string ReservationId,
    string PlannedDate,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IInpatientOrderKey
{
    public string InpatientOrderId => ReservationId;
}

public class AdmMaintainReservationHandler : IRequestHandler<AdmMaintainReservationCmd>
{
    private readonly IInpatientOrderRepo _orderRepo;
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmMaintainReservationHandler(
        IInpatientOrderRepo orderRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _orderRepo = orderRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(AdmMaintainReservationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ReservationId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.PlannedDate);

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var order = _orderRepo.GetByKey(request.ReservationId)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        var snapshotJson = AuditLogSnapshotJson.Serialize(order);
        var plannedDate = request.PlannedDate.ToDate("yyyy-MM-dd");
        var occurredAt = _tglJamProvider.Now;
        var maintained = order.Maintain(plannedDate, kelas, bangsal, request.UserId, occurredAt);

        _orderRepo.SaveChanges(maintained);

        _auditRepo.SaveChanges(AuditLog.Create(
            maintained.AuditTrail.Modified,
            "UPDATE",
            nameof(InpatientOrderModel),
            maintained.InpatientOrderId,
            originalDataJson: snapshotJson));

        return Task.CompletedTask;
    }
}
