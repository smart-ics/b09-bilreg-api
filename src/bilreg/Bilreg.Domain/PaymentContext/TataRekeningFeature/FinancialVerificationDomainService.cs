using System;
using System.Collections.Generic;
using System.Linq;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.Shared;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed class FinancialVerificationDomainService : IFinancialVerificationDomainService
{
    /// <summary>
    /// Cut-off harian yang dipakai pada penentuan hari rawat yang wajib dibebankan sewa kamar.
    /// Sejajar dengan cut-off yang dipakai pada rekonsiliasi checkout kamar rawat inap.
    /// </summary>
    private static readonly TimeSpan DailyCutOffTime = new(12, 0, 0);

    /// <summary>
    /// Batas durasi okupansi yang masih wajar untuk divalidasi (1 tahun).
    /// </summary>
    private const int MaximumStayDays = 366;

    private readonly IRoomChargeCompletenessQuery _roomChargeCompletenessQuery;

    public FinancialVerificationDomainService(IRoomChargeCompletenessQuery roomChargeCompletenessQuery)
    {
        _roomChargeCompletenessQuery = roomChargeCompletenessQuery
            ?? throw new ArgumentNullException(nameof(roomChargeCompletenessQuery));
    }

    public void Verify(
        TataRekeningModel tataRekening,
        string petugasVerif,
        DateTime verifiedAt,
        IEnumerable<MergeRequestModel> pendingMergeRequests)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        ArgumentNullException.ThrowIfNull(pendingMergeRequests);

        if (tataRekening.Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Financial Verification hanya dapat dilakukan setelah Billing berstatus CLOSED.");

        if (tataRekening.Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Financial Verification tidak dapat dilakukan pada billing yang sudah FINALIZED atau LUNAS.");

        var regId = tataRekening.RegId;
        var hasPendingMerge = pendingMergeRequests.Any(m =>
            m.Status == MergeRequestStatusEnum.Pending &&
            (string.Equals(m.SourceRegId, regId, StringComparison.Ordinal) ||
             string.Equals(m.TargetRegId, regId, StringComparison.Ordinal)));

        if (hasPendingMerge)
            throw new InvalidOperationException(
                "Financial Verification tidak dapat dimulai selama masih terdapat Merge Request berstatus Pending.");

        tataRekening.CompleteFinancialVerification(petugasVerif, verifiedAt);
    }

    public void RequireAdjustment(TataRekeningModel tataRekening)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        tataRekening.RequireFinancialAdjustment();
    }

    public void ResetVerification(TataRekeningModel tataRekening)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        tataRekening.ResetFinancialVerification();
    }

    /// <summary>
    /// Membandingkan interval durasi rawat inap dengan catatan Room Charge yang tercatat
    /// pada billing room charge. Interval okupansi tempat tidur diambil dari RNA-BED
    /// (PakaiBed) sebagai acuan; parameter admisi dan pelepasan menjadi nilai cadangan.
    /// </summary>
    public RoomChargeCompletenessResult ValidateRoomChargeCompleteness(
        string regId,
        DateTime admissionDate,
        DateTime dischargeDate)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regId);

        var stay = _roomChargeCompletenessQuery.GetStayInterval(regId);
        if (stay is null)
            return RoomChargeCompletenessResult.NotApplicable(regId);

        return ValidateRoomChargeCompleteness(regId, admissionDate, dischargeDate, stay);
    }

    /// <summary>
    /// Membandingkan interval durasi rawat inap dengan catatan Room Charge yang tercatat
    /// pada billing room charge. Seluruh interval okupansi diambil dari RNA-BED (PakaiBed).
    /// </summary>
    public RoomChargeCompletenessResult ValidateRoomChargeCompleteness(string regId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regId);

        var stay = _roomChargeCompletenessQuery.GetStayInterval(regId);
        if (stay is null)
            return RoomChargeCompletenessResult.NotApplicable(regId);

        return ValidateRoomChargeCompleteness(
            regId,
            stay.AdmissionDate,
            stay.DischargeDate ?? stay.AdmissionDate,
            stay);
    }

    private RoomChargeCompletenessResult ValidateRoomChargeCompleteness(
        string regId,
        DateTime admissionDate,
        DateTime dischargeDate,
        InpatientStayInterval stay)
    {
        // RNA-BED (PakaiBed) merupakan acuan interval okupansi.
        // Parameter pemanggil hanya menjadi cadangan bilamana data okupansi tidak lengkap.
        var effectiveAdmission = stay.AdmissionDate == default
            ? admissionDate
            : stay.AdmissionDate;

        var startDate = effectiveAdmission.Date;
        var endDate = stay.DischargeDate?.Date ?? dischargeDate.Date;
        if (endDate < startDate)
            endDate = startDate;

        if ((endDate - startDate).TotalDays > MaximumStayDays)
            throw new BusinessValidationException(
                "Interval durasi rawat inap tidak valid sehingga kelengkapan sewa kamar tidak dapat diverifikasi.");

        var requiredStayDates = EnumerateStayDates(startDate, endDate);
        ApplyAdmissionDayRule(requiredStayDates, effectiveAdmission, startDate, endDate);
        ApplyDischargeDayRule(requiredStayDates, stay);

        var billedStayDates = (_roomChargeCompletenessQuery.ListBilledRoomChargeDates(regId) ?? [])
            .Select(x => x.Date)
            .ToHashSet();

        var missingStayDates = requiredStayDates
            .Where(x => !billedStayDates.Contains(x))
            .OrderBy(x => x)
            .ToList();

        // Pelepasan bed riil belum tercatat sehingga tagihan kamar pada hari terakhir rawat
        // tidak dapat diverifikasi. Finalisasi billing ditahan sampai bed benar-benar dilepas.
        if (!stay.IsBedReleased && missingStayDates.Count == 0)
            missingStayDates.Add(endDate);

        return missingStayDates.Count == 0
            ? RoomChargeCompletenessResult.Complete(regId, requiredStayDates)
            : RoomChargeCompletenessResult.Incomplete(regId, requiredStayDates, missingStayDates);
    }

    /// <summary>
    /// Enumerasi seluruh tanggal kalender pada interval okupansi (inklusif).
    /// </summary>
    private static List<DateTime> EnumerateStayDates(DateTime startDate, DateTime endDate)
    {
        var stayDates = new List<DateTime>();
        for (var date = startDate; date <= endDate; date = date.AddDays(1))
            stayDates.Add(date);

        return stayDates;
    }

    /// <summary>
    /// Pasien yang masuk setelah cut-off harian tidak dibebankan sewa kamar pada hari admisi.
    /// Rawat inap yang berlangsung pada satu hari kalender tetap membebankan hari admisi.
    /// </summary>
    private static void ApplyAdmissionDayRule(
        List<DateTime> requiredStayDates,
        DateTime effectiveAdmission,
        DateTime startDate,
        DateTime endDate)
    {
        if (startDate == endDate)
            return;

        if (effectiveAdmission.TimeOfDay > DailyCutOffTime)
            requiredStayDates.Remove(startDate);
    }

    /// <summary>
    /// Checkout dalam masa tenggang (maksimal 2 jam setelah cut-off) tidak menghasilkan
    /// pembebanan sewa kamar pada hari pelepasan bed.
    /// </summary>
    private static void ApplyDischargeDayRule(
        List<DateTime> requiredStayDates,
        InpatientStayInterval stay)
    {
        if (!stay.IsBedReleased)
            return;

        if (stay.DischargeDate < stay.AdmissionDate)
            return;

        var chargedUnits = RoomChargeCalculationPolicy.EvaluateDuration(
            stay.AdmissionDate,
            stay.DischargeDate,
            DailyCutOffTime);

        if (chargedUnits <= 0m)
            requiredStayDates.Remove(stay.DischargeDate!.Value.Date);
    }
}
