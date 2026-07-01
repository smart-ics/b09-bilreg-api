namespace Bilreg.Domain.ChargeContext.TarifFeature;

public record TarifPublishLogType : ITarifPublishLogKey
{
    private readonly List<TarifPublishLogDetailType> _details;

    public TarifPublishLogType(
        string publishLogId,
        string tarifPolicyId,
        string publishedBy,
        DateTime publishedDate,
        int variantCount,
        string note,
        IEnumerable<TarifPublishLogDetailType> details)
    {
        PublishLogId = publishLogId;
        TarifPolicyId = tarifPolicyId;
        PublishedBy = publishedBy;
        PublishedDate = publishedDate;
        VariantCount = variantCount;
        Note = note;
        _details = details?.OrderBy(x => x.ItemNo).ToList() ?? [];
    }

    public static TarifPublishLogType Default => new(
        "-", "-", "", new DateTime(3000, 1, 1), 0, "", []);

    public static ITarifPublishLogKey Key(string id) => Default with { PublishLogId = id };

    public string PublishLogId { get; init; }
    public string TarifPolicyId { get; init; }
    public string PublishedBy { get; init; }
    public DateTime PublishedDate { get; init; }
    public int VariantCount { get; init; }
    public string Note { get; init; }
    public IEnumerable<TarifPublishLogDetailType> Details => _details;
}

public interface ITarifPublishLogKey
{
    string PublishLogId { get; }
}

public record TarifPublishLogDetailType(
    int ItemNo,
    string TarifId,
    string KelasId,
    string TipeTarifId,
    string NilaiTarifId,
    decimal Nilai);
