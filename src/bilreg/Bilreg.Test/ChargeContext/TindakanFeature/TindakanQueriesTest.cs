using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanQueriesTest
{
    private readonly Mock<ITindakanRepo> _tdkRepoMock = new();

    private static TindakanModel CreateSampleModel(string tindakanId = "TDK-001", string pakaiBedId = "BED-01", int qty = 3)
    {
        var regReff = new RegReff("REG01", "PAS01", "Pasien A");
        var layananReff = new LayananReff("LAY01", "Layanan A");
        var kelasReff = new KelasReff("K1", "Kelas 1");
        var tipeTarifReff = new TipeTarifReff("TT", "Umum");
        var tarifReff = new TarifReff("TRF01", "Tarif Tindakan");
        var komponen = new TindakanKomponenWithoutPpaType(
            new KomponenReff("K1", "Jasa Sarana"), 1, 50000m, qty, 50000m * qty);
        var auditTrail = AuditTrailType.Create("USR01", new DateTime(2026, 10, 20, 10, 0, 0));

        return new TindakanModel(
            tindakanId, new DateTime(2026, 10, 20, 10, 0, 0), "ORD001",
            regReff, layananReff, kelasReff, tipeTarifReff, tarifReff,
            [komponen], auditTrail, pakaiBedId, qty);
    }

    #region TindakanGetQuery Tests

    [Fact]
    public async Task TindakanGetQuery_WhenEntityExists_ShouldMapAllProperties()
    {
        // Arrange
        var model = CreateSampleModel("TDK-100", "BED-RANAP-05", 4);
        _tdkRepoMock.Setup(x => x.LoadEntity(It.Is<ITindakanKey>(k => k.TindakanId == "TDK-100")))
            .Returns(MayBe.From(model));

        var handler = new TindakanGetHandler(_tdkRepoMock.Object);
        var query = new TindakanGetQuery("TDK-100");

        // Act
        var result = await handler.Handle(query, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TindakanId.Should().Be("TDK-100");
        result.TindakanDate.Should().Be("2026-10-20 10:00:00");
        result.PakaiBedId.Should().Be("BED-RANAP-05");
        result.Qty.Should().Be(4);
        result.Total.Should().Be(200000m); // 50000 * 4
        result.Reg.RegId.Should().Be("REG01");
        result.Layanan.LayananId.Should().Be("LAY01");
        result.Kelas.KelasId.Should().Be("K1");
        result.TipeTarif.TipeTarifId.Should().Be("TT");
        result.Tarif.TarifId.Should().Be("TRF01");
        result.ListKomponen.Should().HaveCount(1);
        result.AuditTrail.Created.UserId.Should().Be("USR01");
        result.OrderTindakanId.Should().Be("ORD001");
    }

    [Fact]
    public async Task TindakanGetQuery_WhenEntityNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        _tdkRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITindakanKey>()))
            .Returns(MayBe<TindakanModel>.None);

        var handler = new TindakanGetHandler(_tdkRepoMock.Object);
        var query = new TindakanGetQuery("TDK-MISSING");

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Tindakan TDK-MISSING not found*");
    }

    #endregion

    #region TindakanListQuery Tests

    [Fact]
    public async Task TindakanListQuery_WithRegIdOnly_ShouldReturnAllItems()
    {
        // Arrange
        var view1 = new TindakanView(
            "TDK-01", new DateTime(2026, 10, 20), "ORD01",
            new RegReff("REG01", "P1", "Pasien 1"),
            new LayananReff("L1", "Layanan 1"),
            new TarifReff("TRF1", "Tarif 1"),
            "BED-01");
        var view2 = new TindakanView(
            "TDK-02", new DateTime(2026, 10, 20), "ORD02",
            new RegReff("REG01", "P1", "Pasien 1"),
            new LayananReff("L1", "Layanan 1"),
            new TarifReff("TRF2", "Tarif 2"),
            "BED-02");

        _tdkRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG01")))
            .Returns([view1, view2]);

        var handler = new TindakanListHandler(_tdkRepoMock.Object);
        var query = new TindakanListQuery("REG01");

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(2);
        result[0].TindakanId.Should().Be("TDK-01");
        result[0].PakaiBedId.Should().Be("BED-01");
        result[1].TindakanId.Should().Be("TDK-02");
        result[1].PakaiBedId.Should().Be("BED-02");
    }

    [Fact]
    public async Task TindakanListQuery_WithRegIdAndPakaiBedId_ShouldFilterByBed()
    {
        // Arrange
        var view1 = new TindakanView(
            "TDK-01", new DateTime(2026, 10, 20), "ORD01",
            new RegReff("REG01", "P1", "Pasien 1"),
            new LayananReff("L1", "Layanan 1"),
            new TarifReff("TRF1", "Tarif 1"),
            "BED-01");
        var view2 = new TindakanView(
            "TDK-02", new DateTime(2026, 10, 20), "ORD02",
            new RegReff("REG01", "P1", "Pasien 1"),
            new LayananReff("L1", "Layanan 1"),
            new TarifReff("TRF2", "Tarif 2"),
            "BED-02");

        _tdkRepoMock.Setup(x => x.ListData(It.Is<IRegKey>(k => k.RegId == "REG01")))
            .Returns([view1, view2]);

        var handler = new TindakanListHandler(_tdkRepoMock.Object);
        var query = new TindakanListQuery("REG01", "BED-01");

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TindakanId.Should().Be("TDK-01");
        result[0].PakaiBedId.Should().Be("BED-01");
    }

    [Fact]
    public async Task TindakanListQuery_WhenRegIdEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var handler = new TindakanListHandler(_tdkRepoMock.Object);
        var query = new TindakanListQuery("");

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion

    #region TdkListTindakanJualQuery Tests

    [Fact]
    public async Task TdkListTindakanJualQuery_ShouldReturnItemsWithQtyAndPakaiBedId()
    {
        // Arrange
        var jualView = new TindakanJualView(
            "TDK-01", new DateTime(2026, 10, 20, 10, 0, 0), "ORD01",
            new RegReff("REG01", "PAS01", "Pasien A"),
            new LayananReff("L01", "Layanan A"),
            "TRF01", "Tarif Tindakan",
            5, 250000m, "TINDAKAN", "BED-03");

        _tdkRepoMock.Setup(x => x.ListDataTdkJual(It.Is<IRegKey>(k => k.RegId == "REG01")))
            .Returns([jualView]);

        var handler = new TdkListTindakanJualHandler(_tdkRepoMock.Object);
        var query = new TdkListTindakanJualQuery("REG01");

        // Act
        var result = (await handler.Handle(query, CancellationToken.None)).ToList();

        // Assert
        result.Should().HaveCount(1);
        result[0].TransaksiId.Should().Be("TDK-01");
        result[0].TransaksiDate.Should().Be("2026-10-20 10:00:00");
        result[0].Qty.Should().Be(5);
        result[0].PakaiBedId.Should().Be("BED-03");
        result[0].TotalNilai.Should().Be(250000m);
        result[0].Diskripsi.DiskripsiId.Should().Be("TRF01");
        result[0].Diskripsi.DiskripsiName.Should().Be("Tarif Tindakan");
    }

    [Fact]
    public async Task TdkListTindakanJualQuery_WhenRegIdEmpty_ShouldThrowArgumentException()
    {
        // Arrange
        var handler = new TdkListTindakanJualHandler(_tdkRepoMock.Object);
        var query = new TdkListTindakanJualQuery("");

        // Act
        var act = async () => await handler.Handle(query, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    #endregion
}
