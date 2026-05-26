namespace Bilreg.Application.ChargeContext.TarifFeature;

/// <summary>
/// Operational migration stage for import vs publish authority (M0–M3).
/// </summary>
public enum TarifMigrationMode
{
    ImportOnly = 0,
    Hybrid = 1,
    PublishPrimary = 2,
    ImportDeprecated = 3
}
