using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using FluentAssertions;

namespace Bilreg.Test.ApotekContext.TelaahResepFeature;

public class TelaahResepKajianModelTest
{
    [Fact]
    public void DefaultCatalog_contains_13_canonical_items_ordered()
    {
        var catalog = TelaahResepKajianModel.DefaultCatalog();

        catalog.Should().HaveCount(13);
        catalog.Select(x => x.NoUrut).Should().ContainInOrder(Enumerable.Range(1, 13));
        catalog.Select(x => x.KajianResepId).Should().ContainInOrder(
            "1.1", "1.2", "1.3", "1.4",
            "2.1", "2.2", "2.3", "2.4",
            "3.1", "3.2", "3.3", "3.4", "3.5"
        );
    }

    [Theory]
    [InlineData("1.1")]
    [InlineData("1.2")]
    [InlineData("1.3")]
    [InlineData("1.4")]
    [InlineData("2.1")]
    [InlineData("2.2")]
    [InlineData("2.3")]
    [InlineData("2.4")]
    [InlineData("3.1")]
    public void Positive_polarity_items_default_to_true(string kajianId)
    {
        var item = TelaahResepKajianModel.Create(1, kajianId, "Test", "1", "Grup");

        item.DefaultIsYes.Should().BeTrue();
        item.IsYes.Should().BeTrue();
        item.RequiresNote.Should().BeFalse();
        item.HasRequiredNote.Should().BeTrue();
        item.IsCompliant.Should().BeTrue();

        var answeredNo = item.WithAnswer(false, "");
        answeredNo.IsYes.Should().BeFalse();
        answeredNo.RequiresNote.Should().BeTrue();
        answeredNo.HasRequiredNote.Should().BeFalse();
        answeredNo.IsCompliant.Should().BeFalse();

        var answeredNoWithNote = item.WithAnswer(false, "Klarifikasi dokter");
        answeredNoWithNote.IsYes.Should().BeFalse();
        answeredNoWithNote.RequiresNote.Should().BeTrue();
        answeredNoWithNote.HasRequiredNote.Should().BeTrue();
        answeredNoWithNote.IsCompliant.Should().BeTrue();
    }

    [Theory]
    [InlineData("3.2")]
    [InlineData("3.3")]
    [InlineData("3.4")]
    [InlineData("3.5")]
    public void Negative_polarity_items_default_to_false(string kajianId)
    {
        var item = TelaahResepKajianModel.Create(1, kajianId, "Test", "3", "Klinis");

        item.DefaultIsYes.Should().BeFalse();
        item.IsYes.Should().BeFalse();
        item.RequiresNote.Should().BeFalse();
        item.HasRequiredNote.Should().BeTrue();
        item.IsCompliant.Should().BeTrue();

        var answeredYes = item.WithAnswer(true, "");
        answeredYes.IsYes.Should().BeTrue();
        answeredYes.RequiresNote.Should().BeTrue();
        answeredYes.HasRequiredNote.Should().BeFalse();
        answeredYes.IsCompliant.Should().BeFalse();

        var answeredYesWithNote = item.WithAnswer(true, "Sudah disesuaikan");
        answeredYesWithNote.IsYes.Should().BeTrue();
        answeredYesWithNote.RequiresNote.Should().BeTrue();
        answeredYesWithNote.HasRequiredNote.Should().BeTrue();
        answeredYesWithNote.IsCompliant.Should().BeTrue();
    }

    [Fact]
    public void Rehydrate_and_WithAnswer_preserve_and_update_immutably()
    {
        var rehydrated = TelaahResepKajianModel.Rehydrate(5, "2.1", "Nama obat sesuai?", "2", "Farmasetik", false, "Catatan lama");

        rehydrated.NoUrut.Should().Be(5);
        rehydrated.KajianResepId.Should().Be("2.1");
        rehydrated.KajianResepName.Should().Be("Nama obat sesuai?");
        rehydrated.GrupKajianResepId.Should().Be("2");
        rehydrated.GrupKajianResepName.Should().Be("Farmasetik");
        rehydrated.IsYes.Should().BeFalse();
        rehydrated.CatatanKajian.Should().Be("Catatan lama");

        var updated = rehydrated.WithAnswer(true, "Catatan baru");
        updated.Should().NotBeSameAs(rehydrated);
        updated.IsYes.Should().BeTrue();
        updated.CatatanKajian.Should().Be("Catatan baru");
        rehydrated.IsYes.Should().BeFalse();
    }

    [Fact]
    public void TelaahResepItemModel_preserves_prescribed_snapshot_after_WithDisposition()
    {
        var pending = TelaahResepItemModel.Pending(1, 10, "BRG01", "Paracetamol 500mg", 10m);

        pending.PrescribedBrgId.Should().Be("BRG01");
        pending.PrescribedBrgName.Should().Be("Paracetamol 500mg");
        pending.PrescribedQty.Should().Be(10m);
        pending.AcceptedBrgId.Should().Be("BRG01");
        pending.AcceptedBrgName.Should().Be("Paracetamol 500mg");
        pending.AcceptedQty.Should().Be(10m);

        var substituted = pending.WithDisposition(
            TelaahDispositionEnum.AcceptedSubstitute,
            "BRG02",
            "Paracetamol Drop",
            1m,
            "Konfirmasi dokter: ganti sediaan drop",
            "PHARM01");

        substituted.PrescribedBrgId.Should().Be("BRG01");
        substituted.PrescribedBrgName.Should().Be("Paracetamol 500mg");
        substituted.PrescribedQty.Should().Be(10m);

        substituted.AcceptedBrgId.Should().Be("BRG02");
        substituted.AcceptedBrgName.Should().Be("Paracetamol Drop");
        substituted.AcceptedQty.Should().Be(1m);
        substituted.Reason.Should().Be("Konfirmasi dokter: ganti sediaan drop");
        substituted.PharmacistId.Should().Be("PHARM01");

        var rejected = pending.WithDisposition(
            TelaahDispositionEnum.Rejected,
            "BRG01",
            "Paracetamol 500mg",
            10m,
            "Alergi",
            "PHARM01");

        rejected.PrescribedBrgId.Should().Be("BRG01");
        rejected.PrescribedBrgName.Should().Be("Paracetamol 500mg");
        rejected.PrescribedQty.Should().Be(10m);
        rejected.AcceptedQty.Should().Be(0m);
    }
}
