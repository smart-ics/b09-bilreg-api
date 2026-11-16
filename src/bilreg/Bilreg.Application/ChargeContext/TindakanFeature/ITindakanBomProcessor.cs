using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;

namespace Bilreg.Application.ChargeContext.TindakanFeature;

public record TindakanBomItemResult(
    string BrgId,
    string BrgName,
    int NoUrut,
    string SatuanBomId,
    decimal QtyBom,
    decimal Konversi,
    string SatuanKecilId,
    decimal QtyKecilRequested,
    decimal QtyImmediateCut,
    decimal QtyPending,
    string PakaiBrgId,
    bool IsChargeBarang,
    decimal HargaJualTotal,
    string TindakanBarangId,
    decimal QtyPerTindakan = 0m
);

public record TindakanBomResult(
    string TindakanId,
    string PakaiBrgId,
    bool IsChargeBarang,
    IReadOnlyList<TindakanBomItemResult> Items
);

public interface ITindakanBomProcessor
{
    Task<TindakanBomResult> Process(
        TindakanModel tindakan,
        RegModel reg,
        string userId,
        CancellationToken cancellationToken = default);
}
