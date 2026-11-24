using System.Text.Json;
using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmGetReservationHistoryQry(string ReservationId)
    : IRequest<AdmReservationHistoryResponse>, IInpatientOrderKey
{
    public string InpatientOrderId => ReservationId;
}

public record AdmReservationHistoryResponse(
    string ReservationId,
    string PasienId,
    string PasienName,
    DateTime CurrentPlannedDate,
    string CurrentKelasName,
    string CurrentBangsalName,
    int OrderStatus,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    IReadOnlyList<AdmReservationHistoryEntry> Entries);

public record AdmReservationHistoryEntry(
    DateTime EventTime,
    string UserId,
    string ActionType,
    DateTime? PreviousPlannedDate,
    DateTime? NewPlannedDate,
    string Notes);

public class AdmGetReservationHistoryHandler : IRequestHandler<AdmGetReservationHistoryQry, AdmReservationHistoryResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmGetReservationHistoryHandler(IInpatientOrderRepo orderRepo)
    {
        _orderRepo = orderRepo;
    }

    public Task<AdmReservationHistoryResponse> Handle(
        AdmGetReservationHistoryQry request,
        CancellationToken cancellationToken)
    {
        var order = _orderRepo.GetByKey(request.ReservationId)
            .GetValueOrThrow($"Reservation '{request.ReservationId}' tidak ditemukan.");

        var audits = _orderRepo.ListAudits(request.ReservationId).ToList();
        var entries = new List<AdmReservationHistoryEntry>();

        // 1. Initial creation entry
        entries.Add(new AdmReservationHistoryEntry(
            order.AuditTrail.Created.Timestamp,
            order.AuditTrail.Created.UserId,
            "CREATE",
            null,
            order.PlannedDate,
            "Reservasi dibuat"));

        // 2. Audit log modifications
        foreach (var audit in audits)
        {
            DateTime? prevDate = null;
            if (!string.IsNullOrWhiteSpace(audit.OriginalDataJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(audit.OriginalDataJson);
                    if (doc.RootElement.TryGetProperty("PlannedDate", out var plannedDateEl) &&
                        plannedDateEl.TryGetDateTime(out var parsedDate))
                    {
                        prevDate = parsedDate;
                    }
                }
                catch
                {
                    // Ignore JSON parse error from legacy data
                }
            }

            var notes = string.IsNullOrWhiteSpace(audit.Reason)
                ? (audit.ActionType == "UPDATE" ? "Reservasi dijadwalkan ulang" : audit.ActionType)
                : audit.Reason;

            entries.Add(new AdmReservationHistoryEntry(
                audit.EventTime,
                audit.UserId,
                audit.ActionType,
                prevDate,
                order.PlannedDate,
                notes));
        }

        // 3. Fallback: If OrderStatus is Maintained but no audit log entry recorded yet (e.g. simulated/manual record)
        if (order.OrderStatus == InpatientOrderStatusEnum.Maintained && audits.Count == 0)
        {
            entries.Add(new AdmReservationHistoryEntry(
                order.AuditTrail.Modified.Timestamp,
                order.AuditTrail.Modified.UserId,
                "UPDATE",
                null,
                order.PlannedDate,
                "Reservasi dijadwalkan ulang"));
        }

        var response = new AdmReservationHistoryResponse(
            order.InpatientOrderId,
            order.Pasien?.PasienId ?? "-",
            order.Pasien?.PasienName ?? "-",
            order.PlannedDate,
            order.KelasRawat?.KelasName ?? "-",
            order.Bangsal?.BangsalName ?? "-",
            (int)order.OrderStatus,
            order.AuditTrail.Created.UserId,
            order.AuditTrail.Created.Timestamp,
            order.AuditTrail.Modified.UserId,
            order.AuditTrail.Modified.Timestamp,
            entries.OrderBy(e => e.EventTime).ToList());

        return Task.FromResult(response);
    }
}
