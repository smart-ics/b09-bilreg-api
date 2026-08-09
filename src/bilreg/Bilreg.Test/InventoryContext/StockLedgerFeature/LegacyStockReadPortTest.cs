using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StockLedgerFeature;
using Bilreg.Infrastructure.InventoryContext.StokFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.InventoryContext.StockLedgerFeature;

public class LegacyStockReadPortTest
{
    private readonly LegacyStockReadPort _sut = new(ConnStringHelper.GetTestEnv());
    private readonly tb_buku_dal _bukuDal = new(ConnStringHelper.GetTestEnv());
    private readonly tb_stok_dal _stokDal = new(ConnStringHelper.GetTestEnv());

    private const string BrgId = "BRGSTLREAD01";
    private const string DoId = "DOSTLREAD1";
    private const string OtherDo = "DOSTLREAD2";

    [Fact]
    public void ListJournals_ScopesByItemAndDo_OrderedByDatetimeThenBukuId()
    {
        using var trans = TransHelper.NewScope();

        _bukuDal.Insert(
        [
            Buku("BKSTL00002", BrgId, "LY001", DoId, "2026-03-01", "12:00:00", "2026-03-01 12:00:00",
                QtyIn: 5, QtyOut: 0, Kind: "DO", Mutasi: "DM00000001"),
            Buku("BKSTL00001", BrgId, "LY001", DoId, "2026-03-01", "12:00:00", "2026-03-01 12:00:00",
                QtyIn: 0, QtyOut: 2, Kind: "PK", Mutasi: "PK00000001"),
            Buku("BKSTL00003", BrgId, "LY002", DoId, "2026-03-02", "09:00:00", "2026-03-02 09:00:00",
                QtyIn: 0, QtyOut: 1, Kind: "PK", Mutasi: "PK00000002"),
            Buku("BKSTL00099", BrgId, "LY001", OtherDo, "2026-03-01", "08:00:00", "2026-03-01 08:00:00",
                QtyIn: 99, QtyOut: 0, Kind: "DO", Mutasi: "DM00000099"),
        ]);

        var journals = _sut.ListJournals(BrgId, DoId);

        journals.Should().HaveCount(3);
        journals.Select(x => x.LegacyBukuId).Should().Equal("BKSTL00001", "BKSTL00002", "BKSTL00003");
        journals.Should().OnlyContain(x => x.BrgMasukReffId == DoId);
        journals[0].QtyOut.Should().Be(2);
        journals[1].QtyIn.Should().Be(5);
        journals[2].TglMutasi.Should().Be(new DateTime(2026, 3, 2, 9, 0, 0));
    }

    [Fact]
    public void ListJournals_MapsSentinelEdAndComposedDatetime()
    {
        using var trans = TransHelper.NewScope();

        _bukuDal.Insert(
        [
            Buku("BKSTL00010", BrgId, "LY001", DoId, "2026-04-01", "15:30:00", "3000-01-01 00:00:00",
                QtyIn: 3, QtyOut: 0, Kind: "DO", Mutasi: "DM00000010",
                TglEd: "3000-01-01"),
            Buku("BKSTL00011", BrgId, "LY001", DoId, "2026-04-02", "08:00:00", "2026-04-02 08:00:00",
                QtyIn: 0, QtyOut: 1, Kind: "DB", Mutasi: "DB00000011",
                TglEd: "2027-01-15"),
        ]);

        var journals = _sut.ListJournals(BrgId, DoId);

        journals.Should().HaveCount(2);
        journals[0].TglEd.Should().Be(StockLedgerSentinel.EmptyDate);
        journals[0].TglMutasi.Should().Be(new DateTime(2026, 4, 1, 15, 30, 0));
        journals[1].TglEd.Should().Be(new DateTime(2027, 1, 15));
        journals[1].MovementKindString.Should().Be("DB");
    }

