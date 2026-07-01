namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifOperationalStateRepo
{
    TarifOperationalState GetState();

    void SetMigrationModeOverride(TarifMigrationMode? mode, string userId);

    void RecordImport(string userId, DateTime importedAt);

    void RecordBaseline(string policyId, string userId, DateTime baselineAt);
}
