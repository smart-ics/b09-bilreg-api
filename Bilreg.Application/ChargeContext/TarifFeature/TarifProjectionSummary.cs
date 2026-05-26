namespace Bilreg.Application.ChargeContext.TarifFeature;

public record TarifProjectionSummary(
    int TotalVariantCount,
    int ImportOnlyCount,
    int PolicySourcedCount);

public record TarifProjectionConsistencyReport(
    int DuplicateVariantKeyCount,
    int HeadersWithoutKomponenCount,
    int OrphanSourcePolicyIdCount,
    bool IsHealthy);

public record TarifMigrationStatus(
    TarifMigrationMode EffectiveMode,
    TarifMigrationMode ConfigMode,
    TarifMigrationMode? DbOverrideMode,
    bool AllowImport,
    bool AllowPublish,
    bool AllowEmergencyImport,
    bool IsOperationInProgress,
    TarifOperation ActiveOperation,
    TarifProjectionSummary ProjectionSummary,
    DateTime? LastImportAt,
    string LastImportBy,
    DateTime? LastPublishAt,
    string? LastPublishPolicyId,
    string? LastPublishLogId,
    DateTime? LastBaselineAt,
    string? LastBaselinePolicyId,
    IReadOnlyList<string> BaselinePolicyNumbers);

public record TarifOperationalState(
    TarifMigrationMode? DbOverrideMode,
    DateTime? LastImportAt,
    string LastImportBy,
    DateTime? LastBaselineAt,
    string LastBaselinePolicyId,
    DateTime UpdatedAt,
    string UpdatedBy);
