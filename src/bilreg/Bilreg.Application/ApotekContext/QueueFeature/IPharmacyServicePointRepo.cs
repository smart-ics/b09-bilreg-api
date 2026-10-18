using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Kontrak repository untuk entitas konfigurasi titik layanan depo farmasi (BILRG_AptServicePoint).
/// </summary>
public interface IPharmacyServicePointRepo :
    ISaveChange<PharmacyServicePointModel>,
    ILoadEntity<PharmacyServicePointModel, IPharmacyServicePointKey>
{
    /// <summary>
    /// Menyimpan atau memperbarui data konfigurasi service point farmasi.
    /// </summary>
    void Save(PharmacyServicePointModel model) => SaveChanges(model);

    /// <summary>
    /// Mengambil data service point berdasarkan ID unik (ServicePointId).
    /// </summary>
    MayBe<PharmacyServicePointModel> Get(string servicePointId)
        => LoadEntity(PharmacyServicePointModel.Key(servicePointId));

    /// <summary>
    /// Mengambil seluruh titik layanan antrean farmasi yang sedang aktif.
    /// </summary>
    IReadOnlyList<PharmacyServicePointModel> ListActive();

    /// <summary>
    /// Mengambil seluruh titik layanan antrean pada depo farmasi tertentu.
    /// </summary>
    IReadOnlyList<PharmacyServicePointModel> ListByDepo(string depoFarmasiId);
}
