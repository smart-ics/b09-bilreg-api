namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Model pemetaan monitor display antrean farmasi ke loket fisik (BILRG_AptDisplayLoket).
/// </summary>
public record PharmacyDisplayLoketModel(string DisplayId, string LoketKey, int SortOrder);

/// <summary>
/// Kontrak repository untuk pemetaan layar display antrean farmasi ke loket-loket fisik.
/// </summary>
public interface IPharmacyDisplayLoketRepo
{
    /// <summary>
    /// Mengambil seluruh konfigurasi loket yang dipantau oleh layar display tertentu, terurut berdasarkan SortOrder.
    /// </summary>
    IReadOnlyList<PharmacyDisplayLoketModel> ListByDisplayId(string displayId);

    /// <summary>
    /// Menyimpan atau memperbarui pemetaan loket ke layar display.
    /// </summary>
    void SaveMapping(string displayId, string loketKey, int sortOrder);

    /// <summary>
    /// Menghapus pemetaan loket dari layar display tertentu.
    /// </summary>
    void DeleteMapping(string displayId, string loketKey);
}
