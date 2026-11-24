using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class PricingBarangUpdateHandlerTest
{
    private readonly Mock<IPricingBarangRepo> _repoMock = new();

    private PricingBarangUpdateHandler CreateHandler() =>
        new(_repoMock.Object, TestTglJamProvider.Instance);

    private static PricingBarangVariantInput Variant(
        string tipeHargaId,
        decimal hargaJualFix = 0m,
        decimal marginPersen = 25m,
        decimal biayaTambahanItem = 0m,
        decimal taxPersen = 0m,
        decimal diskonDefaultPersen = 0m,
        bool isActive = true) =>
        new(tipeHargaId, hargaJualFix, marginPersen, biayaTambahanItem, taxPersen, diskonDefaultPersen, isActive);

    [Fact]
    public async Task UT1_GivenNewBarang_WhenUpdate_ThenCreatesAndSavesWithVariants()
    {
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()))
            .Returns(MayBe<PricingBarangModel>.None);
        PricingBarangModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<PricingBarangModel>()))
            .Callback<PricingBarangModel>(m => saved = m);

        var cmd = new PricingBarangUpdateCommand(
            "BRG1",
            1300m,
            MetodeGenHnaEnum.Terakhir,
            [Variant("UM"), Variant("JM", marginPersen: 20m)],
            "user1");

        var response = await CreateHandler().Handle(cmd, CancellationToken.None);

        response.BarangId.Should().Be("BRG1");
        response.IsNew.Should().BeTrue();
        response.VariantCount.Should().Be(2);
        response.HetNominal.Should().Be(1300m);

        saved.Should().NotBeNull();
        saved!.HetNominal.Should().Be(1300m);
        saved.MetodeGenHna.Should().Be(MetodeGenHnaEnum.Terakhir);
        saved.Variants.Should().HaveCount(2);
        saved.AuditTrail.Created.UserId.Should().Be("user1");
        saved.AuditTrail.Modified.UserId.Should().BeEmpty();
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<PricingBarangModel>()), Times.Once);
    }

    [Fact]
    public async Task UT2_GivenExistingBarang_WhenUpdate_ThenUpdatesHeaderVariantAndAudit()
    {
        var existing = PricingBarangModel
            .Create("BRG1", 1000m, MetodeGenHnaEnum.Terakhir, "creator", TestTglJamProvider.Instance.Now)
            .AddOrUpdateVariant("UM", 0m, 10m, 0m, 0m, 0m, true);
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()))
            .Returns(MayBe.From(existing));
        PricingBarangModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<PricingBarangModel>()))
            .Callback<PricingBarangModel>(m => saved = m);

        var cmd = new PricingBarangUpdateCommand(
            "BRG1",
            1500m,
            MetodeGenHnaEnum.RataRata,
            [Variant("UM", 1200m, 30m, 500m, 11m, 5m, isActive: false)],
            "editor");

        var response = await CreateHandler().Handle(cmd, CancellationToken.None);

        response.IsNew.Should().BeFalse();
        response.VariantCount.Should().Be(1);

        saved.Should().NotBeNull();
        saved!.HetNominal.Should().Be(1500m);
        saved.MetodeGenHna.Should().Be(MetodeGenHnaEnum.RataRata);

        var variant = saved.Variants.Single();
        variant.TipeHargaId.Should().Be("UM");
        variant.HargaJualFix.Should().Be(1200m);
        variant.MarginPersen.Should().Be(30m);
        variant.BiayaTambahanItem.Should().Be(500m);
        variant.TaxPersen.Should().Be(11m);
        variant.DiskonDefaultPersen.Should().Be(5m);
        variant.IsActive.Should().BeFalse();

        saved.AuditTrail.Created.UserId.Should().Be("creator");
        saved.AuditTrail.Modified.UserId.Should().Be("editor");
    }

    [Fact]
    public async Task UT3_GivenNegativeHet_WhenUpdate_ThenThrows()
    {
        var cmd = new PricingBarangUpdateCommand(
            "BRG1", -1m, MetodeGenHnaEnum.Terakhir, [Variant("UM")], "user1");

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*HET*tidak boleh negatif*");
        _repoMock.Verify(x => x.SaveChanges(It.IsAny<PricingBarangModel>()), Times.Never);
    }

    [Fact]
    public async Task UT4_GivenBlankBarangId_WhenUpdate_ThenThrows()
    {
        var cmd = new PricingBarangUpdateCommand(
            " ", 100m, MetodeGenHnaEnum.Terakhir, [Variant("UM")], "user1");

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _repoMock.Verify(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()), Times.Never);
    }

    [Fact]
    public async Task UT5_GivenNoVariants_WhenUpdate_ThenSavesHeaderOnly()
    {
        _repoMock
            .Setup(x => x.LoadEntity(It.IsAny<IPricingBarangKey>()))
            .Returns(MayBe<PricingBarangModel>.None);
        PricingBarangModel? saved = null;
        _repoMock
            .Setup(x => x.SaveChanges(It.IsAny<PricingBarangModel>()))
            .Callback<PricingBarangModel>(m => saved = m);

        var cmd = new PricingBarangUpdateCommand(
            "BRG1", 500m, MetodeGenHnaEnum.Terbesar, null!, "user1");

        var response = await CreateHandler().Handle(cmd, CancellationToken.None);

        response.VariantCount.Should().Be(0);
        saved!.Variants.Should().BeEmpty();
    }
}
