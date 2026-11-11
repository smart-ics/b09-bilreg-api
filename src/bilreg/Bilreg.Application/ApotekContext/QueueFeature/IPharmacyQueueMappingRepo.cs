using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Kontrak repository untuk pemetaan antrean fisik ke permintaan obat dan kehadiran pasien (BILRG_AptQueueMapping).
/// </summary>
public interface IPharmacyQueueMappingRepo :
    ISaveChange<QueueMappingModel>,
    ILoadEntity<QueueMappingModel, IQueueMappingKey>
{
    /// <summary>
    /// Menyimpan atau memperbarui data pemetaan antrean fisik ke permintaan obat.
    /// </summary>
    void Save(QueueMappingModel model) => SaveChanges(model);

    /// <summary>
    /// Mengambil data pemetaan berdasarkan jenis permintaan (Resep/JualBebas) dan ID dokumen.
    /// </summary>
    MayBe<QueueMappingModel> GetByDemand(QueueDemandKindEnum demandKind, string demandId)
        => LoadEntity(QueueMappingModel.Key(demandKind, demandId));

    /// <summary>
    /// Mengambil seluruh pemetaan permintaan obat untuk nomor tiket antrean tertentu.
    /// </summary>
    IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut);

    /// <summary>
    /// Menandai status kehadiran fisik pasien pada dokumen permintaan obat yang dipetakan.
    /// </summary>
    bool SetPasienHadir(QueueDemandKindEnum demandKind, string demandId, string userId, DateTime at) => false;
}
