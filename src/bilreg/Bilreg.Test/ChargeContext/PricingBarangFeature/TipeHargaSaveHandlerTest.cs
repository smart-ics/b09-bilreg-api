using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

public class TipeHargaSaveHandlerTest
{
    private readonly Mock<ITipeHargaRepo> _tipeHargaRepoMock = new();

    private TipeHargaSaveHandler CreateHandler() => new(_tipeHargaRepoMock.Object);

    [Fact]
    public async Task UT1_GivenNewCode_WhenSave_ThenInsertsWithDispensingFees()
    {
        _tipeHargaRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeHargaKey>()))
            .Returns(MayBe<TipeHargaModel>.None);
        TipeHargaModel? saved = null;
        _tipeHargaRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TipeHargaModel>()))
            .Callback<TipeHargaModel>(m => saved = m);

        var cmd = new TipeHargaSaveCommand("UM", "Pasien Umum", 25m, 1500m, 3000m, 11m, 5m, true);

        var response = await CreateHandler().Handle(cmd, CancellationToken.None);

        response.TipeHargaId.Should().Be("UM");
        response.IsNew.Should().BeTrue();

        saved.Should().NotBeNull();
        saved!.TipeHargaName.Should().Be("Pasien Umum");
        saved.DefaultMarginPersen.Should().Be(25m);
        saved.BiayaPerObat_r.Should().Be(1500m);
        saved.BiayaPerResep_R.Should().Be(3000m);
        saved.TaxPersen.Should().Be(11m);
        saved.DiskonPersen.Should().Be(5m);
        saved.IsActive.Should().BeTrue();
        _tipeHargaRepoMock.Verify(x => x.SaveChanges(It.IsAny<TipeHargaModel>()), Times.Once);
    }

    [Fact]
    public async Task UT2_GivenExistingCode_WhenSave_ThenUpdatesExistingRecord()
    {
        var existing = TipeHargaModel.Create("UM", "Pasien Umum", 10m, 100m, 200m, 0m, 0m, true);
        _tipeHargaRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITipeHargaKey>()))
            .Returns(MayBe.From(existing));
        TipeHargaModel? saved = null;
        _tipeHargaRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<TipeHargaModel>()))
            .Callback<TipeHargaModel>(m => saved = m);

        var cmd = new TipeHargaSaveCommand("UM", "Pasien Umum Baru", 25m, 1500m, 3000m, 11m, 5m, false);

        var response = await CreateHandler().Handle(cmd, CancellationToken.None);

        response.IsNew.Should().BeFalse();
        saved.Should().NotBeNull();
        saved!.TipeHargaName.Should().Be("Pasien Umum Baru");
        saved.BiayaPerObat_r.Should().Be(1500m);
        saved.BiayaPerResep_R.Should().Be(3000m);
        saved.IsActive.Should().BeFalse();
    }

    [Fact]
    public async Task UT3_GivenNegativeFeeComponent_WhenSave_ThenThrows()
    {
        var cmd = new TipeHargaSaveCommand("UM", "Pasien Umum", 25m, -1m, 3000m, 11m, 5m, true);

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>().WithMessage("*negatif*");
        _tipeHargaRepoMock.Verify(x => x.SaveChanges(It.IsAny<TipeHargaModel>()), Times.Never);
    }

    [Fact]
    public async Task UT4_GivenBlankName_WhenSave_ThenThrows()
    {
        var cmd = new TipeHargaSaveCommand("UM", " ", 25m, 1500m, 3000m, 11m, 5m, true);

        var act = async () => await CreateHandler().Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _tipeHargaRepoMock.Verify(x => x.SaveChanges(It.IsAny<TipeHargaModel>()), Times.Never);
    }
}
