using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Unit tests handler <see cref="CalculateItemPriceHandler"/> (ARCHITECTURE TD-06).
/// Memvalidasi delegasi ke <see cref="IItemPricingCalculator"/> dan pengembalian DTO
/// kalkulasi harga terurai lengkap, termasuk propagasi <see cref="HnaNotFoundException"/>.
/// </summary>
public class CalculateItemPriceHandlerTest
{
    private const string BarangId = "BRG-001";
    private const string TipeHargaId = "UM";
    private static readonly DateTime Tgl = new(2026, 10, 17);

    private readonly Mock<IItemPricingCalculator> _calculatorMock = new();

    private static ItemPriceCalculationResultDto Dto() => new(
        BarangId, TipeHargaId, Tgl, 1, string.Empty,
        1m, 1100m, 25m, 0m, false, false, 1300m, 1375m, 1300m, 0m, 0m, 1500m, 3000m);

    private CalculateItemPriceHandler CreateSut() => new(_calculatorMock.Object);

    [Fact]
    public async Task Handle_DelegatesToCalculator_AndReturnsFullBreakdown()
    {
        var expected = Dto();
        _calculatorMock
            .Setup(x => x.CalculateAsync(BarangId, TipeHargaId, Tgl, 1, string.Empty, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var result = await CreateSut().Handle(
            new CalculateItemPriceQuery(BarangId, TipeHargaId, Tgl),
            CancellationToken.None);

        result.Should().BeSameAs(expected);
        result.BiayaPerObat_r.Should().Be(1500m);
        result.BiayaPerResep_R.Should().Be(3000m);
        result.HnaApplied.Should().Be(1100m);
        _calculatorMock.Verify(
            x => x.CalculateAsync(BarangId, TipeHargaId, Tgl, 1, string.Empty, It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_PassesQtyAndSatuanToCalculator()
    {
        _calculatorMock
            .Setup(x => x.CalculateAsync(BarangId, TipeHargaId, Tgl, 3, "BOX", It.IsAny<CancellationToken>()))
            .ReturnsAsync(Dto());

        await CreateSut().Handle(
            new CalculateItemPriceQuery(BarangId, TipeHargaId, Tgl, 3, "BOX"),
            CancellationToken.None);

        _calculatorMock.Verify(
            x => x.CalculateAsync(BarangId, TipeHargaId, Tgl, 3, "BOX", It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_HnaNotFound_Propagates()
    {
        _calculatorMock
            .Setup(x => x.CalculateAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<DateTime>(),
                It.IsAny<int>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HnaNotFoundException(BarangId));

        var act = async () => await CreateSut().Handle(
            new CalculateItemPriceQuery(BarangId, TipeHargaId, Tgl),
            CancellationToken.None);

        await act.Should().ThrowAsync<HnaNotFoundException>();
    }
}
