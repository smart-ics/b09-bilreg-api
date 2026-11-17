namespace Bilreg.Domain.ChargeContext.TindakanFeature;

public record TindakanBarangType(
    string TindakanBarangId,
    string TindakanId,
    int NoUrut,
    string BrgId,
    string SatuanBomId,
    decimal QtyBom,
    string SatuanKecilId,
    decimal QtyKecilRequested,
    decimal QtyKecilDeducted,
    decimal QtyKecilPending,
    bool IsChargeBarang,
    decimal HargaJualTotal,
    string PakaiBrgId = "",
    string TrsBillingId = "",
    bool IsVoid = false,
    string BrgName = "",
    decimal QtyPending = 0m
) : ITindakanKey;
