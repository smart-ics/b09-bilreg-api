namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifMigrationModeResolver
{
    TarifMigrationMode GetConfigMode();

    TarifMigrationMode? GetDbOverrideMode();

    TarifMigrationMode GetEffectiveMode();

    bool AllowImport(bool isEmergency = false);

    bool AllowRoutineImport();

    bool AllowPublish();
}
