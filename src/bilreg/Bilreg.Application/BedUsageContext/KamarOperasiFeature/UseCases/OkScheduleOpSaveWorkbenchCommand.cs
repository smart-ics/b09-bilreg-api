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

public record OkScheduleOpSaveWorkbenchCommand(
    string OrderOpId,
    string KamarId,
    string Tgl,
    string Jam,
    int Durasi,
    string UserId,
    string DokterOperatorId = "",
    List<string>? ListPpaIds = null,
    int TurnoverBuffer = 30,
    string AlasanReschedule = "",
    string AlasanAssignLeader = "",
    bool IsOverride = false,
    string OverrideReason = "",
    bool Publish = false
) : IRequest<OkScheduleOpSaveWorkbenchResponse>, IOrderOpKey;

public record OkScheduleOpSaveWorkbenchResponse(
    string ScheduleOpId,
    bool IsDraft,
    bool Rescheduled,
    bool LeaderChanged,
    bool PpaChanged,
    bool Published
);

public class OkScheduleOpSaveWorkbenchHandler : IRequestHandler<OkScheduleOpSaveWorkbenchCommand, OkScheduleOpSaveWorkbenchResponse>
{
    private readonly IScheduleOpRepo _scheduleOpRepo;
    private readonly IOrderOpRepo _orderOpRepo;
    private readonly IKamarRepo _kamarRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOpCaseRepo _opCaseRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public OkScheduleOpSaveWorkbenchHandler(
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

    public async Task<OkScheduleOpSaveWorkbenchResponse> Handle(OkScheduleOpSaveWorkbenchCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;

        // 1. Validasi format input dasar
        Guard.Against.NullOrWhiteSpace(request.OrderOpId, nameof(request.OrderOpId));
        Guard.Against.NullOrWhiteSpace(request.KamarId, nameof(request.KamarId));
        Guard.Against.InvalidDateFormat(request.Tgl, nameof(request.Tgl));
        Guard.Against.NullOrWhiteSpace(request.Jam, nameof(request.Jam));
        Guard.Against.NegativeOrZero(request.Durasi, nameof(request.Durasi));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        // 2. Validasi OrderOp aktif
        var orderOp = _orderOpRepo.LoadEntity(OrderOpModel.Key(request.OrderOpId))
            .GetValueOrThrow($"Order Operasi ID {request.OrderOpId} tidak ditemukan.");

        if (orderOp.AuditTrail.IsVoided)
            throw new InvalidOperationException($"Order Operasi ID {request.OrderOpId} sudah dibatalkan/void.");

        // 3. Validasi Kamar fisik
        var kamar = _kamarRepo.LoadEntity(KamarType.Key(request.KamarId))
            .GetValueOrThrow($"Kamar Operasi ID {request.KamarId} tidak ditemukan.");

        // 4. Validasi OpCase aktif
        var opCase = _opCaseRepo.LoadEntity(orderOp)
            .GetValueOrThrow("Invalid Order Operasi. OpCase data tidak ditemukan.");

        if (opCase.OrderOpState == OpCaseStateEnum.Cancelled)
            throw new InvalidOperationException($"Order Operasi ID {request.OrderOpId} telah dibatalkan.");

        if ((int)opCase.OrderOpState >= (int)OpCaseStateEnum.OpStarted)
            throw new InvalidOperationException($"Status Operasi ({opCase.OrderOpState}) tidak dapat dijadwalkan ulang karena operasi telah dimulai atau selesai.");

        // 5. Memuat jadwal eksisting
        ScheduleOpModel? existingSchedule = null;
        if (opCase.ScheduleOp != null && !string.IsNullOrWhiteSpace(opCase.ScheduleOp.ScheduleOpId) && opCase.ScheduleOp.ScheduleOpId != "-")
        {
            existingSchedule = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(opCase.ScheduleOp.ScheduleOpId)).GetValueOrDefault();
        }

        if (existingSchedule == null && opCase.Pasien != null && !string.IsNullOrWhiteSpace(opCase.Pasien.PasienId))
        {
            var listSchedule = _scheduleOpRepo.ListData(PasienModel.Key(opCase.Pasien.PasienId))?.ToList() ?? [];
            var scheduleWithOrderOpId = listSchedule
                .Where(x => !x.IsVoid)
                .FirstOrDefault(x => x.OrderOp.OrderOpId == request.OrderOpId);
            if (scheduleWithOrderOpId != null)
            {
                existingSchedule = _scheduleOpRepo.LoadEntity(ScheduleOpModel.Key(scheduleWithOrderOpId.ScheduleOpId)).GetValueOrDefault();
            }
        }

        if (existingSchedule == null || existingSchedule.AuditTrail.IsVoided)
        {
            throw new KeyNotFoundException($"Jadwal operasi untuk Order Operasi ID {request.OrderOpId} tidak ditemukan.");
        }

        if (request.Publish && !existingSchedule.IsDraft)
        {
            throw new InvalidOperationException($"Jadwal operasi untuk Order Operasi ID {request.OrderOpId} sudah berstatus resmi (bukan draf).");
        }

        // 6. Resolusi waktu, turnover buffer, dan dokter operator utama
        DateTime start = DateTime.ParseExact($"{request.Tgl} {request.Jam}", "yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture);
        DateTime end = start.AddMinutes(request.Durasi);
        int effectiveTurnoverBuffer = request.TurnoverBuffer > 0 ? request.TurnoverBuffer : 30;

        string targetDokterId = !string.IsNullOrWhiteSpace(request.DokterOperatorId)
            ? request.DokterOperatorId
            : existingSchedule.TeamLead?.PpaId ?? orderOp.DokterOperator?.PpaId ?? "";

        if (string.IsNullOrWhiteSpace(targetDokterId) || targetDokterId == "-")
            throw new ArgumentException("Dokter Operator Utama wajib ditentukan saat penjadwalan kamar operasi.");

        var targetDokter = _ppaRepo.LoadEntity(PpaType.Key(targetDokterId))
            .GetValueOrThrow($"Dokter Operator dengan ID {targetDokterId} tidak ditemukan.");

        var isDokter = targetDokter.IsDokter() || (targetDokter.ListSatTugas.FirstOrDefault()?.SatTugas?.Profesi == ProfesiType.Dokter);
        if (!isDokter)
            throw new ArgumentException("PPA yang ditugaskan sebagai Operator Utama harus berprofesi Dokter.");

        // 7. Evaluasi delta perubahan
        bool isRescheduleChanged = existingSchedule.TglOp != start
            || (existingSchedule.KamarOp?.KamarId ?? "") != request.KamarId
            || existingSchedule.Durasi != request.Durasi
            || existingSchedule.TurnoverBuffer != effectiveTurnoverBuffer;

        bool isLeaderChanged = targetDokterId != (existingSchedule.TeamLead?.PpaId ?? "");

        bool isPpaChanged = false;
        List<string>? targetSupportingPpaIds = null;
        if (request.ListPpaIds != null)
        {
            targetSupportingPpaIds = request.ListPpaIds
                .Where(id => !string.IsNullOrWhiteSpace(id) && id != targetDokterId)
                .Distinct()
                .ToList();

            var currentSupportingPpaIds = existingSchedule.ListPpa
                .Where(p => p.Ppa.PpaId != targetDokterId && p.Ppa.PpaId != (existingSchedule.TeamLead?.PpaId ?? ""))
                .Select(p => p.Ppa.PpaId)
                .Distinct()
                .ToList();

            isPpaChanged = !currentSupportingPpaIds.ToHashSet().SetEquals(targetSupportingPpaIds.ToHashSet());
        }

        // 8. Penegakan Validasi Alasan Mutasi Selektif pada Jadwal Resmi (TD-46)
        if (!existingSchedule.IsDraft)
        {
            if (isRescheduleChanged)
            {
                Guard.Against.NullOrWhiteSpace(request.AlasanReschedule, nameof(request.AlasanReschedule),
                    "Alasan perubahan jadwal (reschedule) wajib diisi saat jadwal telah berstatus resmi.");
                if (request.AlasanReschedule.Trim().Length < 5)
                    throw new ArgumentException("Alasan perubahan jadwal (reschedule) minimal 5 karakter.", nameof(request.AlasanReschedule));
            }

            if (isLeaderChanged)
            {
                Guard.Against.NullOrWhiteSpace(request.AlasanAssignLeader, nameof(request.AlasanAssignLeader),
                    "Alasan pergantian dokter operator utama wajib diisi saat jadwal telah berstatus resmi.");
                if (request.AlasanAssignLeader.Trim().Length < 5)
                    throw new ArgumentException("Alasan pergantian dokter operator utama minimal 5 karakter.", nameof(request.AlasanAssignLeader));
            }
        }

        // 9. Zero Room Collision Check (Hard Block HTTP 409)
        var activeSchedulesInRoom = _scheduleOpRepo.ListActiveSchedulesInRoom(request.KamarId, start.Date)?.ToList() ?? [];
        foreach (var s in activeSchedulesInRoom)
        {
            if (s.IsVoid) continue;
            if (s.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

            var startB = s.TglOp;
            var endB = s.TglOp.AddMinutes(s.Durasi);

            if (start < endB && end > startB)
            {
                throw new ScheduleCollisionException(
                    $"Bentrok jadwal kamar operasi: Kamar {request.KamarId} telah terisi oleh jadwal lain dari jam {startB:HH:mm} s/d {endB:HH:mm}.");
            }
        }

        // 10. Soft Warning Checks (Bentrok Dokter & Turnover Buffer)
        var warnings = new List<string>();

        // Bentrok dokter operator utama di kamar lain
        var activeDoctorSchedules = _scheduleOpRepo.ListActiveSchedulesForDoctor(targetDokter.PpaId, start.Date)?.ToList() ?? [];
        var activeTeamSchedules = _scheduleOpRepo.ListActiveSchedulesForPpaInTeam(targetDokter.PpaId, start.Date)?.ToList() ?? [];

        var combinedDoctorSchedules = activeDoctorSchedules
            .Concat(activeTeamSchedules)
            .GroupBy(x => x.ScheduleOpId)
            .Select(g => g.OrderByDescending(x => !string.IsNullOrWhiteSpace(x.ProfesiName)).First())
            .ToList();

        foreach (var d in combinedDoctorSchedules)
        {
            if (d.IsVoid) continue;
            if (d.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

            var startD = d.TglOp;
            var endD = d.TglOp.AddMinutes(d.Durasi);

            if (start < endD && end > startD)
            {
                var roomDisplay = !string.IsNullOrWhiteSpace(d.Kamar?.KamarId) ? d.Kamar.KamarId : request.KamarId;
                var roleInfo = !string.IsNullOrWhiteSpace(d.ProfesiName) ? $" sebagai {d.ProfesiName}" : "";
                warnings.Add($"Dokter/PPA {targetDokter.PpaName}{roleInfo} telah terjadwal pada operasi lain ({roomDisplay}) dari jam {startD:HH:mm} s/d {endD:HH:mm}.");
            }
        }

        // Bentrok anggota PPA pendukung baru
        if (targetSupportingPpaIds != null)
        {
            foreach (var ppaId in targetSupportingPpaIds)
            {
                if (!existingSchedule.ListPpa.Any(p => p.Ppa.PpaId == ppaId) || isRescheduleChanged)
                {
                    var ppaMember = _ppaRepo.LoadEntity(PpaType.Key(ppaId)).GetValueOrDefault();
                    if (ppaMember != null)
                    {
                        var docSchedules = _scheduleOpRepo.ListActiveSchedulesForDoctor(ppaMember.PpaId, start.Date)?.ToList() ?? [];
                        var teamSchedules = _scheduleOpRepo.ListActiveSchedulesForPpaInTeam(ppaMember.PpaId, start.Date)?.ToList() ?? [];
                        var memberCombined = docSchedules.Concat(teamSchedules).GroupBy(x => x.ScheduleOpId).Select(g => g.First()).ToList();

                        foreach (var md in memberCombined)
                        {
                            if (md.IsVoid) continue;
                            if (md.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

                            var startMd = md.TglOp;
                            var endMd = md.TglOp.AddMinutes(md.Durasi);

                            if (start < endMd && end > startMd)
                            {
                                var roomDisplay = !string.IsNullOrWhiteSpace(md.Kamar?.KamarId) ? md.Kamar.KamarId : request.KamarId;
                                warnings.Add($"Dokter/PPA {ppaMember.PpaName} telah terjadwal pada operasi lain ({roomDisplay}) dari jam {startMd:HH:mm} s/d {endMd:HH:mm}.");
                            }
                        }
                    }
                }
            }
        }

        // Pelanggaran Turnover Buffer sterilisasi
        foreach (var r in activeSchedulesInRoom)
        {
            if (r.IsVoid) continue;
            if (r.ScheduleOpId == existingSchedule.ScheduleOpId) continue;

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

        // 11. Eksekusi Mutasi Domain Model
        var scheduleOp = existingSchedule;
        var wasDraft = scheduleOp.IsDraft;

        // 11.1 Reschedule jika waktu/ruang/durasi berubah
        if (isRescheduleChanged)
        {
            scheduleOp.Reschedule(
                start,
                kamar.ToReff(),
                request.Durasi,
                effectiveTurnoverBuffer,
                scheduleOp.TeamLead ?? targetDokter.ToReff(),
                request.AlasanReschedule,
                request.IsOverride,
                request.OverrideReason,
                request.UserId,
                occurredAt);
        }

        // 11.2 Ganti Leader jika dokter operator berubah (TD-12)
        if (isLeaderChanged)
        {
            scheduleOp.AssignLeader(
                targetDokter,
                request.AlasanAssignLeader,
                request.UserId,
                occurredAt,
                request.IsOverride,
                request.OverrideReason);
        }

        // 11.3 Penyelarasan Tim PPA Pendukung jika ListPpaIds diberikan (TD-51)
        // Kontrak Rekonsiliasi Tim Bedah PPA Berbasis Staging Otoritatif & Nullable Fallback:
        // 1. Jika request.ListPpaIds == null: Backend melewatkan rekonsiliasi tim pendukung sepenuhnya,
        //    mempertahankan seluruh anggota tim apa adanya termasuk dokter lama yang baru saja didemosikan ke NoUrut = 2 oleh Langkah 11.2.
        // 2. Jika request.ListPpaIds != null: Backend merekonsiliasi keanggotaan PPA pendukung terhadap targetSupportingPpaIds:
        //    - Saat klien menyertakan dokter demosi (kondisi standar pasca-koreksi frontend), dokter demosi dipertahankan pada NoUrut = 2.
        //    - Saat klien sengaja tidak menyertakan dokter demosi (karena tombol [Hapus] diklik pengguna di UI), RemovePpa mengeksekusi penghapusan dokter secara sah.
        // 3. Menghindari jebakan false-positive Delta Guard, sehingga intensi pengguna dihormati secara presisi.
        // 4. scheduleOp.RemovePpa() tidak pernah dipanggil pada dokter/PPA yang ada di dalam targetSupportingPpaIds atau TeamLead aktif.
        if (targetSupportingPpaIds != null)
        {
            var membersToRemove = scheduleOp.ListPpa
                .Where(p => p.Ppa.PpaId != (scheduleOp.TeamLead?.PpaId ?? "") && !targetSupportingPpaIds.Contains(p.Ppa.PpaId))
                .ToList();

            foreach (var m in membersToRemove)
            {
                var ppaToRemove = _ppaRepo.LoadEntity(PpaType.Key(m.Ppa.PpaId)).GetValueOrDefault()
                    ?? (PpaType.Default with { PpaId = m.Ppa.PpaId, PpaName = m.Ppa.PpaName });
                scheduleOp.RemovePpa(ppaToRemove, request.UserId, occurredAt);
            }

            foreach (var ppaId in targetSupportingPpaIds)
            {
                if (scheduleOp.ListPpa.All(p => p.Ppa.PpaId != ppaId))
                {
                    var ppaToAdd = _ppaRepo.LoadEntity(PpaType.Key(ppaId))
                        .GetValueOrThrow($"PPA ID {ppaId} tidak ditemukan.");
                    scheduleOp.AddPpa(ppaToAdd, request.UserId, occurredAt);
                }
            }
        }

        // 11.4 Sinkronisasi Dua Arah ke OrderOp (TD-14)
        orderOp.SetDokterOperator(targetDokter.ToReff());
        orderOp.AuditTrail.Modif(request.UserId, occurredAt);

        // 11.5 Koordinasi Siklus Hidup Kasus Operasi & Konfirmasi (TD-39, TD-49)
        bool isPublished = false;
        if (wasDraft && request.Publish)
        {
            scheduleOp.Confirm(request.UserId, occurredAt);
            opCase.ConfirmSchedule(scheduleOp, occurredAt);
            isPublished = true;
        }
        else if (scheduleOp.IsDraft)
        {
            opCase.ScheduleDraft(scheduleOp, occurredAt);
        }
        else
        {
            opCase.Schedule(scheduleOp, occurredAt);
        }

        // 12. Transaksi Database Atomik (All-or-Nothing)
        using (var trans = TransHelper.NewScope())
        {
            _scheduleOpRepo.SaveChanges(scheduleOp);
            _opCaseRepo.SaveChanges(opCase);
            _orderOpRepo.SaveChanges(orderOp);
            trans.Complete();
        }

        // 13. Publikasi Domain Events
        if (isPublished)
        {
            await _mediator.Publish(new OkScheduleOpConfirmedEvent(scheduleOp, occurredAt), cancellationToken);
        }
        else if (!wasDraft && isRescheduleChanged)
        {
            var setCmd = new OkScheduleOpSetCommand(
                request.OrderOpId,
                request.KamarId,
                request.Tgl,
                request.Jam,
                request.Durasi,
                request.UserId,
                targetDokterId,
                effectiveTurnoverBuffer,
                request.AlasanReschedule,
                request.IsOverride,
                request.OverrideReason);
            await _mediator.Publish(new OkScheduleOpSetEvent(setCmd, scheduleOp), cancellationToken);
        }

        return new OkScheduleOpSaveWorkbenchResponse(
            scheduleOp.ScheduleOpId,
            scheduleOp.IsDraft,
            isRescheduleChanged,
            isLeaderChanged,
            isPpaChanged,
            isPublished);
    }
}
