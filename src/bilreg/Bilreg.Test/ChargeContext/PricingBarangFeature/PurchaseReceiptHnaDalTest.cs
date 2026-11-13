using System.Data.SqlClient;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Integration tests query riwayat pengadaan HNA terhadap tabel legacy `tb_trs_do` dan `tb_trs_do2`.
/// Skema legacy tersedia pada basis data `HOSPITAL_PKL` (repositori pattern: PricingBarangFeature).
/// Setiap test di-rollback via `TransHelper.NewScope()`.
/// </summary>
[Collection("PricingBarangDatabaseTests")]
public class PurchaseReceiptHnaDalTest
{
    private const string EmptyVoidDate = "3000-01-01";

    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly PurchaseReceiptHnaDal _sut;

    public PurchaseReceiptHnaDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new PurchaseReceiptHnaDal(_dbOptions);
    }

    private static string NewBarangId() => $"Z{Random.Shared.Next(100000000, 999999999)}";

    private string SeedDo(string tglTrs, string tglVoid = EmptyVoidDate)
    {
        var trsId = $"D{Random.Shared.Next(100000000, 999999999)}";
        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(
            """
            INSERT INTO tb_trs_do (fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas, fd_tgl_void)
            VALUES (@trsId, @tglTrs, '08:00:00', 'U1', @tglVoid)
            """,
            new { trsId, tglTrs, tglVoid });
        return trsId;
    }

    private void SeedDoLine(
        string trsId, int noUrut, string barangId, decimal harga, decimal konversi, decimal qty, bool fbVoid = false)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(
            """
            INSERT INTO tb_trs_do2 (
                fs_kd_trs, fn_no_urut, fs_kd_barang, fn_harga, fn_diskon, fn_qty,
                fs_kd_satuan, fn_sub_total, fn_tax, fn_total, fn_konversi, fb_void)
            VALUES (
                @trsId, @noUrut, @barangId, @harga, 0, @qty,
                'BOK', @harga, 0, @harga, @konversi, @fbVoid)
            """,
            new { trsId, noUrut, barangId, harga, qty, konversi, fbVoid });
    }

    [Fact]
    public void GetProcurementHistory_ComputesPerSmallUnitPrice_FromKonversi()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        var trsId = SeedDo("2026-03-10");
        SeedDoLine(trsId, 1, barangId, harga: 30000m, konversi: 3m, qty: 2m);

        var result = _sut.GetProcurementHistory(barangId).ToList();

        result.Should().HaveCount(1);
        result[0].ReceiptId.Should().Be(trsId);
        result[0].NoUrut.Should().Be(1);
        result[0].BarangId.Should().Be(barangId);
        result[0].Harga.Should().Be(30000m);
        result[0].Konversi.Should().Be(3m);
        result[0].HargaBeliPerSatuanKecil.Should().Be(10000m);
        result[0].TglTrs.Should().Be(new DateTime(2026, 3, 10));
    }

    [Fact]
    public void GetProcurementHistory_DoesNotDivide_WhenKonversiIsZeroOrOne()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();
        var trsId = SeedDo("2026-03-10");
        SeedDoLine(trsId, 1, barangId, harga: 5000m, konversi: 0m, qty: 1m);
        SeedDoLine(trsId, 2, barangId, harga: 7000m, konversi: 1m, qty: 1m);

        var result = _sut.GetProcurementHistory(barangId).ToList();

        result.Should().HaveCount(2);
        result.Single(x => x.NoUrut == 1).HargaBeliPerSatuanKecil.Should().Be(5000m);
        result.Single(x => x.NoUrut == 2).HargaBeliPerSatuanKecil.Should().Be(7000m);
    }

    [Fact]
    public void GetProcurementHistory_ExcludesVoidLines_AndVoidDocuments()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();

        var validTrs = SeedDo("2026-04-01");
        SeedDoLine(validTrs, 1, barangId, harga: 100m, konversi: 1m, qty: 1m);

        SeedDoLine(validTrs, 2, barangId, harga: 200m, konversi: 1m, qty: 1m, fbVoid: true);

        var voidTrs = SeedDo("2026-04-02", tglVoid: "2026-04-03");
        SeedDoLine(voidTrs, 1, barangId, harga: 300m, konversi: 1m, qty: 1m);

        var result = _sut.GetProcurementHistory(barangId).ToList();

        result.Should().HaveCount(1);
        result[0].ReceiptId.Should().Be(validTrs);
        result[0].NoUrut.Should().Be(1);
    }

    [Fact]
    public void GetProcurementHistory_FiltersByDateRange()
    {
        using var trans = TransHelper.NewScope();
        var barangId = NewBarangId();

        var january = SeedDo("2026-01-15");
        SeedDoLine(january, 1, barangId, harga: 100m, konversi: 1m, qty: 1m);

        var june = SeedDo("2026-06-15");
        SeedDoLine(june, 1, barangId, harga: 200m, konversi: 1m, qty: 1m);

        var fromJune = _sut.GetProcurementHistory(barangId, start: new DateTime(2026, 6, 1)).ToList();
        fromJune.Should().HaveCount(1);
        fromJune[0].ReceiptId.Should().Be(june);

        var untilFebruary = _sut.GetProcurementHistory(barangId, end: new DateTime(2026, 2, 1)).ToList();
        untilFebruary.Should().HaveCount(1);
        untilFebruary[0].ReceiptId.Should().Be(january);
    }

    [Fact]
    public void GetProcurementHistory_UnknownBarang_ReturnsEmpty()
    {
        using var trans = TransHelper.NewScope();

        var result = _sut.GetProcurementHistory(NewBarangId()).ToList();

        result.Should().BeEmpty();
    }
}
