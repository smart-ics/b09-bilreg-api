namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Interval penempatan tempat tidur pasien rawat inap (RNA-BED / PakaiBed).
/// <para>
/// <see cref="AdmissionDate"/> adalah waktu check-in riil saat pasien menempati kamar.
/// <see cref="DischargeDate"/> adalah waktu pelepasan bed riil (bed checkout). Nilai null
/// berarti pelepasan bed belum pernah tercatat sehingga tagihan kamar pada hari terakhir
/// tidak dapat diverifikasi.
/// </para>
/// </summary>
public record InpatientStayInterval(
    string RegId,
    DateTime AdmissionDate,
    DateTime? DischargeDate)
{
    /// <summary>
    /// True bila pelepasan bed riil sudah tercatat pada RNA-BED (PakaiBed).
    /// </summary>
    public bool IsBedReleased => DischargeDate.HasValue;
}
