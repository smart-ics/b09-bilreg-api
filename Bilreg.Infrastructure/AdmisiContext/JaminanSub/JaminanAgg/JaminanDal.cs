using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.JaminanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
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
                fs_benefit_mou )
            VALUES (
                @fs_kd_jaminan, @fs_nm_jaminan, @fs_alm1_jaminan, @fs_alm2_jaminan,
                @fs_kota_jaminan, @fb_aktif, @fs_kd_cara_bayar_dk, @fs_kd_grup_jaminan,
                @fs_benefit_mou )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_jaminan", model.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", model.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", model.Address.Kota, SqlDbType.VarChar);

        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDk.CaraBayarDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminan.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_benefit_mou", model.BenefitMou, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JaminanModel model)
    {
        const string sql = @"
            UPDATE 
                ta_jaminan
            SET 
                fs_nm_jaminan = @fs_nm_jaminan,
                fs_alm1_jaminan = @fs_alm1_jaminan,
                fs_alm2_jaminan = @fs_alm2_jaminan,
                fs_kota_jaminan = @fs_kota_jaminan,
                fb_aktif = @fb_aktif,
                fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk,
                fs_kd_grup_jaminan = @fs_kd_grup_jaminan,
                fs_benefit_mou = @fs_benefit_mou
            WHERE 
                fs_kd_jaminan = @fs_kd_jaminan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", model.JaminanName, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_jaminan", model.Address.Alamat, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", model.Address.Alamat2, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", model.Address.Kota, SqlDbType.VarChar);

        dp.AddParam("@fb_aktif", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@fs_kd_cara_bayar_dk", model.CaraBayarDk.CaraBayarDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminan.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_benefit_mou", model.BenefitMou, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJaminanKey key)
    {
        const string sql = @"
            DELETE FROM ta_jaminan
            WHERE fs_kd_jaminan = @fs_kd_jaminan ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public GetDataResult<JaminanModel> GetData2(IJaminanKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fs_alm1_jaminan, aa.fs_alm2_jaminan,
                aa.fs_kota_jaminan, aa.fb_aktif, aa.fs_benefit_mou, aa.fs_kd_cara_bayar_dk, 
                aa.fs_kd_grup_jaminan, 
                ISNULL(bb.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk,
                ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan
            FROM 
                ta_jaminan aa
                LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
                LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
            WHERE 
                aa.fs_kd_jaminan = @fs_kd_jaminan ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var data = conn.ReadSingle<JaminanDto>(sql, dp);
        var result = new GetDataResult<JaminanModel>(data.ToModel(), key.JaminanId);
        return result;
    }

    public ListDataResult<JaminanModel> ListData2()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fs_alm1_jaminan, aa.fs_alm2_jaminan,
                aa.fs_kota_jaminan, aa.fb_aktif, aa.fs_benefit_mou, aa.fs_kd_cara_bayar_dk, 
                aa.fs_kd_grup_jaminan, 
                ISNULL(bb.fs_nm_cara_bayar_dk, '') fs_nm_cara_bayar_dk,
                ISNULL(cc.fs_nm_grup_jaminan, '') fs_nm_grup_jaminan
            FROM 
                ta_jaminan aa
                LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
                LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<JaminanDto>(sql)?.ToList() ?? new List<JaminanDto>();
        var result = new ListDataResult<JaminanModel>(list.Select(x => x.ToModel()));
        return result;
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
        var expected = new JaminanModel("A", "B", AddressType.Default, false,
            CaraBayarDkModel.Default, GrupJaminanModel.Default, string.Empty);
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JaminanModel("A", "B", AddressType.Default, false,
            CaraBayarDkModel.Default, GrupJaminanModel.Default, string.Empty);
        _sut.Update(expected);
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var key = new JaminanKey("A");
        _sut.Delete(key);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JaminanModel("A", "B", AddressType.Default, false,
            CaraBayarDkModel.Default, GrupJaminanModel.Default, string.Empty);
        _sut.Insert(expected);
        var actual = _sut.GetData2(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JaminanModel("A", "B", AddressType.Default, false,
            CaraBayarDkModel.Default, GrupJaminanModel.Default, string.Empty);
        _sut.Insert(expected);
        var actual = _sut.ListData2().Value;
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}