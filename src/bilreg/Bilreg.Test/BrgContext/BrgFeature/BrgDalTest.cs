using System.Data.SqlClient;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Infrastructure.BrgContext.BrgFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.BrgContext.BrgFeature;

public class BrgDalTest
{
    private readonly BrgDal _sut = new(ConnStringHelper.GetTestEnv());
    private readonly BrgStockMovementCheckDal _movementCheckSut = new(ConnStringHelper.GetTestEnv());

    private static BrgDto Faker()
        => new BrgDto(
            fs_kd_barang: "BRG001",
            fs_nm_barang: "Paracetamol 500mg",
            fb_aktif: true,
            fs_ket_barang: "Keterangan",
            fs_kd_grup_rek: "GR01",
            fs_kd_pabrik: "P01",
            fs_nm_grup_rek: "Grup Obat",
            fs_kd_grup_rek_dk: "OBT",
            fs_nm_grup_rek_dk: "Obat-obatan",
            fs_nm_pabrik: "Kimia Farma");

    private static IBrgKey FakerKey()
        => new BrgKey("BRG001");

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var updated = Faker() with { fs_nm_barang = "Paracetamol Forte" };
        _sut.Update(updated);
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.fs_nm_barang.Should().Be("Paracetamol Forte");
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        _sut.Delete(FakerKey());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeNull();
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.fs_kd_barang.Should().Be("BRG001");
        actual.fs_nm_barang.Should().Be("Paracetamol 500mg");
        actual.fb_aktif.Should().BeTrue();
        actual.fs_ket_barang.Should().Be("Keterangan");
        actual.fs_kd_grup_rek.Should().Be("GR01");
        actual.fs_kd_pabrik.Should().Be("P01");
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var keyword = "Paracetamol";
        var act = () => _sut.ListData(keyword);
        act.Should().NotThrow();
    }

    [Fact]
    public void HasStockMovementOrBalance_ReturnsFalse_WhenNoMovementOrStock()
    {
        using var trans = TransHelper.NewScope();
        var result = _movementCheckSut.HasStockMovementOrBalance("NON_EXISTING_BRG_XYZ");
        result.Should().BeFalse();
    }

    [Fact]
    public void HasStockMovementOrBalance_ReturnsTrue_WhenStockExistsInTbStok()
    {
        using var trans = TransHelper.NewScope();
        var opt = ConnStringHelper.GetTestEnv().Value;
        using var conn = new SqlConnection(ConnStringHelper.Get(opt));
        conn.Execute("""
            INSERT INTO tb_stok(fs_kd_trs, fs_kd_barang, fs_kd_layanan, fn_qty, fn_qty_in, fn_hpp)
            VALUES('TRS999', 'BRG_STOK_01', 'LY01', 5, 5, 100)
            """);

        var result = _movementCheckSut.HasStockMovementOrBalance("BRG_STOK_01");
        result.Should().BeTrue();
    }
}