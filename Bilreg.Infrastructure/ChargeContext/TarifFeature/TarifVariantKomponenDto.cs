using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifVariantKomponenDto(
    string TarifPolicyId,
    int ItemNo,
    int NoUrut,
    string KomponenId,
    decimal Nilai)
{
    public static TarifVariantKomponenDto FromModel(TarifVariantType variant, TarifVariantKomponenType line) =>
        new(
            variant.TarifPolicyId,
            variant.ItemNo,
            line.NoUrut,
            line.Komponen.KomponenId,
            line.Nilai);

    public TarifVariantKomponenType ToModel() =>
        new(NoUrut, new KomponenReff(KomponenId, ""), Nilai);
}
