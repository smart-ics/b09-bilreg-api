using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkPreOpClearanceGetQuery(
    string? OrderOpId = null,
    string? ScheduleOpId = null) : IRequest<OkPreOpClearanceGetResponse>;

public record OkPreOpClearanceGetResponse(
    string ClearanceId,
    string ScheduleOpId,
    string OrderOpId,
    string RegId,
    int NoUrut,
    string ClearanceStatus,
    string VerifikatorPpaId,
    string DokterPjId,
    DateTime CompletedAt,
    string DokterOverrideId,
    string AlasanOverride,
    string AlasanNotCleared,
    IEnumerable<PreOpChecklistItemDto> ChecklistItems);

public class OkPreOpClearanceGetHandler
    : IRequestHandler<OkPreOpClearanceGetQuery, OkPreOpClearanceGetResponse>
{
    private readonly IPreOpClearanceRepo _preOpClearanceRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IScheduleOpRepo _scheduleOpRepo;

    public OkPreOpClearanceGetHandler(
        IPreOpClearanceRepo preOpClearanceRepo,
        IOpCaseRepo opCaseRepo,
        IScheduleOpRepo scheduleOpRepo)
    {
        _preOpClearanceRepo = preOpClearanceRepo;
        _opCaseRepo = opCaseRepo;
        _scheduleOpRepo = scheduleOpRepo;
    }

    public Task<OkPreOpClearanceGetResponse> Handle(
        OkPreOpClearanceGetQuery request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.OrderOpId) &&
            string.IsNullOrWhiteSpace(request.ScheduleOpId))
        {
            throw new ArgumentException("OrderOpId atau ScheduleOpId wajib diisi.");
        }

        var scheduleOpId = ResolveScheduleOpId(request);

        var clearance = _preOpClearanceRepo.GetLatestByScheduleOpId(scheduleOpId)
            .GetValueOrThrow($"Catatan Pre-Operative Clearance untuk ScheduleOp ID {scheduleOpId} tidak ditemukan.");

        var response = new OkPreOpClearanceGetResponse(
            ClearanceId: clearance.ClearanceId,
            ScheduleOpId: clearance.ScheduleOpId,
            OrderOpId: clearance.OrderOpId,
            RegId: clearance.RegId,
            NoUrut: clearance.NoUrut,
            ClearanceStatus: OkPreOpClearanceSaveHandler.FormatClearanceStatus(clearance.ClearanceStatus),
            VerifikatorPpaId: clearance.VerifikatorPpaId,
            DokterPjId: clearance.DokterPjId,
            CompletedAt: clearance.CompletedAt,
            DokterOverrideId: clearance.DokterOverrideId,
            AlasanOverride: clearance.AlasanOverride,
            AlasanNotCleared: clearance.AlasanNotCleared,
            ChecklistItems: clearance.ChecklistItems
                .Select(x => new PreOpChecklistItemDto(x.ChecklistKey, x.Status, x.Notes))
                .ToList());

        return Task.FromResult(response);
    }

    private string ResolveScheduleOpId(OkPreOpClearanceGetQuery request)
    {
        if (!string.IsNullOrWhiteSpace(request.ScheduleOpId))
            return request.ScheduleOpId.Trim();

        var orderOpId = request.OrderOpId!.Trim();
        var opCase = _opCaseRepo.LoadEntity(OrderOpModel.Key(orderOpId)).GetValueOrDefault();

        if (opCase?.ScheduleOp is not null &&
            !string.IsNullOrWhiteSpace(opCase.ScheduleOp.ScheduleOpId) &&
            opCase.ScheduleOp.ScheduleOpId != "-")
        {
            return opCase.ScheduleOp.ScheduleOpId;
        }

        if (opCase?.Pasien is not null &&
            !string.IsNullOrWhiteSpace(opCase.Pasien.PasienId) &&
            opCase.Pasien.PasienId != "-")
        {
            var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(opCase.Pasien.PasienId))?.ToList() ?? [];
            var match = listSchedule
                .Where(x => !x.IsVoid)
                .FirstOrDefault(x => x.OrderOp.OrderOpId == orderOpId);

            if (match is not null && !string.IsNullOrWhiteSpace(match.ScheduleOpId))
                return match.ScheduleOpId;
        }

        throw new KeyNotFoundException($"Schedule Operasi untuk Order Operasi ID {orderOpId} tidak ditemukan.");
    }
}
