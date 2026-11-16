using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifBarangDalTest
{
    private readonly TarifBarangDal _sut = new(ConnStringHelper.GetTestEnv());

    private static void SeedBarang(string brgId, string brgName)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM tb_barang WHERE fs_kd_barang = @BrgId)
            BEGIN
                INSERT INTO tb_barang (fs_kd_barang, fs_nm_barang)
                VALUES (@BrgId, @BrgName);
            END
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@BrgName", brgName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql, dp);
    }

    private static void SeedSatuan(string satuanId, string satuanName)
    {
        const string sql = """
            IF NOT EXISTS (SELECT 1 FROM tb_satuan WHERE fs_kd_satuan = @SatuanId)
            BEGIN
                INSERT INTO tb_satuan (fs_kd_satuan, fs_nm_satuan)
                VALUES (@SatuanId, @SatuanName);
            END
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@SatuanId", satuanId, SqlDbType.VarChar);
        dp.AddParam("@SatuanName", satuanName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql, dp);
    }

    private static void InsertTarif7(string tarifId, decimal noUrut, string brgId, string satuanId, decimal qty, decimal harga = 0m)
    {
        const string sql = """
            INSERT INTO TA_TARIF7 (fs_kd_tarif, fn_no_urut, fs_kd_barang, fs_kd_satuan, fn_qty_barang, fn_harga)
            VALUES (@TarifId, @NoUrut, @BrgId, @SatuanId, @Qty, @Harga);
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", tarifId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Decimal);
        dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        dp.AddParam("@SatuanId", satuanId, SqlDbType.VarChar);
        dp.AddParam("@Qty", qty, SqlDbType.Decimal);
        dp.AddParam("@Harga", harga, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql, dp);
    }

    [Fact]
    public void ListData_GivenTarif7ItemsWithScrambledOrder_ReturnsOrderedByNoUrutAsc()
    {
        using var trans = TransHelper.NewScope();
        var tarifId = "TRF_ORD1";
        var brgId1 = "BRG_001";
        var brgId2 = "BRG_002";
        var brgId3 = "BRG_003";
        var satId = "PCS";

        SeedSatuan(satId, "Pieces");
        SeedBarang(brgId1, "Barang Satu");
        SeedBarang(brgId2, "Barang Dua");
        SeedBarang(brgId3, "Barang Tiga");

        // Insert in scrambled order: 3, 1, 2
        InsertTarif7(tarifId, 3m, brgId3, satId, 5m);
        InsertTarif7(tarifId, 1m, brgId1, satId, 10m);
        InsertTarif7(tarifId, 2m, brgId2, satId, 2m);

        var actual = _sut.ListData(TarifType.Key(tarifId)).ToList();

        actual.Should().HaveCount(3);
        actual[0].fn_no_urut.Should().Be(1m);
        actual[0].fs_kd_barang.Should().Be(brgId1);
        actual[1].fn_no_urut.Should().Be(2m);
        actual[1].fs_kd_barang.Should().Be(brgId2);
        actual[2].fn_no_urut.Should().Be(3m);
        actual[2].fs_kd_barang.Should().Be(brgId3);
    }

    [Fact]
    public void ListData_GivenTarif7Items_JoinsWithBarangAndSatuanNames()
    {
        using var trans = TransHelper.NewScope();
        var tarifId = "TRF_JOIN1";
        var brgId = "BRG_JOIN";
        var satId = "KTK";

        SeedSatuan(satId, "Kotak");
        SeedBarang(brgId, "Kasa Steril Box");

        InsertTarif7(tarifId, 1m, brgId, satId, 2.5m);

        var actual = _sut.ListData(TarifType.Key(tarifId)).ToList();

        actual.Should().HaveCount(1);
        actual[0].fs_kd_tarif.Should().Be(tarifId);
        actual[0].fs_kd_barang.Should().Be(brgId);
        actual[0].fs_nm_barang.Should().Be("Kasa Steril Box");
        actual[0].fs_kd_satuan.Should().Be(satId);
        actual[0].fs_nm_satuan.Should().Be("Kotak");
        actual[0].fn_qty_barang.Should().Be(2.5m);
    }

    [Fact]
    public void ListData_GivenTarif7WithFnHarga_IgnoresFnHargaAndMapsProperly()
    {
        using var trans = TransHelper.NewScope();
        var tarifId = "TRF_HRG1";
        var brgId = "BRG_HRG";
        var satId = "AMP";

        SeedSatuan(satId, "Ampul");
        SeedBarang(brgId, "Lidocaine Ampul");

        // fn_harga set to 99999.00 in TA_TARIF7, but should be ignored
        InsertTarif7(tarifId, 1m, brgId, satId, 1m, harga: 99999m);

        var actual = _sut.ListData(TarifType.Key(tarifId)).ToList();

        actual.Should().HaveCount(1);
        var model = actual[0].ToModel();
        model.TarifId.Should().Be(tarifId);
        model.BrgId.Should().Be(brgId);
        model.SatuanId.Should().Be(satId);
        model.QtyBarang.Should().Be(1m);
    }

    [Fact]
    public void ListData_GivenNonExistentTarifId_ReturnsEmptyList()
    {
        using var trans = TransHelper.NewScope();
        var actual = _sut.ListData(TarifType.Key("NON_EXISTENT_TRF"));
        actual.Should().BeEmpty();
    }
}