    [Fact]
    public void ListJournals_EmptyJam_OrdersByComposedDatetimeThenBukuId()
    {
        using var trans = TransHelper.NewScope();

        // Empty jam + sentinel tgl_jam → ComposeDateAndTime uses 00:00:00 (not SQL trailing-space key).
        _bukuDal.Insert(
        [
            Buku("BKSTL00022", BrgId, "LY001", DoId, "2026-03-01", "08:00:00", "3000-01-01 00:00:00",
                QtyIn: 0, QtyOut: 1, Kind: "PK", Mutasi: "PK00000022"),
            Buku("BKSTL00021", BrgId, "LY001", DoId, "2026-03-01", "", "3000-01-01 00:00:00",
                QtyIn: 5, QtyOut: 0, Kind: "DO", Mutasi: "DM00000021"),
            Buku("BKSTL00023", BrgId, "LY001", DoId, "2026-03-01", "  ", "3000-01-01 00:00:00",
                QtyIn: 2, QtyOut: 0, Kind: "DO", Mutasi: "DM00000023"),
        ]);

        var journals = _sut.ListJournals(BrgId, DoId);

        journals.Should().HaveCount(3);
        journals.Select(x => x.LegacyBukuId).Should().Equal("BKSTL00021", "BKSTL00023", "BKSTL00022");
        journals[0].TglMutasi.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0));
        journals[1].TglMutasi.Should().Be(new DateTime(2026, 3, 1, 0, 0, 0));
        journals[2].TglMutasi.Should().Be(new DateTime(2026, 3, 1, 8, 0, 0));
    }

    [Fact]
    public void ListBalances_ScopesByItemAndDo()
    {
        using var trans = TransHelper.NewScope();

        _stokDal.Insert(Stok("STSTL00001", BrgId, "LY001", DoId, qty: 7, hpp: 100));
        _stokDal.Insert(Stok("STSTL00002", BrgId, "LY002", DoId, qty: 3, hpp: 100));
        _stokDal.Insert(Stok("STSTL00099", BrgId, "LY001", OtherDo, qty: 50, hpp: 100));

        var balances = _sut.ListBalances(BrgId, DoId);

        balances.Should().HaveCount(2);
        balances.Should().OnlyContain(x => x.BrgMasukReffId == DoId);
        balances.Sum(x => x.QtySisa).Should().Be(10);
        balances.Should().Contain(x => x.LegacyStokId == "STSTL00001" && x.QtySisa == 7);
    }

    [Fact]
    public void ParseHelpers_SentinelAndCompose()
    {
        LegacyStockReadPort.ParseLegacyDate("3000-01-01").Should().Be(StockLedgerSentinel.EmptyDate);
        LegacyStockReadPort.ParseLegacyDate("2026-05-01").Should().Be(new DateTime(2026, 5, 1));
        LegacyStockReadPort.ComposeMutasiDateTime(
                "2026-05-01 14:00:00", "2026-01-01", "00:00:00")
            .Should().Be(new DateTime(2026, 5, 1, 14, 0, 0));
        LegacyStockReadPort.ComposeMutasiDateTime(
                "3000-01-01 00:00:00", "2026-05-02", "09:15:00")
            .Should().Be(new DateTime(2026, 5, 2, 9, 15, 0));
        LegacyStockReadPort.ComposeDateAndTime("2026-05-03", null)
            .Should().Be(new DateTime(2026, 5, 3, 0, 0, 0));
        LegacyStockReadPort.ComposeDateAndTime("2026-05-03", "")
            .Should().Be(new DateTime(2026, 5, 3, 0, 0, 0));
        LegacyStockReadPort.ComposeDateAndTime("2026-05-03", "   ")
            .Should().Be(new DateTime(2026, 5, 3, 0, 0, 0));
    }

    private static tb_buku_dto Buku(
        string id,
        string brgId,
        string layananId,
        string doId,
        string tgl,
        string jam,
        string tglJam,
        decimal QtyIn,
        decimal QtyOut,
        string Kind,
        string Mutasi,
        string TglEd = "3000-01-01") =>
        new(
            fs_kd_trs: id,
            fs_kd_barang: brgId,
            fs_kd_layanan: layananId,
            fs_kd_po: "",
            fs_kd_do: doId,
            fd_tgl_ed: TglEd,
            fs_no_batch: "",
            fn_stok_in: QtyIn,
            fn_stok_out: QtyOut,
            fn_hpp: 1000,
            fs_kd_mutasi: Mutasi,
            fd_tgl_mutasi: tgl,
            fs_jam_mutasi: jam,
            fd_tgl_jam_mutasi: tglJam,
            fs_kd_jenis_mutasi: Kind,
            fs_kd_satuan: "PCS",
            fs_nm_barang: "",
            fs_nm_layanan: "");

    private static tb_stok_dto Stok(
        string id,
        string brgId,
        string layananId,
        string doId,
        decimal qty,
        decimal hpp) =>
        new(
            fs_kd_trs: id,
            fs_kd_barang: brgId,
            fs_kd_layanan: layananId,
            fs_kd_po: "",
            fs_kd_do: doId,
            fd_tgl_ed: "3000-01-01",
            fs_no_batch: "",
            fn_qty_in: qty,
            fn_qty: qty,
            fn_hpp: hpp,
            fd_tgl_do: "2026-03-01",
            fs_jam_do: "10:00:00",
            fs_kd_mutasi: "DM00000001",
            fd_tgl_mutasi: "2026-03-01",
            fs_jam_mutasi: "10:00:00",
            fs_kd_satuan: "PCS",
            fs_nm_barang: "",
            fs_nm_layanan: "");
}
