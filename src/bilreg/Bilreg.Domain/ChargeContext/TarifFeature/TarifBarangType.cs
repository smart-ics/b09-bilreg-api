namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifBarangType(
    string TarifId,
    int NoUrut,
    string BrgId,
    string BrgName,
    string SatuanId,
    string SatuanName,
    decimal QtyBarang
);
