namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record NilaiTarifProjectionSummaryRow(
    int TotalVariantCount,
    int ImportOnlyCount,
    int PolicySourcedCount);

public record NilaiTarifProjectionConsistencyRow(
    int DuplicateVariantKeyCount,
    int HeadersWithoutKomponenCount,
    int OrphanSourcePolicyIdCount);

public record TarifLastPublishRow(
    DateTime PublishedDate,
    string TarifPolicyId,
    string PublishLogId);
