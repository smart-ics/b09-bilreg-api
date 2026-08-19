using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.ResepKerjaFeature;

public class ResepKerjaModelTest
{
    [Fact]
    public void Electronic_intake_is_immutable_copy_and_physical_requires_capture_fields()
    {
        var electronic = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-1", "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 1, [Item(1, "BRG1")], [], AuditTrailType.Create("u", DateTime.Now));
        electronic.IterConsumed.Should().Be(0);
        electronic.CaptureNote.Should().BeEmpty();

        var actPhysical = () => ResepKerjaModel.IntakePhysical(
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01", "", "doc", [Item(1, "BRG1")], [],
            AuditTrailType.Create("u", DateTime.Now));
        actPhysical.Should().Throw<Exception>();

        electronic.FreezeItems();
        var rewrite = () => electronic.RewriteItems([Item(1, "BRG2")], [], AuditTrailType.Create("u", DateTime.Now));
        rewrite.Should().Throw<ApotekDomainException>();
    }

    private static ResepKerjaItemModel Item(int no, string brg)
        => new(no, no, brg, brg, "TAB", "TAB", 10, 0, "3x1", "", "", false);
}
