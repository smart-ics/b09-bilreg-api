using Bilreg.Application.InventoryContext.StockLedgerFeature.Ports;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

[Collection(StockLedgerSqlCollection.Name)]
public class LegacyStockWriterPortTest
{
    private readonly LegacyStockWriterPort _sut = new(ConnStringHelper.GetTestEnv());
    private readonly LegacyStockReadPort _read = new(ConnStringHelper.GetTestEnv());

    private const string BrgId = "BRGSTLWRT0001";
    private const string DoId = "DOSTLWRT01";
    private const string LayananId = "LY001";
    private static readonly DateTime TglMasuk = new(2026, 4, 1, 10, 0, 0);
    private static readonly DateTime TglMutasi = new(2026, 4, 1, 10, 30, 0);
    private static readonly DateTime TglEd = new(2027, 12, 31);

    [Fact]
    public void InsertInbound_CreatesBukuAndStok_IdsLength10()
    {
        using var trans = TransHelper.NewScope();

        var result = _sut.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, DoId, LayananId,
            Qty: 10, Hpp: 150.5m, TglEd, TglMasuk, TglMutasi,
            TrsReffId: "DMSTLWRT01",
            MovementKindString: "DO",
            PoReffId: "POSTLWRT01",
            NoBatch: "NB-1",
            SatuanId: "PCS"));

        result.LegacyBukuId.Should().HaveLength(10);
        result.LegacyStokId.Should().HaveLength(10);
        result.LegacyBukuId.Should().StartWith("BK");
        result.LegacyStokId.Should().StartWith("ST");

        var journals = _read.ListJournals(BrgId, DoId);
        journals.Should().ContainSingle(x => x.LegacyBukuId == result.LegacyBukuId);
        var jurnal = journals.Single(x => x.LegacyBukuId == result.LegacyBukuId);
        jurnal.QtyIn.Should().Be(10);
        jurnal.QtyOut.Should().Be(0);
        jurnal.Hpp.Should().Be(150.5m);
        jurnal.MovementKindString.Should().Be("DO");
        jurnal.TglMutasi.Should().Be(TglMutasi);
        jurnal.TglEd.Should().Be(TglEd.Date);

        var balances = _read.ListBalances(BrgId, DoId);
        balances.Should().ContainSingle(x => x.LegacyStokId == result.LegacyStokId);
        var stok = balances.Single(x => x.LegacyStokId == result.LegacyStokId);
        stok.QtySisa.Should().Be(10);
        stok.Hpp.Should().Be(150.5m);
        stok.TglMasuk.Should().Be(TglMasuk);
    }

    [Fact]
    public void InsertReverseBuku_DoesNotDeleteOriginalBuku()
    {
        using var trans = TransHelper.NewScope();

        var inbound = _sut.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, DoId, LayananId,
            Qty: 5, Hpp: 100m, StockLedgerSentinel.EmptyDate, TglMasuk, TglMutasi,
            TrsReffId: "DMSTLWRT02",
            MovementKindString: "DO"));

        var beforeCount = _read.ListJournals(BrgId, DoId).Count;

        var reverse = _sut.InsertReverseBuku(new LegacyReverseBukuWriteRequest(
            BrgId, DoId, LayananId,
            QtyIn: 5, QtyOut: 0, Hpp: 100m,
            StockLedgerSentinel.EmptyDate,
            TglMutasi.AddHours(1),
            TrsReffId: "VRSTLWRT02",
            MovementKindString: "DB_V"));

        reverse.LegacyBukuId.Should().HaveLength(10);
        reverse.LegacyBukuId.Should().NotBe(inbound.LegacyBukuId);

        var journals = _read.ListJournals(BrgId, DoId);
        journals.Should().HaveCount(beforeCount + 1);
        journals.Should().Contain(x => x.LegacyBukuId == inbound.LegacyBukuId);
        journals.Should().Contain(x => x.LegacyBukuId == reverse.LegacyBukuId && x.MovementKindString == "DB_V");
    }

    [Fact]
    public void DepleteStok_Partial_ReducesQty_RetainsRow()
    {
        using var trans = TransHelper.NewScope();

        var inbound = _sut.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, DoId, LayananId,
            Qty: 10, Hpp: 100m, StockLedgerSentinel.EmptyDate, TglMasuk, TglMutasi,
            TrsReffId: "DMSTLWRT03",
            MovementKindString: "DO"));

        _sut.DepleteStok(new LegacyStokDepleteRequest(inbound.LegacyStokId, QtyOut: 4));

        var stok = _read.ListBalances(BrgId, DoId)
            .SingleOrDefault(x => x.LegacyStokId == inbound.LegacyStokId);
        stok.Should().NotBeNull();
        stok!.QtySisa.Should().Be(6);
    }

    [Fact]
    public void DepleteStok_ZeroRemaining_DeletesStokRowOnly()
    {
        using var trans = TransHelper.NewScope();

        var inbound = _sut.InsertInbound(new LegacyInboundWriteRequest(
            BrgId, DoId, LayananId,
            Qty: 3, Hpp: 100m, StockLedgerSentinel.EmptyDate, TglMasuk, TglMutasi,
            TrsReffId: "DMSTLWRT04",
            MovementKindString: "DO"));

        _sut.InsertOutboundBuku(new LegacyOutboundBukuWriteRequest(
            BrgId, DoId, LayananId,
            QtyOut: 3, Hpp: 100m, StockLedgerSentinel.EmptyDate, TglMutasi.AddMinutes(5),
            TrsReffId: "PKSTLWRT04",
            MovementKindString: "PK"));

        _sut.DepleteStok(new LegacyStokDepleteRequest(inbound.LegacyStokId, QtyOut: 3));

        _read.ListBalances(BrgId, DoId)
            .Should().NotContain(x => x.LegacyStokId == inbound.LegacyStokId);
        _read.ListJournals(BrgId, DoId)
            .Should().Contain(x => x.LegacyBukuId == inbound.LegacyBukuId);
    }
}
