using Bilreg.Application.InventoryContext.LogistikMedisFeature;
using Bilreg.Application.InventoryContext.LogistikMedisFeature.UseCases;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.InventoryContext.LogistikMedisFeature;

public class LogistikMedisStokKanvasQueryTest
{
    private readonly Mock<ILogistikMedisBffDal> _bffDalMock = new();

    private LogistikMedisStokKanvasHandler CreateSut() => new(_bffDalMock.Object);

    [Fact]
    public async Task Handle_MergesBatchesFefoBufferStokPendingAndIncoming()
    {
        // ARRANGE
        _bffDalMock.Setup(x => x.ListStokBatch("LY01")).Returns(
        [
            new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol 500mg", "TAB", "Tablet", "OBT", "B-2028", "2028-03-01", 250m, 40m),
            new LogistikMedisStokBatchRowDto("BRG01", "Paracetamol 500mg", "TAB", "Tablet", "OBT", "B-2026", "2026-12-15", 200m, 15m),
        ]);
        _bffDalMock.Setup(x => x.ListBarangLayanan("LY01")).Returns(
        [
            new LogistikMedisBarangLayananRowDto("BRG01", "Paracetamol 500mg", "TAB", "Tablet", "OBT", 30m, 100m, "R1", "Rak A"),
        ]);
        _bffDalMock.Setup(x => x.ListPendingOrderQty("LY01")).Returns(
        [
            new LogistikMedisAggQtyRowDto("BRG01", "Paracetamol 500mg", "TAB", "Tablet", "OBT", 50m),
        ]);
        _bffDalMock.Setup(x => x.ListIncomingMutasiQty("LY01")).Returns(
        [
            new LogistikMedisAggQtyRowDto("BRG01", "Paracetamol 500mg", "TAB", "Tablet", "OBT", 25m),
        ]);

        var sut = CreateSut();

        // ACT
        var result = (await sut.Handle(new LogistikMedisStokKanvasQuery("LY01"), CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        var item = result[0];
        item.BrgId.Should().Be("BRG01");
        item.BrgName.Should().Be("Paracetamol 500mg");
        item.TotalStok.Should().Be(55m);
        item.SatuanId.Should().Be("TAB");
        item.SatuanName.Should().Be("Tablet");
        item.GroupRekDkId.Should().Be("OBT");
        item.HppDefault.Should().Be(200m);
        item.MinStok.Should().Be(30m);
        item.MaxStok.Should().Be(100m);
        item.NearestTglEd.Should().Be("2026-12-15");
        item.PendingRequestQty.Should().Be(50m);
        item.IncomingMutasiQty.Should().Be(25m);

        item.Batches.Should().HaveCount(2);
        item.Batches[0].NoBatch.Should().Be("B-2026");
        item.Batches[0].TglEd.Should().Be("2026-12-15");
        item.Batches[1].NoBatch.Should().Be("B-2028");
        item.Batches[1].TglEd.Should().Be("2028-03-01");
    }

    [Fact]
    public async Task Handle_IncludesZeroStockItemsFromBarangLayananPendingAndIncoming()
    {
        // ARRANGE
        _bffDalMock.Setup(x => x.ListStokBatch("LY01")).Returns([]);
        _bffDalMock.Setup(x => x.ListBarangLayanan("LY01")).Returns(
        [
            new LogistikMedisBarangLayananRowDto("BRG02", "Spuit 3cc", "PCS", "Pieces", "BHP", 20m, 80m, "", ""),
        ]);
        _bffDalMock.Setup(x => x.ListPendingOrderQty("LY01")).Returns(
        [
            new LogistikMedisAggQtyRowDto("BRG03", "Kassa Steril", "BOX", "Box", "BHP", 10m),
        ]);
        _bffDalMock.Setup(x => x.ListIncomingMutasiQty("LY01")).Returns(
        [
            new LogistikMedisAggQtyRowDto("BRG04", "Alkohol 70%", "BTL", "Botol", "BHP", 5m),
        ]);

        var sut = CreateSut();

        // ACT
        var result = (await sut.Handle(new LogistikMedisStokKanvasQuery("LY01"), CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(3);

        var spuit = result.Single(x => x.BrgId == "BRG02");
        spuit.TotalStok.Should().Be(0m);
        spuit.MinStok.Should().Be(20m);
        spuit.MaxStok.Should().Be(80m);
        spuit.NearestTglEd.Should().BeEmpty();
        spuit.Batches.Should().BeEmpty();

        var kassa = result.Single(x => x.BrgId == "BRG03");
        kassa.TotalStok.Should().Be(0m);
        kassa.PendingRequestQty.Should().Be(10m);
        kassa.MinStok.Should().Be(0m);
        kassa.MaxStok.Should().Be(0m);

        var alkohol = result.Single(x => x.BrgId == "BRG04");
        alkohol.TotalStok.Should().Be(0m);
        alkohol.IncomingMutasiQty.Should().Be(5m);
    }

    [Fact]
    public async Task Handle_StockWithoutBarangLayananDefaultsMinAndMaxToZero_AndNormalizesSentinelEd()
    {
        // ARRANGE
        _bffDalMock.Setup(x => x.ListStokBatch("LY01")).Returns(
        [
            new LogistikMedisStokBatchRowDto("BRG05", "Kertas Thermal", "ROL", "Roll", "ATK", "", "3000-01-01", 12000m, 8m),
        ]);
        _bffDalMock.Setup(x => x.ListBarangLayanan("LY01")).Returns([]);
        _bffDalMock.Setup(x => x.ListPendingOrderQty("LY01")).Returns([]);
        _bffDalMock.Setup(x => x.ListIncomingMutasiQty("LY01")).Returns([]);

        var sut = CreateSut();

        // ACT
        var result = (await sut.Handle(new LogistikMedisStokKanvasQuery("LY01"), CancellationToken.None)).ToList();

        // ASSERT
        result.Should().HaveCount(1);
        var item = result[0];
        item.MinStok.Should().Be(0m);
        item.MaxStok.Should().Be(0m);
        item.NearestTglEd.Should().BeEmpty();
        item.Batches[0].TglEd.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_FiltersByKeywordCaseInsensitivelyOnBrgNameOrBrgId()
    {
        // ARRANGE
        _bffDalMock.Setup(x => x.ListStokBatch("LY01")).Returns(
        [
            new LogistikMedisStokBatchRowDto("OBT-001", "Amoxicillin 500mg", "TAB", "Tablet", "OBT", "B1", "2027-01-01", 500m, 10m),
            new LogistikMedisStokBatchRowDto("BHP-002", "Handscoon M", "PCS", "Pieces", "BHP", "B2", "2027-02-01", 1000m, 20m),
        ]);
        _bffDalMock.Setup(x => x.ListBarangLayanan("LY01")).Returns([]);
        _bffDalMock.Setup(x => x.ListPendingOrderQty("LY01")).Returns([]);
        _bffDalMock.Setup(x => x.ListIncomingMutasiQty("LY01")).Returns([]);

        var sut = CreateSut();

        // ACT
        var byName = (await sut.Handle(new LogistikMedisStokKanvasQuery("LY01", "amoxi"), CancellationToken.None)).ToList();
        var byCode = (await sut.Handle(new LogistikMedisStokKanvasQuery("LY01", "bhp-002"), CancellationToken.None)).ToList();

        // ASSERT
        byName.Should().ContainSingle(x => x.BrgId == "OBT-001");
        byCode.Should().ContainSingle(x => x.BrgId == "BHP-002");
    }

    [Fact]
    public async Task Handle_GivenBlankLayananId_ThrowsArgumentException()
    {
        var sut = CreateSut();

        var act = async () => await sut.Handle(new LogistikMedisStokKanvasQuery("  "), CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
    }
}
