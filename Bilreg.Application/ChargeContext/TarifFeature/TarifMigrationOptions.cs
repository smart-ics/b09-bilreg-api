namespace Bilreg.Application.ChargeContext.TarifFeature;

public class TarifMigrationOptions
{
    public const string SECTION_NAME = "Tarif";

    public TarifMigrationMode Mode { get; set; } = TarifMigrationMode.ImportOnly;

    public bool AllowEmergencyImport { get; set; }
}
