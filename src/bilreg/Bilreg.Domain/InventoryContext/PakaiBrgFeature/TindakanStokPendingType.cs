namespace Bilreg.Domain.InventoryContext.PakaiBrgFeature;

public record TindakanStokPendingType(
    string PendingId,
    string TindakanId,
    string TindakanBarangId,
    int NoUrut,
    string RegId,
    string LayananId,
    string BrgId,
    string SatuanKecilId,
    decimal QtyPending,
    decimal QtySettled = 0m,
    bool StatusPotStok = false,
    string PakaiBrgId = "",
    bool IsVoid = false,
    DateTime SettledDate = default,
    DateTime VoidDate = default,
    DateTime CrtDate = default
);
