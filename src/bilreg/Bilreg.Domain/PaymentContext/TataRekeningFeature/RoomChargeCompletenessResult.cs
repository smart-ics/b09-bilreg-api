using System;
using System.Collections.Generic;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Hasil validasi kelengkapan pembebanan sewa kamar (Room Charge) untuk satu episode rawat inap.
/// </summary>
/// <param name="RegId">Nomor registrasi episode rawat inap.</param>
/// <param name="IsComplete">True bila seluruh hari rawat wajib sudah dibebankan sewa kamar.</param>
/// <param name="Status">Status verifikasi finansial yang dihasilkan oleh validasi.</param>
/// <param name="RequiredStayDates">Daftar tanggal rawat yang wajib memiliki catatan Room Charge.</param>
/// <param name="MissingStayDates">Daftar tanggal rawat yang belum memiliki catatan Room Charge yang sah.</param>
public record RoomChargeCompletenessResult(
    string RegId,
    bool IsComplete,
    FinancialVerificationStatusEnum Status,
    IReadOnlyList<DateTime> RequiredStayDates,
    IReadOnlyList<DateTime> MissingStayDates)
{
    /// <summary>
    /// Validasi tidak berlaku karena episode tidak memiliki data okupansi tempat tidur
    /// (bukan rawat inap). Tidak ada sewa kamar yang diharapkan.
    /// </summary>
    public static RoomChargeCompletenessResult NotApplicable(string regId)
    {
        return new RoomChargeCompletenessResult(
            regId,
            true,
            FinancialVerificationStatusEnum.Valid,
            Array.Empty<DateTime>(),
            Array.Empty<DateTime>());
    }

    /// <summary>
    /// Seluruh hari rawat yang wajib sudah dibebankan sewa kamar.
    /// </summary>
    public static RoomChargeCompletenessResult Complete(
        string regId,
        IReadOnlyList<DateTime> requiredStayDates)
    {
        return new RoomChargeCompletenessResult(
            regId,
            true,
            FinancialVerificationStatusEnum.Valid,
            requiredStayDates,
            Array.Empty<DateTime>());
    }

    /// <summary>
    /// Terdapat hari rawat yang belum dibebankan sewa kamar.
    /// </summary>
    public static RoomChargeCompletenessResult Incomplete(
        string regId,
        IReadOnlyList<DateTime> requiredStayDates,
        IReadOnlyList<DateTime> missingStayDates)
    {
        return new RoomChargeCompletenessResult(
            regId,
            false,
            FinancialVerificationStatusEnum.CompletenessIncomplete,
            requiredStayDates,
            missingStayDates);
    }
}
