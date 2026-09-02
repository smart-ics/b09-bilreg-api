using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.ResepKerjaFeature;

public class ResepKerjaModelTest
{
    [Fact]
    public void Electronic_intake_is_immutable_copy_without_physical_capture_fields()
    {
        var electronic = ResepKerjaModel.IntakeElectronic(
            ResepKerjaSourceKindEnum.LegacyResep, "RS-1", "R1", "P1", "Pasien", "D1", "Dokter", "LY01",
            0, 1, [Item(1, "BRG1")], [], AuditTrailType.Create("u", DateTime.Now));
        electronic.IterConsumed.Should().Be(0);
        electronic.CaptureNote.Should().BeEmpty();

        electronic.FreezeItems();
        var rewrite = () => electronic.RewriteItems([Item(1, "BRG2")], [], AuditTrailType.Create("u", DateTime.Now));
        rewrite.Should().Throw<ApotekDomainException>();
    }

    [Fact]
    public void IntakePhysical_requires_captureNote()
    {
        var act = () => IntakePhysical(captureNote: "   ");
        act.Should().Throw<ArgumentException>().WithParameterName("captureNote");
    }

    [Fact]
    public void IntakePhysical_requires_documentRef()
    {
        var act = () => IntakePhysical(documentRef: "");
        act.Should().Throw<ArgumentException>().WithParameterName("documentRef");
    }

    [Fact]
    public void IntakePhysical_requires_at_least_one_item()
    {
        var act = () => IntakePhysical(items: []);
        act.Should().Throw<ApotekDomainException>()
            .WithMessage("Physical Resep Kerja requires at least one item.");
    }

    [Fact]
    public void IntakePhysical_keeps_physical_source_and_capture_fields()
    {
        var physical = IntakePhysical(captureNote: "resep fisik poli", documentRef: "SCAN-2026-0001");
        physical.SourceKind.Should().Be(ResepKerjaSourceKindEnum.Physical);
        physical.SourceResepId.Should().BeEmpty();
        physical.CaptureNote.Should().Be("resep fisik poli");
        physical.DocumentRef.Should().Be("SCAN-2026-0001");
    }

    private static ResepKerjaModel IntakePhysical(
        string captureNote = "note",
        string documentRef = "doc",
        IEnumerable<ResepKerjaItemModel>? items = null)
        => ResepKerjaModel.IntakePhysical(
            "R1", "P1", "Pasien", "D1", "Dokter", "LY01", captureNote, documentRef,
            items ?? [Item(1, "BRG1")], [], AuditTrailType.Create("u", DateTime.Now));

    private static ResepKerjaItemModel Item(int no, string brg)
        => new(no, no, brg, brg, "TAB", "TAB", 10, 0, "3x1", "", "", false);
}
