using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.InventoryContext.PakaiBrgFeature;
using Bilreg.Application.InventoryContext.PakaiBrgFeature.UseCases;
using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.InventoryContext.PakaiBrgFeature;
using FluentAssertions;
using MediatR;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext;

public class IgdStockServiceTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITarifBarangRepo> _tarifBarangRepoMock = new();
    private readonly Mock<IBrgRepo> _brgRepoMock = new();
    private readonly Mock<ILegacyStockReadPort> _legacyStockReadPortMock = new();
    private readonly Mock<ITindakanStokPendingRepo> _tindakanStokPendingRepoMock = new();

    private readonly IgdStockService _sut;

    public IgdStockServiceTest()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PakaiBrgCreateResponse("PK0001", new DateTime(2026, 11, 26, 10, 0, 0), 10000m, 1));

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgVoidCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PakaiBrgVoidCommand cmd, CancellationToken _) =>
                new PakaiBrgVoidResponse(cmd.PakaiBrgId, true, new DateTime(2026, 11, 26, 11, 0, 0), cmd.AlasanVoid));

        _sut = new IgdStockService(
            _mediatorMock.Object,
            _tarifRepoMock.Object,
            _tarifBarangRepoMock.Object,
            _brgRepoMock.Object,
            _legacyStockReadPortMock.Object,
            _tindakanStokPendingRepoMock.Object);
    }

    private static Mock<IBrg> CreateMockBrg(
        string brgId,
        string brgName,
        string satuanKecilId,
        params (string SatuanId, int Konversi)[] listSatuan)
    {
        var brgMock = new Mock<IBrg>();
        brgMock.Setup(b => b.BrgId).Returns(brgId);
        brgMock.Setup(b => b.BrgName).Returns(brgName);
        brgMock.Setup(b => b.SatuanKecil).Returns(new SatuanType(satuanKecilId, satuanKecilId));

        var satuanList = listSatuan
            .Select(s => new BrgSatuanType(new SatuanType(s.SatuanId, s.SatuanId), s.Konversi))
            .ToList();
        brgMock.Setup(b => b.ListSatuan).Returns(satuanList);

        return brgMock;
    }

    private static LegacyStockBalanceReadModel CreateStockBalance(
        string brgId,
        string layananId,
        decimal qtySisa)
    {
        return new LegacyStockBalanceReadModel(
            LegacyStokId: "STK01",
            BrgId: brgId,
            BrgMasukReffId: "DO01",
            LayananId: layananId,
            TglEd: new DateTime(2027, 1, 1),
            NoBatch: "B01",
            QtySisa: qtySisa,
            Hpp: 5000m,
            TglMasuk: new DateTime(2026, 1, 1));
    }

    #region DeductBhpAsync

    [Fact]
    public async Task DeductBhpAsync_WhenStockSufficient_SendsPakaiBrgCreateAndReturnsId()
    {
        PakaiBrgCreateCommand? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PakaiBrgCreateResponse>, CancellationToken>((req, _) => capturedCmd = (PakaiBrgCreateCommand)req)
            .ReturnsAsync(new PakaiBrgCreateResponse("PK-IGD-01", new DateTime(2026, 11, 26), 15000m, 1));

        var pakaiBrgId = await _sut.DeductBhpAsync("IGD01", "BRG01", 3m, "USER1", "BHP-IGD:TKI01");

        pakaiBrgId.Should().Be("PK-IGD-01");
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LayananId.Should().Be("IGD01");
        capturedCmd.UserId.Should().Be("USER1");
        capturedCmd.Keterangan.Should().Be("BHP-IGD:TKI01");
        capturedCmd.ListItem.Should().ContainSingle(i => i.BrgId == "BRG01" && i.Qty == 3m);
    }

    [Fact]
    public async Task DeductBhpAsync_WhenStockInsufficient_PropagatesException()
    {
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Stok barang 'Spuit' (BRG01) tidak mencukupi di layanan 'IGD'. Kekurangan: 2."));

        var act = () => _sut.DeductBhpAsync("IGD01", "BRG01", 5m, "USER1", "BHP-IGD:TKI01");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*tidak mencukupi*");
    }

    #endregion

    #region ProcessTindakanBomAsync

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenNoBom_ReturnsEmptyResultWithoutPendingOrPakai()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns([]);

        var result = await _sut.ProcessTindakanBomAsync("TKI01", "TRF01", 1, "IGD01", "", "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.IsChargeBarang.Should().BeFalse();
        result.ListBarang.Should().BeEmpty();
        _mediatorMock.Verify(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenFullStockWithUomConversionAndChargeBarang_DeductsFullAndReturnsSnapshot()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF01", IsChargeBarang = true }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == "TRF01")))
            .Returns([new TarifBarangType("TRF01", 1, "BRG01", "Kassa Steril", "BOX", "Box", 1m)]);

        var brgMock = CreateMockBrg("BRG01", "Kassa Steril", "PCS", ("BOX", 10));
        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG01")))
            .Returns(MayBe.From(brgMock.Object));

        _legacyStockReadPortMock
            .Setup(x => x.ListBalancesAtLocation("BRG01", "IGD01"))
            .Returns([CreateStockBalance("BRG01", "IGD01", 25m)]);

        PakaiBrgCreateCommand? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PakaiBrgCreateResponse>, CancellationToken>((req, _) => capturedCmd = (PakaiBrgCreateCommand)req)
            .ReturnsAsync(new PakaiBrgCreateResponse("PK-BOM-01", new DateTime(2026, 11, 26), 50000m, 1));

        var result = await _sut.ProcessTindakanBomAsync("TKI01", "TRF01", 2, "IGD01", "REG01", "USER1");

        result.PakaiBrgId.Should().Be("PK-BOM-01");
        result.IsChargeBarang.Should().BeTrue();
        result.ListBarang.Should().ContainSingle();

        var item = result.ListBarang[0];
        item.NoUrut.Should().Be(1);
        item.BrgId.Should().Be("BRG01");
        item.SatuanBomId.Should().Be("BOX");
        item.QtyBom.Should().Be(2m);
        item.SatuanKecilId.Should().Be("PCS");
        item.QtyKecilRequested.Should().Be(20m);
        item.QtyKecilDeducted.Should().Be(20m);
        item.QtyKecilPending.Should().Be(0m);
        item.IsChargeBarang.Should().BeTrue();
        item.PakaiBrgId.Should().Be("PK-BOM-01");
        item.TrsBillingId.Should().Be("TKI01-B01");
        item.IsVoid.Should().BeFalse();

        capturedCmd.Should().NotBeNull();
        capturedCmd!.LayananId.Should().Be("IGD01");
        capturedCmd.ListItem.Should().ContainSingle(i => i.BrgId == "BRG01" && i.Qty == 20m);
        _tindakanStokPendingRepoMock.Verify(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenPartialStockPreReg_DeductsAvailableAndRecordsPending()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == "TRF02")))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF02", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.Is<ITarifKey>(k => k.TarifId == "TRF02")))
            .Returns([new TarifBarangType("TRF02", 1, "BRG02", "Infus Set", "PCS", "Pcs", 5m)]);

        var brgMock = CreateMockBrg("BRG02", "Infus Set", "PCS");
        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG02")))
            .Returns(MayBe.From(brgMock.Object));

        _legacyStockReadPortMock
            .Setup(x => x.ListBalancesAtLocation("BRG02", "IGD01"))
            .Returns([CreateStockBalance("BRG02", "IGD01", 2m)]);

        List<TindakanStokPendingType>? insertedPending = null;
        _tindakanStokPendingRepoMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()))
            .Callback<IEnumerable<TindakanStokPendingType>, string>((list, _) => insertedPending = list.ToList());

        var result = await _sut.ProcessTindakanBomAsync("TKI02", "TRF02", 1, "IGD01", "", "USER1");

        result.PakaiBrgId.Should().Be("PK0001");
        result.ListBarang.Should().ContainSingle();
        result.ListBarang[0].QtyKecilRequested.Should().Be(5m);
        result.ListBarang[0].QtyKecilDeducted.Should().Be(2m);
        result.ListBarang[0].QtyKecilPending.Should().Be(3m);
        result.ListBarang[0].PakaiBrgId.Should().Be("PK0001");
        result.ListBarang[0].TrsBillingId.Should().BeEmpty();

        insertedPending.Should().NotBeNull();
        insertedPending!.Should().ContainSingle();
        insertedPending[0].TindakanId.Should().Be("TKI02");
        insertedPending[0].RegId.Should().BeEmpty();
        insertedPending[0].LayananId.Should().Be("IGD01");
        insertedPending[0].BrgId.Should().Be("BRG02");
        insertedPending[0].QtyPending.Should().Be(3m);
        insertedPending[0].NoUrut.Should().Be(1);
    }

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenZeroStock_SkipsPakaiBrgAndRecordsFullPending()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF03", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF03", 1, "BRG03", "Alcohol Swab", "PCS", "Pcs", 4m)]);

        var brgMock = CreateMockBrg("BRG03", "Alcohol Swab", "PCS");
        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG03")))
            .Returns(MayBe.From(brgMock.Object));

        _legacyStockReadPortMock
            .Setup(x => x.ListBalancesAtLocation("BRG03", "IGD01"))
            .Returns([]);

        List<TindakanStokPendingType>? insertedPending = null;
        _tindakanStokPendingRepoMock
            .Setup(x => x.Insert(It.IsAny<IEnumerable<TindakanStokPendingType>>(), It.IsAny<string>()))
            .Callback<IEnumerable<TindakanStokPendingType>, string>((list, _) => insertedPending = list.ToList());

        var result = await _sut.ProcessTindakanBomAsync("TKI03", "TRF03", 1, "IGD01", "", "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.ListBarang.Should().ContainSingle();
        result.ListBarang[0].QtyKecilDeducted.Should().Be(0m);
        result.ListBarang[0].QtyKecilPending.Should().Be(4m);
        result.ListBarang[0].PakaiBrgId.Should().BeEmpty();

        _mediatorMock.Verify(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        insertedPending.Should().ContainSingle(p => p.QtyPending == 4m && p.TindakanId == "TKI03");
    }

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenLegacyStockReadThrows_FallbacksToPendingWithoutBlocking()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF04", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF04", 1, "BRG04", "Gloves", "PCS", "Pcs", 2m)]);

        var brgMock = CreateMockBrg("BRG04", "Gloves", "PCS");
        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG04")))
            .Returns(MayBe.From(brgMock.Object));

        _legacyStockReadPortMock
            .Setup(x => x.ListBalancesAtLocation("BRG04", "IGD01"))
            .Throws(new InvalidOperationException("Legacy DB timeout"));

        var result = await _sut.ProcessTindakanBomAsync("TKI04", "TRF04", 1, "IGD01", "", "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.ListBarang.Should().ContainSingle(b => b.QtyKecilDeducted == 0m && b.QtyKecilPending == 2m);
        _tindakanStokPendingRepoMock.Verify(
            x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(l => l.Count() == 1 && l.First().QtyPending == 2m), "USER1"),
            Times.Once);
    }

    [Fact]
    public async Task ProcessTindakanBomAsync_WhenPakaiBrgCreateThrows_FallbacksToPendingWithoutBlocking()
    {
        _tarifRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<ITarifKey>()))
            .Returns(MayBe.From(TarifType.Default with { TarifId = "TRF05", IsChargeBarang = false }));

        _tarifBarangRepoMock
            .Setup(x => x.ListData(It.IsAny<ITarifKey>()))
            .Returns([new TarifBarangType("TRF05", 1, "BRG05", "Spuit 5cc", "PCS", "Pcs", 3m)]);

        var brgMock = CreateMockBrg("BRG05", "Spuit 5cc", "PCS");
        _brgRepoMock
            .Setup(x => x.LoadEntity(It.Is<IBrgKey>(k => k.BrgId == "BRG05")))
            .Returns(MayBe.From(brgMock.Object));

        _legacyStockReadPortMock
            .Setup(x => x.ListBalancesAtLocation("BRG05", "IGD01"))
            .Returns([CreateStockBalance("BRG05", "IGD01", 10m)]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgCreateCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Concurrent depletion"));

        var result = await _sut.ProcessTindakanBomAsync("TKI05", "TRF05", 1, "IGD01", "REG05", "USER1");

        result.PakaiBrgId.Should().BeEmpty();
        result.ListBarang.Should().ContainSingle(b => b.QtyKecilDeducted == 0m && b.QtyKecilPending == 3m && b.PakaiBrgId == "");
        _tindakanStokPendingRepoMock.Verify(
            x => x.Insert(It.Is<IEnumerable<TindakanStokPendingType>>(l => l.Count() == 1 && l.First().QtyPending == 3m), "USER1"),
            Times.Once);
    }

    #endregion

    #region VoidTindakanStockAsync

    [Fact]
    public async Task VoidTindakanStockAsync_PreRegWithHeaderPakaiBrgId_VoidsPakaiBrgAndMarksPendingVoid()
    {
        _tindakanStokPendingRepoMock
            .Setup(x => x.ListByTindakanId("TKI10"))
            .Returns([]);

        var voidDate = new DateTime(2026, 11, 26, 12, 0, 0);
        await _sut.VoidTindakanStockAsync("TKI10", "PK-BHP-10", null, "USER1", "Salah input", voidDate);

        _mediatorMock.Verify(
            x => x.Send(
                It.Is<PakaiBrgVoidCommand>(c => c.PakaiBrgId == "PK-BHP-10" && c.UserId == "USER1" && c.AlasanVoid == "Salah input"),
                It.IsAny<CancellationToken>()),
            Times.Once);
        _tindakanStokPendingRepoMock.Verify(x => x.MarkVoidByTindakanId("TKI10", voidDate, "USER1"), Times.Once);
    }

    [Fact]
    public async Task VoidTindakanStockAsync_PostRegWithSettledPendingStock_VoidsAllUniquePakaiBrgIdsAndMarksVoid()
    {
        var voidedIds = new List<string>();
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<PakaiBrgVoidCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PakaiBrgVoidResponse>, CancellationToken>((req, _) => voidedIds.Add(((PakaiBrgVoidCommand)req).PakaiBrgId))
            .ReturnsAsync((PakaiBrgVoidCommand cmd, CancellationToken _) =>
                new PakaiBrgVoidResponse(cmd.PakaiBrgId, true, new DateTime(2026, 11, 26, 12, 0, 0), cmd.AlasanVoid));

        _tindakanStokPendingRepoMock
            .Setup(x => x.ListByTindakanId("TKI20"))
            .Returns([
                new TindakanStokPendingType("PND1", "TKI20", "", 1, "REG20", "IGD01", "BRG01", "PCS", 3m, 3m, true, "PK-SETTLE-01", false),
                new TindakanStokPendingType("PND2", "TKI20", "", 2, "REG20", "IGD01", "BRG02", "PCS", 2m, 0m, false, "", false),
                new TindakanStokPendingType("PND3", "TKI20", "", 3, "REG20", "IGD01", "BRG03", "PCS", 1m, 1m, true, "PK-ALREADY-VOID", true)
            ]);

        var voidDate = new DateTime(2026, 11, 26, 12, 30, 0);
        await _sut.VoidTindakanStockAsync(
            "TKI20",
            "PK-INIT-20",
            ["PK-INIT-20", "PK-ITEM-21"],
            "USER1",
            "Batal tindakan",
            voidDate);

        voidedIds.Should().BeEquivalentTo(["PK-INIT-20", "PK-ITEM-21", "PK-SETTLE-01"]);
        _tindakanStokPendingRepoMock.Verify(x => x.MarkVoidByTindakanId("TKI20", voidDate, "USER1"), Times.Once);
    }

    [Fact]
    public async Task VoidTindakanStockAsync_HistoricalWithoutPakaiBrgId_SkipsPakaiBrgVoidAndMarksPendingVoid()
    {
        _tindakanStokPendingRepoMock
            .Setup(x => x.ListByTindakanId("TKI30"))
            .Returns([]);

        var voidDate = new DateTime(2026, 11, 26, 13, 0, 0);
        await _sut.VoidTindakanStockAsync("TKI30", "", null, "USER1", "Legacy void", voidDate);

        _mediatorMock.Verify(x => x.Send(It.IsAny<PakaiBrgVoidCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        _tindakanStokPendingRepoMock.Verify(x => x.MarkVoidByTindakanId("TKI30", voidDate, "USER1"), Times.Once);
    }

    #endregion
}
