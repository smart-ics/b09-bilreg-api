using Bilreg.Domain.ApotekContext.QueueFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.ApotekContext.QueueFeature;

/// <summary>
/// Kontrak repository untuk entitas operasional antrean farmasi (BILRG_AptQueueEntry).
/// </summary>
public interface IApotekQueueEntryRepo :
    ISaveChange<ApotekQueueEntryModel>,
    ILoadEntity<ApotekQueueEntryModel, IApotekQueueEntryKey>
{
    /// <summary>
    /// Menyimpan atau memperbarui data entri antrean farmasi.
    /// </summary>
    void Save(ApotekQueueEntryModel model) => SaveChanges(model);

    /// <summary>
    /// Mengambil entri antrean farmasi berdasarkan nomor sesi antrean rumah sakit dan nomor urut.
    /// </summary>
    MayBe<ApotekQueueEntryModel> Get(string antrianId, int noUrut)
        => LoadEntity(ApotekQueueEntryModel.Key(antrianId, noUrut));

    /// <summary>
    /// Mengambil seluruh entri antrean aktif pada depo farmasi tertentu (belum SELESAI, BATAL, atau NO_SHOW).
    /// </summary>
    IReadOnlyList<ApotekQueueEntryModel> ListActiveByDepo(string depoId);

    /// <summary>
    /// Mengambil entri antrean pada depo farmasi berdasarkan fase operasional tertentu.
    /// </summary>
    IReadOnlyList<ApotekQueueEntryModel> ListByPhase(string depoId, ApotekPhaseEnum phase);
}
