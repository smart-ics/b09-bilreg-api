namespace Bilreg.Domain.IgdContext.TindakanIgdFeature;

public record TindakanIgdBarangType(
    int NoUrut,
    string BrgId,
    string SatuanBomId,
    decimal QtyBom,
    string SatuanKecilId,
    decimal QtyKecilRequested,
    decimal QtyKecilDeducted,
    decimal QtyKecilPending,
    bool IsChargeBarang,
    string PakaiBrgId,
    string TrsBillingId,
    bool IsVoid);
