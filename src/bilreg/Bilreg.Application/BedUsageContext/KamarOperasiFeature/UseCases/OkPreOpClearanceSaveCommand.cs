using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record ChecklistItemInput(
    string ChecklistKey,
    ChecklistItemStatusEnum Status,
    string Notes = "");

public record PreOpChecklistItemDto(
    string ChecklistKey,
    ChecklistItemStatusEnum Status,
    string Notes);

public record OkPreOpClearanceSaveCommand(
    string OrderOpId,
    string UserId,
    IEnumerable<ChecklistItemInput> ChecklistItems,
    string VerifikatorPpaId,
    string DokterPjId,
    string? FinalStatus = null,
    string? DokterOverrideId = null,
    string? AlasanOverride = null,
    string? AlasanNotCleared = null
) : IRequest<OkPreOpClearanceSaveResponse>, IOrderOpKey;

public record OkPreOpClearanceSaveResponse(
    string ClearanceId,
    string ScheduleOpId,
    string OrderOpId,
    string RegId,
    int NoUrut,
    string ClearanceStatus,
    string OpCaseState,
    string VerifikatorPpaId,
    string DokterPjId,
    DateTime CompletedAt,
    string DokterOverrideId,
    string AlasanOverride,
    string AlasanNotCleared,
    IEnumerable<PreOpChecklistItemDto> ChecklistItems);

