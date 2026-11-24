using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Infrastructure.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TarifFeature;

public class TarifDalTest
{
    private readonly TarifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static void InsertTarif(string tarifId, string tarifName, bool isChargeBarang)
    {
        const string sql = """
            INSERT INTO ta_tarif (fs_kd_tarif, fs_nm_tarif, fb_charge_brg, fb_aktif)
            VALUES (@TarifId, @TarifName, @IsChargeBarang, 1);
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TarifId", tarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", tarifName, SqlDbType.VarChar);
        dp.AddParam("@IsChargeBarang", isChargeBarang, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql, dp);
    }

    [Fact]
    public void GetData_GivenTarifWithChargeBarangTrue_ReturnsDtoWithFbChargeBrgTrue()
    {
        using var trans = TransHelper.NewScope();
        var tarifId = "TRF_TEST1";
        InsertTarif(tarifId, "Test Tarif Charge Barang", true);

        var actual = _sut.GetData(TarifType.Key(tarifId));

        actual.Should().NotBeNull();
        actual.fs_kd_tarif.Should().Be(tarifId);
        actual.fb_charge_brg.Should().BeTrue();
    }

    [Fact]
    public void GetData_GivenTarifWithChargeBarangFalse_ReturnsDtoWithFbChargeBrgFalse()
    {
        using var trans = TransHelper.NewScope();
        var tarifId = "TRF_TEST2";
        InsertTarif(tarifId, "Test Tarif Gratis Barang", false);

        var actual = _sut.GetData(TarifType.Key(tarifId));

        actual.Should().NotBeNull();
        actual.fs_kd_tarif.Should().Be(tarifId);
        actual.fb_charge_brg.Should().BeFalse();
    }

    [Fact]
    public void DtoToModel_MapsIsChargeBarangCorrectly()
    {
        var dtoTrue = new TarifDto("T1", "Tarif 1", "G1", "GD1", "J1", "R1", "Grup", "GrupDk", "Jenis", "Rekap", true);
        var modelTrue = dtoTrue.ToModel();
        modelTrue.IsChargeBarang.Should().BeTrue();

        var dtoFalse = new TarifDto("T2", "Tarif 2", "G1", "GD1", "J1", "R1", "Grup", "GrupDk", "Jenis", "Rekap", false);
        var modelFalse = dtoFalse.ToModel();
        modelFalse.IsChargeBarang.Should().BeFalse();
    }

    [Fact]
    public void DtoFromModel_MapsIsChargeBarangCorrectly()
    {
        var model = new TarifType("T1", "Tarif 1", GroupTarifType.Default, GroupTarifDkType.Default, JenisTarifType.Default, RekapCetakType.Default.ToReff(), true);
        var dto = TarifDto.FromModel(model);
        dto.fb_charge_brg.Should().BeTrue();
    }
}
