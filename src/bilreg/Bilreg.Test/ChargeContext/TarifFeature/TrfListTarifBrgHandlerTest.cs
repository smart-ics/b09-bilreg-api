using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TrfListTarifBrgHandlerTest
{
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<IStokRepo> _stokRepoMock = new();
    private readonly Mock<IBrgPricingService> _brgPricingServiceMock = new();
    private readonly TrfListTarifBrgHandler _sut;

    public TrfListTarifBrgHandlerTest()
    {
        _sut = new TrfListTarifBrgHandler(
            _nilaiTarifRepoMock.Object,
            _stokRepoMock.Object,
            _brgPricingServiceMock.Object);
    }

    [Fact]
    public async Task Handle_WhenCalledWithTipeJaminanId_PopulatesUnitPricesInBatchAndSortsByDisplayName()
    {
        // Arrange
        const string layananId = "LYN-01";
        const string kelasId = "KLS-01";
        const string tipeTarifId = "TT-01";
        const string keyword = "paracetamol";
        const string tipeJaminanId = "TJ-01";

        var query = new TrfListTarifBrgQuery(layananId, kelasId, tipeTarifId, keyword, tipeJaminanId);

        var brgList = new List<StokView>
        {
            new("BRG-01", "Paracetamol 500mg", 100m, "TAB", "Tablet"),
            new("BRG-02", "Paracetamol Sirup", 25m, "BTL", "Botol")
        };

        var tarifList = new List<NilaiTarifView>
        {
            new("TRF-01", "Injeksi Paracetamol", new TipeTarifReff(tipeTarifId, "Umum"), new KelasReff(kelasId, "Kelas 1"), 45000m)
        };

        _stokRepoMock
            .Setup(x => x.ListData(It.Is<ILayananKey>(l => l.LayananId == layananId), keyword))
            .Returns(brgList);

        _nilaiTarifRepoMock
            .Setup(x => x.Search(
                It.Is<ILayananKey>(l => l.LayananId == layananId),
                It.Is<INilaiTarifVariant>(v => v.KelasId == kelasId && v.TipeTarifId == tipeTarifId),
                keyword))
            .Returns(tarifList);

        var priceMap = new Dictionary<string, decimal>
        {
            { "BRG-01", 1500m },
            { "BRG-02", 12500m }
        };

        _brgPricingServiceMock
            .Setup(x => x.GetUnitPrices(
                It.Is<IEnumerable<string>>(ids => ids.Contains("BRG-01") && ids.Contains("BRG-02")),
                tipeJaminanId,
                layananId))
            .Returns(priceMap);

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(3);
        // DisplayNames: "Injeksi Paracetamol", "Paracetamol 500mg", "Paracetamol Sirup"
        result[0].DisplayName.Should().Be("Injeksi Paracetamol");
        var tarifItem = result[0].Should().BeOfType<TrfListTarif>().Subject;
        tarifItem.TarifId.Should().Be("TRF-01");
        tarifItem.Harga.Should().Be(45000m);

        result[1].DisplayName.Should().Be("Paracetamol 500mg");
        var brg1 = result[1].Should().BeOfType<TrfListBrg>().Subject;
        brg1.BrgId.Should().Be("BRG-01");
        brg1.Harga.Should().Be(1500m);
        brg1.Stok.Should().Be(100m);
        brg1.Satuan.Should().Be("Tablet");

        result[2].DisplayName.Should().Be("Paracetamol Sirup");
        var brg2 = result[2].Should().BeOfType<TrfListBrg>().Subject;
        brg2.BrgId.Should().Be("BRG-02");
        brg2.Harga.Should().Be(12500m);
        brg2.Stok.Should().Be(25m);
        brg2.Satuan.Should().Be("Botol");

        _brgPricingServiceMock.Verify(x => x.GetUnitPrices(
            It.Is<IEnumerable<string>>(ids => ids.Count() == 2),
            tipeJaminanId,
            layananId), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoBrgFound_DoesNotCallPricingService()
    {
        // Arrange
        var query = new TrfListTarifBrgQuery("LYN-01", "KLS-01", "TT-01", "konsul");

        _stokRepoMock
            .Setup(x => x.ListData(It.IsAny<ILayananKey>(), It.IsAny<string>()))
            .Returns(new List<StokView>());

        _nilaiTarifRepoMock
            .Setup(x => x.Search(It.IsAny<ILayananKey>(), It.IsAny<INilaiTarifVariant>(), It.IsAny<string>()))
            .Returns(new List<NilaiTarifView>
            {
                new("TRF-01", "Konsultasi Dokter", new TipeTarifReff("TT-01", "Umum"), new KelasReff("KLS-01", "Kelas 1"), 150000m)
            });

        // Act
        var result = (await _sut.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].Should().BeOfType<TrfListTarif>();
        _brgPricingServiceMock.Verify(x => x.GetUnitPrices(
            It.IsAny<IEnumerable<string>>(),
            It.IsAny<string>(),
            It.IsAny<string>()), Times.Never);
    }
}
