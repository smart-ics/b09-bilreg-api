using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionDeviceConfigurationDomainTest
{
    [Fact]
    public void ActiveKiosk_RequiresServicePointMapping()
    {
        var act = () => AdmissionQueueKioskModel.Create(
            " kiosk-01 ", "Kiosk 01", "", true, 5050, "",
            Array.Empty<AdmissionKioskServicePointMapping>(), "u1", DateTime.UtcNow);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*requires at least one service point*");
    }

    [Fact]
    public void Kiosk_NormalizesAndOrdersUniqueServicePoints()
    {
        var model = AdmissionQueueKioskModel.Create(
            " kiosk-01 ", "Kiosk 01", "", true, 5050, "",
            [
                new AdmissionKioskServicePointMapping(" BPJS ", 2),
                new AdmissionKioskServicePointMapping("REG", 0),
                new AdmissionKioskServicePointMapping("reg", 5)
            ],
            "u1",
            DateTime.UtcNow);

        model.StationId.Should().Be("kiosk-01");
        model.ServicePoints.Select(x => x.ServicePointId).Should().Equal("REG", "BPJS");
    }

    [Fact]
    public void Kiosk_RejectsInvalidPrinterPortAndStaleUpdate()
    {
        var invalidPort = () => AdmissionQueueKioskModel.Create(
            "kiosk-01", "Kiosk 01", "", false, 0, "",
            Array.Empty<AdmissionKioskServicePointMapping>(), "u1", DateTime.UtcNow);
        invalidPort.Should().Throw<ArgumentException>();

        var model = AdmissionQueueKioskModel.Create(
            "kiosk-01", "Kiosk 01", "", false, 5050, "",
            Array.Empty<AdmissionKioskServicePointMapping>(), "u1", DateTime.UtcNow);
        var stale = () => model.Update("Kiosk", "", 5050, "", 9, "u2", DateTime.UtcNow);
        stale.Should().Throw<InvalidOperationException>().WithMessage("*stale*");
    }
    [Fact]
    public void Workstation_Update_RejectsStaleRowVersion()
    {
        var model = AdmissionWorkstationModel.Create(
            "WS-1", "PC 1", "Lobby", "LOKET-01", true, "", "u1", DateTime.UtcNow);
        var act = () => model.Update("PC 1b", "Lobby", "LOKET-01", "", 99, "u1", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*stale*");
    }

    [Fact]
    public void ActiveDisplay_RequiresLoketMapping()
    {
        var act = () => AdmissionQueueDisplayModel.Create(
            "DISP-1", "Lobby A", "Lobby", true, true, 15000, "default", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow);
        act.Should().Throw<InvalidOperationException>().WithMessage("*loket*");
    }

    [Fact]
    public void Display_AllowsManyLokets()
    {
        var model = AdmissionQueueDisplayModel.Create(
            "DISP-1", "Lobby A", "Lobby", true, true, 15000, "default", "",
            [
                new AdmissionDisplayLoketMapping("LOKET-01", 0),
                new AdmissionDisplayLoketMapping("LOKET-02", 1)
            ],
            "u1", DateTime.UtcNow);
        model.Lokets.Should().HaveCount(2);
    }

    [Fact]
    public void ActiveDisplay_ClinicLayout_RequiresPoliMapping()
    {
        var act = () => AdmissionQueueDisplayModel.Create(
            "DISP-CLINIC-1", "Poli Display", "Klinik Lt 2", true, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow,
            Array.Empty<AdmissionDisplayPoliMapping>());

        act.Should().Throw<InvalidOperationException>().WithMessage("*requires at least one poli*");
    }

    [Fact]
    public void ActiveDisplay_ClinicLayout_SucceedsWithPoliMapping()
    {
        var model = AdmissionQueueDisplayModel.Create(
            "DISP-CLINIC-1", "Poli Display", "Klinik Lt 2", true, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow,
            [
                new AdmissionDisplayPoliMapping("POLI-INT", 0),
                new AdmissionDisplayPoliMapping("POLI-ANAK", 1)
            ]);

        model.IsClinicLayout.Should().BeTrue();
        model.Polis.Should().HaveCount(2);
        model.Polis.Select(x => x.PoliId).Should().Equal("POLI-INT", "POLI-ANAK");
    }

    [Fact]
    public void Display_EnsureCanBoot_ClinicLayout_ValidatesActiveAndPolis()
    {
        var inactiveModel = AdmissionQueueDisplayModel.Create(
            "DISP-CLINIC-1", "Poli Display", "Klinik Lt 2", false, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow,
            [new AdmissionDisplayPoliMapping("POLI-INT", 0)]);

        var actInactive = () => inactiveModel.EnsureCanBoot();
        actInactive.Should().Throw<InvalidOperationException>().WithMessage("*inactive*");

        var activeNoPolis = AdmissionQueueDisplayModel.Load(
            "DISP-CLINIC-2", "Poli Display", "Klinik Lt 2", true, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), 1, "u1", DateTime.UtcNow, "u1", DateTime.UtcNow,
            Array.Empty<AdmissionDisplayPoliMapping>());

        var actNoPolis = () => activeNoPolis.EnsureCanBoot();
        actNoPolis.Should().Throw<InvalidOperationException>().WithMessage("*no poli mapping*");

        var validModel = AdmissionQueueDisplayModel.Create(
            "DISP-CLINIC-3", "Poli Display", "Klinik Lt 2", true, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow,
            [new AdmissionDisplayPoliMapping("POLI-INT", 0)]);

        var actValid = () => validModel.EnsureCanBoot();
        actValid.Should().NotThrow();
    }

    [Fact]
    public void Display_ReplacePolis_UpdatesAndNormalizesPolis()
    {
        var model = AdmissionQueueDisplayModel.Create(
            "DISP-CLINIC-1", "Poli Display", "Klinik Lt 2", true, true, 15000, "clinic", "",
            Array.Empty<AdmissionDisplayLoketMapping>(), "u1", DateTime.UtcNow,
            [new AdmissionDisplayPoliMapping("POLI-INT", 0)]);

        var updated = model.ReplacePolis(
            [
                new AdmissionDisplayPoliMapping(" POLI-MATA ", 2),
                new AdmissionDisplayPoliMapping("POLI-ANAK", 0),
                new AdmissionDisplayPoliMapping("poli-anak", 5)
            ],
            1, "u2", DateTime.UtcNow);

        updated.RowVersion.Should().Be(2);
        updated.Polis.Select(x => x.PoliId).Should().Equal("POLI-ANAK", "POLI-MATA");
    }
}
