using System.Collections.Generic;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

/// <summary>
/// Port domain untuk membaca data pendukung validasi kelengkapan sewa kamar (Room Charge).
/// Implementasi berada pada layer Infrastructure.
/// </summary>
public interface IRoomChargeCompletenessQuery
{
    /// <summary>
    /// Memuat interval penempatan tempat tidur pasien rawat inap dari RNA-BED (PakaiBed).
    /// Mengembalikan null bila tidak ada data okupansi (episode bukan rawat inap).
    /// </summary>
    InpatientStayInterval? GetStayInterval(string regId);

    /// <summary>
    /// Memuat daftar tanggal rawat yang sudah memiliki catatan Room Charge yang sah
    /// (bukan Reversed) pada billing room charge.
    /// </summary>
    IEnumerable<DateTime> ListBilledRoomChargeDates(string regId);
}
