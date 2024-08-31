using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.JaminanAgg;

public class JaminanDal: IJaminanDal
{
    private readonly DatabaseOptions _opt;

    public JaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JaminanModel model)
    {
        const string sql = @"
            INSERT INTO ta_jaminan (
                fs_kd_jaminan, fs_nm_jaminan, fs_alm1_jaminan, fs_alm2_jaminan,
                fs_kota_jaminan, fb_aktif, fs_kd_cara_bayar_dk, fs_kd_grup_jaminan,
                fs_benefit_mou
            )
            VALUES (
                @fs_kd_jaminan, @fs_nm_jaminan, @fs_alm1_jaminan, @fs_alm2_jaminan,
                @fs_kota_jaminan, @fb_aktif, @fs_kd_cara_bayar_dk, @fs_kd_grup_jaminan,
                @fs_benefit_mou
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_jaminan", model.Alamat1, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_benefit_mou", model.BenefitMou, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JaminanModel model)
    {
        const string sql = @"
            UPDATE ta_jaminan
                SET fs_nm_jaminan = @fs_nm_jaminan,
                    fs_alm1_jaminan = @fs_alm1_jaminan,
                    fs_alm2_jaminan = @fs_alm2_jaminan,
                    fs_kota_jaminan = @fs_kota_jaminan,
                    fb_aktif = @fb_aktif,
                    fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk,
                    fs_kd_grup_jaminan = @fs_kd_grup_jaminan,
                    fs_benefit_mou = @fs_benefit_mou
            WHERE fs_kd_jaminan = @fs_kd_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_jaminan", model.Alamat1, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", model.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", model.Kota, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_benefit_mou", model.BenefitMou, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JaminanModel GetData(IJaminanKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fs_alm1_jaminan, aa.fs_alm2_jaminan,
                aa.fs_kota_jaminan, aa.fb_aktif, aa.fs_benefit_mou,
                aa.fs_kd_cara_bayar_dk, ISNULL(bb.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk,
                aa.fs_kd_grup_jaminan, ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan
            FROM ta_jaminan aa
                LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
                LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
            WHERE aa.fs_kd_jaminan = @fs_kd_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<JaminanDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<JaminanModel> ListData()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fs_alm1_jaminan, aa.fs_alm2_jaminan,
                aa.fs_kota_jaminan, aa.fb_aktif, aa.fs_benefit_mou,
                aa.fs_kd_cara_bayar_dk, ISNULL(bb.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk,
                aa.fs_kd_grup_jaminan, ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan
            FROM ta_jaminan aa
                LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
                LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<JaminanDto>(sql);
        return result?.Select(x => x.ToModel())!;
    }
}

public class JaminanDalTest
{
    private readonly JaminanDal _sut;

    public JaminanDalTest()
    {
        _sut = new JaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = JaminanModel.Create("A", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = JaminanModel.Create("A", "B");
        _sut.Update(expected);
    }
    
    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = JaminanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = JaminanModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}