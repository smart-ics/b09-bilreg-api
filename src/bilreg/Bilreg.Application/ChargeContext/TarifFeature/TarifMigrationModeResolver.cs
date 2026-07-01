using Microsoft.Extensions.Options;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public class TarifMigrationModeResolver : ITarifMigrationModeResolver
{
    private readonly TarifMigrationOptions _options;
    private readonly ITarifOperationalStateRepo _operationalStateRepo;

    public TarifMigrationModeResolver(
        IOptions<TarifMigrationOptions> options,
        ITarifOperationalStateRepo operationalStateRepo)
    {
        _options = options.Value;
        _operationalStateRepo = operationalStateRepo;
    }

    public TarifMigrationMode GetConfigMode() => _options.Mode;

    public TarifMigrationMode? GetDbOverrideMode() =>
        _operationalStateRepo.GetState().DbOverrideMode;

    public TarifMigrationMode GetEffectiveMode() =>
        GetDbOverrideMode() ?? GetConfigMode();

    public bool AllowRoutineImport()
    {
        var mode = GetEffectiveMode();
        return mode is TarifMigrationMode.ImportOnly or TarifMigrationMode.Hybrid;
    }

    public bool AllowImport(bool isEmergency = false)
    {
        if (AllowRoutineImport())
            return true;

        return isEmergency && _options.AllowEmergencyImport;
    }

    public bool AllowPublish()
    {
        var mode = GetEffectiveMode();
        return mode is not TarifMigrationMode.ImportOnly;
    }
}
