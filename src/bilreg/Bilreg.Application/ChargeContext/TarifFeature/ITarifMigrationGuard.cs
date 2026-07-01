namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface ITarifMigrationGuard
{
    void EnsureImportAllowed(bool isEmergency = false);

    void EnsurePublishAllowed();
}
