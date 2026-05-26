using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifVariantDto(
    string TarifPolicyId,
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    decimal Nilai,
    string PublishedNilaiTarifId)
{
    public static TarifVariantDto FromModel(TarifVariantType model) =>
        new(
            model.TarifPolicyId,
            model.ItemNo,
            model.TarifId,
            model.KelasId,
            model.TipeTarifId,
            model.Nilai,
            model.PublishedNilaiTarifId);

    public TarifVariantType ToModel(IEnumerable<TarifVariantKomponenType> listKomponen) =>
        new(
            TarifPolicyId,
            ItemNo,
            TarifId,
            KelasId,
            TipeTarifId,
            Nilai,
            PublishedNilaiTarifId,
            listKomponen);
}
