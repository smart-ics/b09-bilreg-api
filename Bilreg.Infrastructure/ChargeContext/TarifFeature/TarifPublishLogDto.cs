using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifPublishLogDto(
    string PublishLogId,
    string TarifPolicyId,
    string PublishedBy,
    DateTime PublishedDate,
    int VariantCount,
    string Note)
{
    public static TarifPublishLogDto FromModel(TarifPublishLogType model) =>
        new(
            model.PublishLogId,
            model.TarifPolicyId,
            model.PublishedBy,
            model.PublishedDate,
            model.VariantCount,
            model.Note);

    public TarifPublishLogType ToModel(IEnumerable<TarifPublishLogDetailType> details) =>
        new(
            PublishLogId,
            TarifPolicyId,
            PublishedBy,
            PublishedDate,
            VariantCount,
            Note,
            details);
}

public record TarifPublishLogDetailDto(
    string PublishLogId,
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    string NilaiTarifId,
    decimal Nilai)
{
    public static TarifPublishLogDetailDto FromModel(string publishLogId, TarifPublishLogDetailType model) =>
        new(
            publishLogId,
            model.ItemNo,
            model.TarifId,
            model.KelasId,
            model.TipeTarifId,
            model.NilaiTarifId,
            model.Nilai);

    public TarifPublishLogDetailType ToModel() =>
        new(ItemNo, TarifId, KelasId, TipeTarifId, NilaiTarifId, Nilai);
}
