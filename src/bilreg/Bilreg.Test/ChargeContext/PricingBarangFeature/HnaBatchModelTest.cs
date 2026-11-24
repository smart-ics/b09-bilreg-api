using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class HnaBatchModelTest
{
    private static readonly DateTime TglTrs = new(2026, 10, 17, 9, 30, 0);
    private static readonly DateTime TglBerlaku = new(2026, 10, 18);

    private static HnaBatchModel CreateSut() => HnaBatchModel.Create("PETUGAS01", TglTrs);

    private static HnaBatchItemModel CreateItem(
        string barangId = "BRG0000000001",
        decimal hnaGen = 1050m,
        DateTime? tglBerlaku = null,
        DateTime? tglExpired = null)
        => HnaBatchItemModel.Create(
            "HN0000001A",
            1,
            barangId,
            1000m,
            1025m,
            1060m,
            hnaGen,
            25m,
            tglBerlaku ?? TglBerlaku,
            tglExpired);

    [Fact]
    public void Create_ShouldGenerateLegacyHnTransactionNumber()
    {
        // Act
        var sut = CreateSut();

        // Assert
        sut.HnaBatchId.Should().StartWith("HN");
        sut.HnaBatchId.Should().NotBeNullOrWhiteSpace();
        sut.TglTrs.Should().Be(TglTrs);
        sut.PetugasId.Should().Be("PETUGAS01");
        sut.AuditTrail.Created.UserId.Should().Be("PETUGAS01");
        sut.ListItem.Should().BeEmpty();
        sut.IsVoid.Should().BeFalse();
    }

    [Fact]
    public void Create_WithBlankPetugas_ShouldThrowArgumentException()
    {
        // Act
        var act = () => HnaBatchModel.Create("   ", TglTrs);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*Petugas ID*");
    }

    [Fact]
    public void Key_ShouldPreserveGivenBatchId()
    {
        // Act
        var sut = HnaBatchModel.Key("HN0000009A");

        // Assert
        sut.HnaBatchId.Should().Be("HN0000009A");
        sut.ListItem.Should().BeEmpty();
    }

    [Fact]
    public void AddItem_ShouldAppendOrderedItemsLinkedToBatch()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1050m, 25m, TglBerlaku);
        sut.AddItem("BRG0000000002", 900m, 950m, 980m, 970m, 10m, TglBerlaku);

        // Assert
        sut.ListItem.Should().HaveCount(2);
        sut.ListItem[0].NoUrut.Should().Be(1);
        sut.ListItem[0].HnaBatchId.Should().Be(sut.HnaBatchId);
        sut.ListItem[1].NoUrut.Should().Be(2);
        sut.ListItem[1].HnaBatchId.Should().Be(sut.HnaBatchId);
    }

    [Fact]
    public void AddItem_ShouldCarryHnaVariationsAndEffectiveDates()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1040m, 12.5m, TglBerlaku);

        // Assert
        var item = sut.ListItem.Single();
        item.HnaAkhir.Should().Be(1000m);
        item.HnaRata.Should().Be(1025m);
        item.HnaMax.Should().Be(1060m);
        item.HnaGen.Should().Be(1040m);
        item.DiskonGen.Should().Be(12.5m);
        item.TglBerlaku.Should().Be(TglBerlaku);
        item.TglExpired.Should().Be(HnaBatchItemModel.TglExpiredDefault);
        item.TglExpired.Should().Be(AppConst.DEF_DATE);
        item.IsActive.Should().BeTrue();
    }

    [Fact]
    public void AddItem_DuplicateBarang_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1050m, 25m, TglBerlaku);

        // Act
        var act = () => sut.AddItem("BRG0000000001", 1m, 1m, 1m, 1m, 0m, TglBerlaku);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*duplikasi*");
    }

    [Fact]
    public void CreateItem_WithNegativeHnaGen_ShouldThrowArgumentException()
    {
        // Act
        var act = () => CreateItem(hnaGen: -1m);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*HnaGen*negatif*");
    }

    [Fact]
    public void CreateItem_WithNegativeHnaAkhir_ShouldThrowArgumentException()
    {
        // Act
        var act = () => HnaBatchItemModel.Create(
            "HN0000001A", 1, "BRG0000000001", -1m, 0m, 0m, 0m, 0m, TglBerlaku);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*HnaAkhir*negatif*");
    }

    [Fact]
    public void CreateItem_WithDefaultTglBerlaku_ShouldThrowArgumentException()
    {
        // Act
        var act = () => CreateItem(tglBerlaku: default(DateTime));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*TglBerlaku*tanggal berlaku*");
    }

    [Fact]
    public void CreateItem_WithActiveSentinelAsTglBerlaku_ShouldThrowArgumentException()
    {
        // Act
        var act = () => CreateItem(tglBerlaku: HnaBatchItemModel.TglExpiredDefault);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*TglBerlaku*tanggal berlaku*");
    }

    [Fact]
    public void CreateItem_WithTglExpiredBeforeTglBerlaku_ShouldThrowArgumentException()
    {
        // Act
        var act = () => CreateItem(tglExpired: TglBerlaku.AddDays(-1));

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*TglExpired*tidak boleh lebih awal*");
    }

    [Fact]
    public void CreateItem_WithHistoricalExpiredDate_ShouldNotBeActive()
    {
        // Act
        var item = CreateItem(tglExpired: TglBerlaku.AddDays(30));

        // Assert
        item.IsActive.Should().BeFalse();
        item.TglExpired.Should().Be(TglBerlaku.AddDays(30));
    }

    [Fact]
    public void CreateItem_WithZeroNoUrut_ShouldThrowArgumentException()
    {
        // Act
        var act = () => HnaBatchItemModel.Create(
            "HN0000001A", 0, "BRG0000000001", 0m, 0m, 0m, 0m, 0m, TglBerlaku);

        // Assert
        act.Should().Throw<ArgumentException>().WithMessage("*NoUrut*");
    }

    [Fact]
    public void Validate_WithoutItems_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*minimal 1 item barang*");
    }

    [Fact]
    public void Validate_WithItems_ShouldPass()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1050m, 25m, TglBerlaku);

        // Act
        var act = () => sut.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Void_ShouldVoidAuditAndItems()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1050m, 25m, TglBerlaku);
        var voidTime = new DateTime(2026, 10, 20, 14, 0, 0);

        // Act
        sut.Void("USER02", voidTime);

        // Assert
        sut.IsVoid.Should().BeTrue();
        sut.AuditTrail.Voided.UserId.Should().Be("USER02");
        sut.ListItem.Single().IsVoid.Should().BeTrue();
        sut.ListItem.Single().IsActive.Should().BeFalse();
    }

    [Fact]
    public void Alterations_WhenVoided_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddItem("BRG0000000001", 1000m, 1025m, 1060m, 1050m, 25m, TglBerlaku);
        sut.Void("USER02", new DateTime(2026, 10, 20, 14, 0, 0));

        // Act & Assert
        sut.Invoking(x => x.AddItem("BRG0000000002", 1m, 1m, 1m, 1m, 0m, TglBerlaku))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.Modify("USER03"))
            .Should().Throw<InvalidOperationException>();
    }
}
