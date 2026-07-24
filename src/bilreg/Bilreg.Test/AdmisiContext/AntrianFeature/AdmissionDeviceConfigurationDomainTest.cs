using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class AdmissionDeviceConfigurationDomainTest
{
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
}
