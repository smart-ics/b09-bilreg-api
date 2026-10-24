using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.InventoryContext.PakaiBrgFeature;

public interface IPakaiBrgRepo :
    ISaveChange<PakaiBrgModel>,
    ILoadEntity<PakaiBrgModel, IPakaiBrgKey>
{
    IEnumerable<PakaiBrgModel> ListByLayanan(ILayananKey layanan, Periode periode);
    IEnumerable<PakaiBrgStokLokasiView> ListStokLokasi(ILayananKey layanan);
}

public record PakaiBrgStokLokasiView(
    string BrgId,
    string BrgName,
    decimal TotalStok,
    string SatuanId,
    string SatuanName,
    string GroupRekDkId,
    decimal HppDefault,
    IReadOnlyList<PakaiBrgStokBatchView> Batches);

public record PakaiBrgStokBatchView(
    string NoBatch,
    DateTime TglEd,
    decimal QtySisa,
    decimal Hpp);
