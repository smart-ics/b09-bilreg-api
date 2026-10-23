using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkScheduleOpSetCommand(
    string OrderOpId,
    string KamarId,
    string Tgl,
    string Jam,
    int Durasi,
    string UserId,
    string DokterOperatorId = "",
    int TurnoverBuffer = 0,
    string AlasanPerubahan = "",
    bool IsOverride = false,
    string OverrideReason = "") : IRequest<OkScheduleOpSetResponse>, IOrderOpKey;

public record OkScheduleOpSetResponse(string ScheduleOpId);

public class OkScheduleOpSetCommandHandler : IRequestHandler<OkScheduleOpSetCommand, OkScheduleOpSetResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpSetCommandHandler(
        IScheduleOpRepo scheduleOpRepo,
        IOrderOpRepo orderOpRepo,
        IKamarRepo kamarRepo,
        IPpaRepo ppaRepo,
        IOpCaseRepo opCaseRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _scheduleOpRepo = scheduleOpRepo;
        _orderOpRepo = orderOpRepo;
        _kamarRepo = kamarRepo;
        _ppaRepo = ppaRepo;
        _opCaseRepo = opCaseRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OkScheduleOpSetResponse> Handle(OkScheduleOpSetCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));
        Guard.Against.NullOrWhiteSpace(request.Jam, nameof(request.Jam));
        Guard.Against.NegativeOrZero(request.Durasi, nameof(request.Durasi));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        // 1. Validasi OrderOp aktif
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        if (orderOp.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Order Operasi ID {request.OrderOpId} sudah dibatalkan/void.");

        // 2. Validasi kamar fisik aktif
        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        // Validasi OpCase
        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        if (opCase.OrderOpState == OpCaseStateEnum.Cancelled)
            throw new InvalidOperationException($"Order Operasi ID {request.OrderOpId} telah dibatalkan.");

        if ((int)opCase.OrderOpState >= (int)OpCaseStateEnum.OpStarted)
            throw new InvalidOperationException($"Status Operasi ({opCase.OrderOpState}) tidak dapat dijadwalkan ulang karena operasi telah dimulai atau selesai.");

        // 3. Deteksi Jadwal Eksisting
        ScheduleOpModel? existingSchedule = null;
        if (opCase.ScheduleOp != null && !string.IsNullOrWhiteSpace(opCase.ScheduleOp.ScheduleOpId) && opCase.ScheduleOp.ScheduleOpId != "-")
        {
            existingSchedule = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(opCase.ScheduleOp.ScheduleOpId)).GetValueOrDefault();
        }

        if (existingSchedule == null)
        {
            var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(orderOp.Pasien.PasienId))?.ToList() ?? [];
            var scheduleWithOrderOpId = listSchedule
                .Where(x => !x.IsVoid)
                .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
            if (scheduleWithOrderOpId != null)
            {
                existingSchedule = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId)).GetValueOrDefault();
            }
        }

        if (existingSchedule != null && existingSchedule.AuditTrail.IsVoided)
        {
            existingSchedule = null;
        }

        if (existingSchedule != null)
        {
            Guard.Against.NullOrWhiteSpace(request.AlasanPerubahan, nameof(request.AlasanPerubahan),
                "Alasan perubahan jadwal wajib diisi saat reschedule.");
        }

        // 4. Resolusi Dokter Operator Utama (3-tier fallback)
        string? dokterOperatorId = null;
        if (!string.IsNullOrWhiteSpace(request.DokterOperatorId))
        {
            dokterOperatorId = request.DokterOperatorId;
        }
        else if (existingSchedule != null && !string.IsNullOrWhiteSpace(existingSchedule.TeamLead?.PpaId) && existingSchedule.TeamLead.PpaId != "-")
        {
            dokterOperatorId = existingSchedule.TeamLead.PpaId;
        }
        else
        {
            dokterOperatorId = orderOp.DokterOperator?.PpaId;
        }

        if (string.IsNullOrWhiteSpace(dokterOperatorId) || dokterOperatorId == "-")
            throw new ArgumentException("Dokter Operator Utama wajib ditentukan saat penjadwalan kamar operasi.");

        var ppa = _ppaRepo.LoadEntity(PpaType.Key(dokterOperatorId))
            .GetValueOrThrow($"Dokter Operator dengan ID {dokterOperatorId} tidak ditemukan.");

        var isDokter = ppa.IsDokter() || (ppa.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi == ProfesiType.Dokter);
        if (!isDokter)
            throw new ArgumentException("PPA yang ditugaskan sebagai Operator Utama harus berprofesi Dokter.");

        // 5. Perhitungan Waktu
        DateTime start = DateTime.ParseExact($"{request.Tgl} {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        DateTime end = start.AddMinutes(request.Durasi);
        int effectiveTurnoverBuffer = request.TurnoverBuffer > 0 ? request.TurnoverBuffer : 30;

        // 6. Hard Block Check (Zero Room Collision)
        var activeSchedulesInRoom = _scheduleOpRepo.ListActiveSchedulesInRoom(request.KamarId, start.Date)?.ToList() ?? [];

        foreach (var s in activeSchedulesInRoom)
        {
            if (s.IsVoid) continue;
            if (existingSchedule != null && s.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

            var startB = s.TglOp;
            var endB = s.TglOp.AddMinutes(s.Durasi);

            if (start < endB && end > startB)
            {
                throw new ScheduleCollisionException(
                    $"Bentrok jadwal kamar operasi: Kamar {request.KamarId} telah terisi oleh jadwal lain dari jam {startB:HH:mm} s/d {endB:HH:mm}.");
            }
        }

        // 7. Soft Warning Checks
        var warnings = new List<string>();

        // Bentrok Dokter / PPA di Kamar Lain (Jalur Lead & Anggota Tim)
        var activeDoctorSchedules = _scheduleOpRepo.ListActiveSchedulesForDoctor(ppa.PpaId, start.Date)?.ToList() ?? [];
        var activeTeamSchedules = _scheduleOpRepo.ListActiveSchedulesForPpaInTeam(ppa.PpaId, start.Date)?.ToList() ?? [];

        var combinedDoctorSchedules = activeDoctorSchedules
            .Concat(activeTeamSchedules)
            .GroupBy(x => x.ScheduleOpId)
            .Select(g => g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.ProfesiName)).First())
            .ToList();

        foreach (var d in combinedDoctorSchedules)
        {
            if (d.IsVoid) continue;
            if (existingSchedule != null && d.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

            var startD = d.TglOp;
            var endD = d.TglOp.AddMinutes(d.Durasi);

            if (start < endD && end > startD)
            {
                var roomDisplay = !string.IsNullOrWhiteSpace(d.Kamar?.KamarId) ? d.Kamar.KamarId : request.KamarId;
                var roleInfo = !string.IsNullOrWhiteSpace(d.ProfesiName) ? $" sebagai {d.ProfesiName}" : "";
                warnings.Add($"Dokter/PPA {ppa.PpaName}{roleInfo} telah terjadwal pada operasi lain ({roomDisplay}) dari jam {startD:HH:mm} s/d {endD:HH:mm}.");
            }
        }

        // Pelanggaran Turnover Buffer Sterilisasi
        foreach (var r in activeSchedulesInRoom)
        {
            if (r.IsVoid) continue;
            if (existingSchedule != null && r.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

            var startR = r.TglOp;
            var endR = r.TglOp.AddMinutes(r.Durasi);

            if (endR <= start && start < endR.AddMinutes(effectiveTurnoverBuffer))
            {
                warnings.Add($"Jeda sterilisasi (turnover buffer {effectiveTurnoverBuffer} menit) tidak terpenuhi setelah jadwal sebelumnya yang berakhir pada {endR:HH:mm}.");
            }

            if (startR >= end && startR < end.AddMinutes(effectiveTurnoverBuffer))
            {
                warnings.Add($"Jeda sterilisasi (turnover buffer {effectiveTurnoverBuffer} menit) tidak terpenuhi sebelum jadwal berikutnya yang dimulai pada {startR:HH:mm}.");
            }
        }

        if (warnings.Count > 0 && !request.IsOverride)
        {
            throw new ScheduleWarningException(warnings);
        }

        if (warnings.Count > 0 && request.IsOverride)
        {
            Guard.Against.NullOrWhiteSpace(request.OverrideReason, nameof(request.OverrideReason),
                "Alasan diskresi override wajib diisi jika terdapat peringatan bentrok dokter/turnover buffer.");
        }

        // 8. Eksekusi Mutasi
        ScheduleOpModel scheduleOp;
        if (existingSchedule != null)
        {
            existingSchedule.Reschedule(
                start,
                kamar.ToReff(),
                request.Durasi,
                effectiveTurnoverBuffer,
                ppa.ToReff(),
                request.AlasanPerubahan,
                request.IsOverride,
                request.OverrideReason,
                request.UserId,
                occurredAt);
            scheduleOp = existingSchedule;
        }
        else
        {
            scheduleOp = ScheduleOpModel.CreateFromOrder(orderOp, request.UserId, kamar, ppa, start, occurredAt);
            scheduleOp.SetSchedule(start, kamar.ToReff(), request.Durasi, request.UserId, occurredAt, effectiveTurnoverBuffer);
        }

        // 9. Sinkronisasi Kasus Operasi
        opCase.Schedule(scheduleOp, occurredAt);

        // 10. Transaksi Atomik
        using (var trans = TransHelper.NewScope())
        {
            orderOp.SetDokterOperator(ppa.ToReff());
            orderOp.AuditTrail.Modif(request.UserId, occurredAt);
            _scheduleOpRepo.SaveChanges(scheduleOp);
            _opCaseRepo.SaveChanges(opCase);
            _orderOpRepo.SaveChanges(orderOp);
            trans.Complete();
        }

        // 11. Publikasi Event
        var domainEvent = new OkScheduleOpSetEvent(request, scheduleOp);
        _mediator.Publish(domainEvent, cancellationToken);

        return Task.FromResult(new OkScheduleOpSetResponse(scheduleOp.ScheduleOpId));
    }
}

public record OkScheduleOpSetEvent(OkScheduleOpSetCommand Command, ScheduleOpModel Aggregate) : INotification;