public class OkPreOpClearanceSaveHandler
    : IRequestHandler<OkPreOpClearanceSaveCommand, OkPreOpClearanceSaveResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IPreOpClearanceRepo _preOpClearanceRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public OkPreOpClearanceSaveHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IOpCaseRepo opCaseRepo,
        IPreOpClearanceRepo preOpClearanceRepo,
        IPpaRepo ppaRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _opCaseRepo = opCaseRepo;
        _preOpClearanceRepo = preOpClearanceRepo;
        _ppaRepo = ppaRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<OkPreOpClearanceSaveResponse> Handle(
        OkPreOpClearanceSaveCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var now = _tglJamProvider.Now;

        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        var scheduleOp = LoadScheduleOp(orderOp, opCase);

        ValidateScheduleAndLifecycle(scheduleOp, opCase, request.OrderOpId);

        var clearance = ResolveOrCreateClearance(scheduleOp, orderOp, request, now);
        var checklistItems = (request.ChecklistItems ?? [])
            .Select(x => new PreOpChecklistItemType(x.ChecklistKey, x.Status, x.Notes))
            .ToList();
        clearance.UpdateChecklist(checklistItems, request.UserId, now);

        var opCaseChanged = ApplyFinalStatus(clearance, opCase, scheduleOp, orderOp, request, now);

        using (var trans = TransHelper.NewScope())
        {
            _preOpClearanceRepo.SaveChanges(clearance);
            if (opCaseChanged)
                _opCaseRepo.SaveChanges(opCase);
            trans.Complete();
        }

        if (clearance.ClearanceStatus is PreOpClearanceStatusEnum.Cleared
            or PreOpClearanceStatusEnum.EmergencyOverridden)
        {
            await _mediator.Publish(
                new OkPreOpClearedEvent(clearance, now, orderOp.Pasien, scheduleOp.TglOp),
                cancellationToken);
        }

        return BuildResponse(clearance, opCase);
    }

    private ScheduleOpModel LoadScheduleOp(OrderOpModel orderOp, OpCaseModel opCase)
    {
        ScheduleOpModel? scheduleOp = null;

        if (opCase.ScheduleOp is not null &&
            !string.IsNullOrWhiteSpace(opCase.ScheduleOp.ScheduleOpId) &&
            opCase.ScheduleOp.ScheduleOpId != "-")
        {
            scheduleOp = _scheduleOpRepo
                .LoadEntity(ScheduleOpModel.Key(opCase.ScheduleOp.ScheduleOpId))
                .GetValueOrDefault();
        }

        if (scheduleOp is null)
        {
            var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList() ?? [];
            var scheduleWithOrderOpId = listSchedule
                .Where(x => !x.IsVoid)
                .FirstOrDefault(x => x.OrderOp.OrderOpId == orderOp.OrderOpId);

            if (scheduleWithOrderOpId is not null)
            {
                scheduleOp = _scheduleOpRepo
                    .LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId))
                    .GetValueOrDefault();
            }
        }

        return scheduleOp ?? throw new KeyNotFoundException(
            $"Jadwal operasi untuk Order Operasi ID {orderOp.OrderOpId} tidak ditemukan.");
    }

    private static void ValidateScheduleAndLifecycle(
        ScheduleOpModel scheduleOp,
        OpCaseModel opCase,
        string orderOpId)
    {
        // Guard EX-01: schedule must be official and not voided (check Voided.IsEmpty for SQL-hydrated models)
        if (scheduleOp.IsDraft)
            throw new InvalidOperationException(
                "Pre-Operative Clearance tidak dapat diproses karena jadwal operasi masih berstatus DRAFT. Konfirmasi jadwal secara resmi terlebih dahulu.");

        if (scheduleOp.AuditTrail.IsVoided || !scheduleOp.AuditTrail.Voided.IsEmpty)
            throw new InvalidOperationException(
                "Pre-Operative Clearance tidak dapat diproses karena jadwal operasi telah dibatalkan/void.");

        // Guard EX-06 & Lifecycle
        if (opCase.OrderOpState == OpCaseStateEnum.Cancelled)
            throw new InvalidOperationException($"Order Operasi ID {orderOpId} telah dibatalkan.");

        if (opCase.OrderOpState >= OpCaseStateEnum.OpStarted)
            throw new InvalidOperationException(
                $"Pre-Operative Clearance tidak dapat diubah karena tindakan operasi sudah berjalan atau selesai (Status: {opCase.OrderOpState}).");

        if (opCase.OrderOpState != OpCaseStateEnum.Scheduled)
            throw new InvalidOperationException(
                $"Pre-Operative Clearance hanya dapat dilakukan pada kasus berstatus Scheduled. Status saat ini: {opCase.OrderOpState}.");
    }

    private PreOpClearanceModel ResolveOrCreateClearance(
        ScheduleOpModel scheduleOp,
        OrderOpModel orderOp,
        OkPreOpClearanceSaveCommand request,
        DateTime now)
    {
        var latestOpt = _preOpClearanceRepo.GetLatestByScheduleOpId(scheduleOp.ScheduleOpId);
        if (latestOpt.HasValue && latestOpt.Value.ClearanceStatus == PreOpClearanceStatusEnum.PendingClearance)
            return latestOpt.Value;

        if (!latestOpt.HasValue || latestOpt.Value.ClearanceStatus == PreOpClearanceStatusEnum.NotCleared)
        {
            var nextNoUrut = _preOpClearanceRepo.CountByScheduleOpId(scheduleOp.ScheduleOpId) + 1;
            return PreOpClearanceModel.Create(
                scheduleOp.ScheduleOpId,
                orderOp.OrderOpId,
                orderOp.Reg.RegId,
                nextNoUrut,
                request.VerifikatorPpaId,
                request.DokterPjId,
                request.UserId,
                now);
        }

        throw new InvalidOperationException(
            $"Clearance untuk jadwal {scheduleOp.ScheduleOpId} sudah berstatus final ({latestOpt.Value.ClearanceStatus}).");
    }

    private bool ApplyFinalStatus(
        PreOpClearanceModel clearance,
        OpCaseModel opCase,
        ScheduleOpModel scheduleOp,
        OrderOpModel orderOp,
        OkPreOpClearanceSaveCommand request,
        DateTime now)
    {
        var normalizedStatus = string.IsNullOrWhiteSpace(request.FinalStatus)
            ? "PENDING_CLEARANCE"
            : request.FinalStatus.Trim().ToUpperInvariant();

        switch (normalizedStatus)
        {
            case "PENDING_CLEARANCE":
                return false;

            case "CLEARED":
                clearance.Approve(request.UserId, now);
                opCase.PreOpClear(request.UserId, now);
                return true;

            case "NOT_CLEARED":
                clearance.Reject(request.AlasanNotCleared ?? string.Empty, request.UserId, now);
                return false;

            case "EMERGENCY_OVERRIDDEN":
                ExecuteEmergencyOverride(clearance, scheduleOp, orderOp, request, now);
                opCase.PreOpClear(request.UserId, now);
                return true;

            default:
                throw new ArgumentException(
                    $"FinalStatus '{request.FinalStatus}' tidak dikenal. Nilai yang valid: PENDING_CLEARANCE, CLEARED, NOT_CLEARED, EMERGENCY_OVERRIDDEN.",
                    nameof(request.FinalStatus));
        }
    }

    private void ExecuteEmergencyOverride(
        PreOpClearanceModel clearance,
        ScheduleOpModel scheduleOp,
        OrderOpModel orderOp,
        OkPreOpClearanceSaveCommand request,
        DateTime now)
    {
        if (string.IsNullOrWhiteSpace(request.DokterOverrideId))
            throw new UnauthorizedAccessException("DokterOverrideId wajib diisi untuk emergency override.");

        var dokterOverrideId = request.DokterOverrideId.Trim();
        var ppaOpt = _ppaRepo.LoadEntity(PpaType.Key(dokterOverrideId));
        if (!ppaOpt.HasValue || !ppaOpt.Value.IsDokter())
            throw new UnauthorizedAccessException(
                "Hanya PPA Dokter yang terdaftar dan valid yang berwenang melakukan emergency override.");

        var authorizedDokterPpaIds = BuildAuthorizedDokterPpaIds(scheduleOp);
        clearance.EmergencyOverride(
            dokterOverrideId,
            request.AlasanOverride ?? string.Empty,
            request.UserId,
            now,
            orderOp.UrgencyLevel,
            authorizedDokterPpaIds);
    }

    private static HashSet<string> BuildAuthorizedDokterPpaIds(ScheduleOpModel scheduleOp)
    {
        var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (!string.IsNullOrWhiteSpace(scheduleOp.TeamLead?.PpaId) && scheduleOp.TeamLead.PpaId != "-")
            result.Add(scheduleOp.TeamLead.PpaId.Trim());

        if (scheduleOp.ListPpa is not null)
        {
            foreach (var member in scheduleOp.ListPpa)
            {
                if (member.Profesi?.ProfesiId == ProfesiType.Dokter.ProfesiId &&
                    !string.IsNullOrWhiteSpace(member.Ppa?.PpaId) &&
                    member.Ppa.PpaId != "-")
                {
                    result.Add(member.Ppa.PpaId.Trim());
                }
            }
        }

        return result;
    }

    private static OkPreOpClearanceSaveResponse BuildResponse(
        PreOpClearanceModel clearance,
        OpCaseModel opCase)
    {
        return new OkPreOpClearanceSaveResponse(
            ClearanceId: clearance.ClearanceId,
            ScheduleOpId: clearance.ScheduleOpId,
            OrderOpId: clearance.OrderOpId,
            RegId: clearance.RegId,
            NoUrut: clearance.NoUrut,
            ClearanceStatus: FormatClearanceStatus(clearance.ClearanceStatus),
            OpCaseState: opCase.OrderOpState.ToString(),
            VerifikatorPpaId: clearance.VerifikatorPpaId,
            DokterPjId: clearance.DokterPjId,
            CompletedAt: clearance.CompletedAt,
            DokterOverrideId: clearance.DokterOverrideId,
            AlasanOverride: clearance.AlasanOverride,
            AlasanNotCleared: clearance.AlasanNotCleared,
            ChecklistItems: clearance.ChecklistItems
                .Select(x => new PreOpChecklistItemDto(x.ChecklistKey, x.Status, x.Notes))
                .ToList());
    }

    internal static string FormatClearanceStatus(PreOpClearanceStatusEnum status) => status switch
    {
        PreOpClearanceStatusEnum.PendingClearance => "PENDING_CLEARANCE",
        PreOpClearanceStatusEnum.Cleared => "CLEARED",
        PreOpClearanceStatusEnum.NotCleared => "NOT_CLEARED",
        PreOpClearanceStatusEnum.EmergencyOverridden => "EMERGENCY_OVERRIDDEN",
        _ => status.ToString()
    };
}
