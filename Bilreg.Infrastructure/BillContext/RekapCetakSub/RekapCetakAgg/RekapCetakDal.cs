using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.RekapCetakAgg;

public class RekapCetakDal : IRekapCetakDal
{
    private readonly DatabaseOptions _opt;

    public RekapCetakDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(RekapCetakModel model)
    {
        const string sql = @"
            INSERT INTO ta_rekap_cetak_tarif(
                fs_kd_rekap_cetak_tarif, fs_nm_rekap_cetak_tarif,
                fs_urut, fb_grup_baru, fn_level,
                fs_kd_grup_rekap_cetak, fs_kd_rekap_cetak_dk)
            VALUES(
                @fs_kd_rekap_cetak_tarif, @fs_nm_rekap_cetak_tarif,
                @fs_urut, @fb_grup_baru, @fn_level,
                @fs_kd_grup_rekap_cetak, @fs_kd_rekap_cetak_dk
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rekap_cetak_tarif", model.RekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rekap_cetak_tarif", model.RekapCetakName, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", model.NoUrut.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fb_grup_baru", model.IsGrupBaru, SqlDbType.Bit);
        dp.AddParam("@fn_level", model.Level, SqlDbType.Int);
        dp.AddParam("@fs_kd_grup_rekap_cetak", model.GrupRekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak_dk", model.RekapCetakDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RekapCetakModel model)
    {
        const string sql = @"
            UPDATE 
                ta_rekap_cetak_tarif
            SET 
                fs_nm_rekap_cetak_tarif = @fs_nm_rekap_cetak_tarif,
                fs_urut = @fs_urut,
                fb_grup_baru = @fb_grup_baru,
                fn_level = @fn_level,
                fs_kd_grup_rekap_cetak = @fs_kd_grup_rekap_cetak,
                fs_kd_rekap_cetak_dk = @fs_kd_rekap_cetak_dk
            WHERE   
                fs_kd_rekap_cetak_tarif = @fs_kd_rekap_cetak_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rekap_cetak_tarif", model.RekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_rekap_cetak_tarif", model.RekapCetakName, SqlDbType.VarChar);
        dp.AddParam("@fs_urut", model.NoUrut, SqlDbType.VarChar);
        dp.AddParam("@fb_grup_baru", model.IsGrupBaru, SqlDbType.Bit);
        dp.AddParam("@fn_level", model.Level, SqlDbType.Int);
        dp.AddParam("@fs_kd_grup_rekap_cetak", model.GrupRekapCetakId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rekap_cetak_dk", model.RekapCetakDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRekapCetakKey key)
    {
        const string sql = @"
            DELETE FROM 
                ta_rekap_cetak_tarif
            WHERE   
                fs_kd_rekap_cetak_tarif = @fs_kd_rekap_cetak_tarif";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rekap_cetak_tarif", key.RekapCetakId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    }

    public RekapCetakModel GetData(IRekapCetakKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_rekap_cetak_tarif, aa.fs_nm_rekap_cetak_tarif, 
                aa.fs_urut, aa.fb_grup_baru, aa.fn_level,
                aa.fs_kd_grup_rekap_cetak, aa.fs_kd_rekap_cetak_dk,
                ISNULL(bb.fs_nm_grup_rekap_cetak, '') fs_nm_grup_rekap_cetak,
                ISNULL(cc.fs_nm_rekap_cetak_dk, '') fs_nm_rekap_cetak_dk
            FROM
                ta_rekap_cetak_tarif aa
                LEFT JOIN ta_grup_rekap_cetak bb ON aa.fs_kd_grup_rekap_cetak = bb.fs_kd_grup_rekap_cetak
                LEFT JOIN ta_rekap_cetak_dk cc ON aa.fs_kd_rekap_cetak_dk = cc.fs_kd_rekap_cetak_dk
            WHERE
                aa.fs_kd_rekap_cetak_tarif = @fs_kd_rekap_cetak_tarif
                ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rekap_cetak_tarif", key.RekapCetakId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RekapCetakDto>(sql, dp);
    }

    public IEnumerable<RekapCetakModel> ListData()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_rekap_cetak_tarif, aa.fs_nm_rekap_cetak_tarif, 
                aa.fs_urut, aa.fb_grup_baru, aa.fn_level,
                aa.fs_kd_grup_rekap_cetak, aa.fs_kd_rekap_cetak_dk,
                ISNULL(bb.fs_nm_grup_rekap_cetak, '') fs_nm_grup_rekap_cetak,
                ISNULL(cc.fs_nm_rekap_cetak_dk, '') fs_nm_rekap_cetak_dk
            FROM
                ta_rekap_cetak_tarif aa
                LEFT JOIN ta_grup_rekap_cetak bb ON aa.fs_kd_grup_rekap_cetak = bb.fs_kd_grup_rekap_cetak
                LEFT JOIN ta_rekap_cetak_dk cc ON aa.fs_kd_rekap_cetak_dk = cc.fs_kd_rekap_cetak_dk
            ";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RekapCetakDto>(sql);
    }
}

public class RekapCetakTest
{
    private readonly RekapCetakDal _sut;

    public RekapCetakTest()
    {
        _sut = new RekapCetakDal(ConnStringHelper.GetTestEnv());
    }
    
    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDto();
        expected.SetTestData();
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDto();
        expected.SetTestData();
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDto();
        expected.SetTestData();
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDto();
        expected.SetTestData();
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new RekapCetakDto();
        expected.SetTestData();
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}