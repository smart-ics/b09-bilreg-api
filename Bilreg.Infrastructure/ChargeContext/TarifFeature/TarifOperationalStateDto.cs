namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public record TarifOperationalStateDto(
    int RowId,
    int? MigrationMode,
    DateTime? LastImportAt,
    string LastImportBy,
    DateTime? LastBaselineAt,
    string LastBaselinePolicyId,
    DateTime UpdatedAt,
    string UpdatedBy);
