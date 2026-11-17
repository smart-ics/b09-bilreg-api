using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TarifFeature.UseCases;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class NilaiTarifGetHandlerTest
{
    private readonly Mock<INilaiTarifRepo> _nilaiTarifRepoMock = new();
    private readonly Mock<IKomponenRepo> _komponenRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITarifBarangRepo> _tarifBarangRepoMock = new();
    private readonly Mock<IBrgPricingService> _brgPricingServiceMock = new();
    private readonly NilaiTarifGetHandler _sut;

    public NilaiTarifGetHandlerTest()
    {
        _sut = new NilaiTarifGetHandler(
            _nilaiTarifRepoMock.Object,
            _komponenRepoMock.Object,
            _tarifRepoMock.Object,
            _tarifBarangRepoMock.Object,
            _brgPricingServiceMock.Object);
    }

    private static NilaiTarifType CreateSampleNilaiTarif(string tarifId = "TRF-01")
    {
        var kompReff = new KomponenReff("KOMP-01", "Jasa Dokter");
        var listKomp = new List<NilaiTarifKomponenType>
        {
            new(1, kompReff, 100000m)
        };
        return new NilaiTarifType(
            "NT-01",
            tarifId,
            "Tindakan Test",
            new TipeTarifReff("TT-01", "Umum"),
            new KelasReff("KLS-01", "Kelas 1"),
            100000m,
            listKomp);
    }

    private static TarifType CreateSampleTarif(string tarifId = "TRF-01", bool isChargeBarang = false)
    {
        return new TarifType(
            tarifId,
            "Tindakan Test",
            GroupTarifType.Default,
            GroupTarifDkType.Default,
            JenisTarifType.Default,
            RekapCetakType.Default.ToReff(),
            isChargeBarang);
    }

    [Fact]
    public async Task Handle_WhenNilaiTarifNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var query = new TrfGetNilaiTarifQuery("TRF-NONE", "TT-01", "KLS-01");
        _nilaiTarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<INilaiTarifCompositKey>()))
            .Returns(MayBe<NilaiTarifType>.None);

        // Act
        var act = () => _sut.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*TRF-NONE not found*");
    }

    [Fact]
    public async Task Handle_WhenTarifIsChargeBarangFalse_ReturnsBOMWithZeroPrices()
    {
        // Arrange
        const string tarifId = "TRF-01";
        var query = new TrfGetNilaiTarifQuery(tarifId, "TT-01", "KLS-01");
        var nilaiTarif = CreateSampleNilaiTarif(tarifId);
        var tarif = CreateSampleTarif(tarifId, isChargeBarang: false);

        var bomItems = new List<TarifBarangType>
        {
            new(tarifId, 1, "BRG-01", "Spuit 3cc", "PCS", "Pcs", 2m),
            new(tarifId, 2, "BRG-02", "Alkohol Swab", "PCS", "Pcs", 1m)
        };

        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns(new List<KomponenType>());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(MayBe.From(tarif));
        _tarifBarangRepoMock.Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(bomItems);

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsChargeBarang.Should().BeFalse();
        response.ListBarang.Should().HaveCount(2);

        var item1 = response.ListBarang.First();
        item1.BrgId.Should().Be("BRG-01");
        item1.Qty.Should().Be(2m);
        item1.NoUrut.Should().Be(1);
        item1.IsChargeBarang.Should().BeFalse();
        item1.HargaSatuan.Should().Be(0m);
        item1.SubTotalPerTindakan.Should().Be(0m);

        var item2 = response.ListBarang.Last();
        item2.BrgId.Should().Be("BRG-02");
        item2.Qty.Should().Be(1m);
        item2.NoUrut.Should().Be(2);
        item2.IsChargeBarang.Should().BeFalse();
        item2.HargaSatuan.Should().Be(0m);
        item2.SubTotalPerTindakan.Should().Be(0m);

        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice(
            It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTarifIsChargeBarangTrue_ReturnsBOMWithCalculatedPrices()
    {
        // Arrange
        const string tarifId = "TRF-01";
        const string tipeJaminanId = "TJ-BPJS";
        const string layananId = "LYN-BEDAH";
        var query = new TrfGetNilaiTarifQuery(tarifId, "TT-01", "KLS-01", tipeJaminanId, layananId);
        var nilaiTarif = CreateSampleNilaiTarif(tarifId);
        var tarif = CreateSampleTarif(tarifId, isChargeBarang: true);

        var bomItems = new List<TarifBarangType>
        {
            new(tarifId, 1, "BRG-01", "Spuit 3cc", "PCS", "Pcs", 2m),
            new(tarifId, 2, "BRG-02", "Betadine 60ml", "BTL", "Botol", 1m)
        };

        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns(new List<KomponenType>());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(MayBe.From(tarif));
        _tarifBarangRepoMock.Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(bomItems);

        var calcItem1 = new BrgPriceCalcResult(
            new TipeBrgReff("TB-01", "Obat/Alkes"),
            new NilaiItemType(15000m, 0m, 0m, 30000m, 0m, 0m, 0m, 0m, 0m, 30000m));
        var calcItem2 = new BrgPriceCalcResult(
            new TipeBrgReff("TB-01", "Obat/Alkes"),
            new NilaiItemType(25000m, 0m, 0m, 25000m, 0m, 0m, 0m, 0m, 0m, 25000m));

        _brgPricingServiceMock
            .Setup(x => x.CalculateItemPrice("BRG-01", 2m, tipeJaminanId, layananId))
            .Returns(calcItem1);
        _brgPricingServiceMock
            .Setup(x => x.CalculateItemPrice("BRG-02", 1m, tipeJaminanId, layananId))
            .Returns(calcItem2);

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsChargeBarang.Should().BeTrue();
        response.ListBarang.Should().HaveCount(2);

        var item1 = response.ListBarang.First();
        item1.BrgId.Should().Be("BRG-01");
        item1.Qty.Should().Be(2m);
        item1.IsChargeBarang.Should().BeTrue();
        item1.HargaSatuan.Should().Be(15000m);
        item1.SubTotalPerTindakan.Should().Be(30000m); // 15000 * 2

        var item2 = response.ListBarang.Last();
        item2.BrgId.Should().Be("BRG-02");
        item2.Qty.Should().Be(1m);
        item2.IsChargeBarang.Should().BeTrue();
        item2.HargaSatuan.Should().Be(25000m);
        item2.SubTotalPerTindakan.Should().Be(25000m); // 25000 * 1

        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice("BRG-01", 2m, tipeJaminanId, layananId), Times.Once);
        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice("BRG-02", 1m, tipeJaminanId, layananId), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenBOMItemsUnsorted_ReturnsSortedByNoUrut()
    {
        // Arrange
        const string tarifId = "TRF-01";
        var query = new TrfGetNilaiTarifQuery(tarifId, "TT-01", "KLS-01");
        var nilaiTarif = CreateSampleNilaiTarif(tarifId);
        var tarif = CreateSampleTarif(tarifId, isChargeBarang: false);

        // Out-of-order items: 3, 1, 2
        var bomItems = new List<TarifBarangType>
        {
            new(tarifId, 3, "BRG-03", "Kasa", "PCS", "Pcs", 5m),
            new(tarifId, 1, "BRG-01", "Spuit", "PCS", "Pcs", 1m),
            new(tarifId, 2, "BRG-02", "Needle", "PCS", "Pcs", 1m)
        };

        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns(new List<KomponenType>());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(MayBe.From(tarif));
        _tarifBarangRepoMock.Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(bomItems);

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.ListBarang.Select(x => x.NoUrut).Should().ContainInOrder(1, 2, 3);
        response.ListBarang.Select(x => x.BrgId).Should().ContainInOrder("BRG-01", "BRG-02", "BRG-03");
    }

    [Fact]
    public async Task Handle_WhenTipeJaminanIdAndLayananIdOmitted_FallsBackToEmptyStrings()
    {
        // Arrange
        const string tarifId = "TRF-01";
        // Call without optional params -> defaults to ""
        var query = new TrfGetNilaiTarifQuery(tarifId, "TT-01", "KLS-01");
        query.TipeJaminanId.Should().BeEmpty();
        query.LayananId.Should().BeEmpty();

        var nilaiTarif = CreateSampleNilaiTarif(tarifId);
        var tarif = CreateSampleTarif(tarifId, isChargeBarang: true);

        var bomItems = new List<TarifBarangType>
        {
            new(tarifId, 1, "BRG-01", "Spuit 3cc", "PCS", "Pcs", 1m)
        };

        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns(new List<KomponenType>());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(MayBe.From(tarif));
        _tarifBarangRepoMock.Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == tarifId))).Returns(bomItems);

        var calcItem = new BrgPriceCalcResult(
            new TipeBrgReff("TB-01", "Obat/Alkes"),
            new NilaiItemType(10000m, 0m, 0m, 10000m, 0m, 0m, 0m, 0m, 0m, 10000m));

        _brgPricingServiceMock
            .Setup(x => x.CalculateItemPrice("BRG-01", 1m, "", ""))
            .Returns(calcItem);

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.IsChargeBarang.Should().BeTrue();
        _brgPricingServiceMock.Verify(x => x.CalculateItemPrice("BRG-01", 1m, "", ""), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTarifRepoReturnsNone_DefaultsIsChargeBarangToFalse()
    {
        // Arrange
        const string tarifId = "TRF-01";
        var query = new TrfGetNilaiTarifQuery(tarifId, "TT-01", "KLS-01");
        var nilaiTarif = CreateSampleNilaiTarif(tarifId);

        _nilaiTarifRepoMock.Setup(x => x.LoadEntity(query)).Returns(MayBe.From(nilaiTarif));
        _komponenRepoMock.Setup(x => x.ListData(It.IsAny<IEnumerable<IKomponenKey>>())).Returns(new List<KomponenType>());
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>())).Returns(MayBe<TarifType>.None);
        _tarifBarangRepoMock.Setup(x => x.ListData(It.IsAny<ITarifKey>())).Returns(new List<TarifBarangType>());

        // Act
        var response = await _sut.Handle(query, CancellationToken.None);

        // Assert
        response.IsChargeBarang.Should().BeFalse();
        response.ListBarang.Should().BeEmpty();
    }
}
