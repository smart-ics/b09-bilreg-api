namespace Bilreg.Application.ChargeContext.TarifFeature;

public class TarifMigrationGuard : ITarifMigrationGuard
{
    private readonly ITarifMigrationModeResolver _modeResolver;

    public TarifMigrationGuard(ITarifMigrationModeResolver modeResolver) =>
        _modeResolver = modeResolver;

    public void EnsureImportAllowed(bool isEmergency = false)
    {
        if (_modeResolver.AllowImport(isEmergency))
            return;

        var mode = _modeResolver.GetEffectiveMode();
        throw new InvalidOperationException(
            $"NilaiTarif import tidak diperbolehkan pada mode migrasi {mode}. " +
            "Gunakan publish policy, atau jalankan import darurat dengan IsEmergency=true jika AllowEmergencyImport diaktifkan.");
    }

    public void EnsurePublishAllowed()
    {
        if (_modeResolver.AllowPublish())
            return;

        var mode = _modeResolver.GetEffectiveMode();
        throw new InvalidOperationException(
            $"TarifPolicy publish tidak diperbolehkan pada mode migrasi {mode}. " +
            "Ubah mode ke Hybrid atau PublishPrimary terlebih dahulu.");
    }
}
