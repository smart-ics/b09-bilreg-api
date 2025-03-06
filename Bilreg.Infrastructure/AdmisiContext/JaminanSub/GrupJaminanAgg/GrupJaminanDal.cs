using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.GrupJaminanAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.GrupJaminanAgg;

public class GrupJaminanDal: IGrupJaminanDal
{
    private readonly DatabaseOptions _opt;

    public GrupJaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(GrupJaminanModel model)
    {
        const string sql = @"
            INSERT INTO ta_grup_jaminan 
                (fs_kd_grup_jaminan, fs_nm_grup_jaminan, fb_karyawan, fs_keterangan)
            VALUES 
                (@fs_kd_grup_jaminan, @fs_nm_grup_jaminan, @fb_karyawan, @fs_keterangan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_jaminan", model.GrupJaminanName, SqlDbType.VarChar);
        dp.AddParam("@fb_karyawan", model.IsKaryawan, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(GrupJaminanModel model)
    {
        const string sql = @"
            UPDATE ta_grup_jaminan
            SET fs_nm_grup_jaminan = @fs_nm_grup_jaminan,
                fb_karyawan = @fb_karyawan,
                fs_keterangan = @fs_keterangan
            WHERE fs_kd_grup_jaminan = @fs_kd_grup_jaminan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_jaminan", model.GrupJaminanName, SqlDbType.VarChar);
        dp.AddParam("@fb_karyawan", model.IsKaryawan, SqlDbType.Bit);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IGrupJaminanKey key)
    {
        const string sql = @"
            DELETE FROM ta_grup_jaminan
            WHERE fs_kd_grup_jaminan = @fs_kd_grup_jaminan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", key.GrupJaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public GetDataResult<GrupJaminanModel> GetData2(IGrupJaminanKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_grup_jaminan AS GrupJaminanId, 
                fs_nm_grup_jaminan AS GrupJaminanName, 
                fb_karyawan AS IsKaryawan, 
                fs_keterangan AS Keterangan
            FROM 
                ta_grup_jaminan
            WHERE 
                fs_kd_grup_jaminan = @fs_kd_grup_jaminan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", key.GrupJaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var data = conn.ReadSingle<GrupJaminanModel>(sql, dp);
        var result = new GetDataResult<GrupJaminanModel>(data, key.GrupJaminanId);
        return result;
    }

    public ListDataResult<GrupJaminanModel> ListData2()
    {
        const string sql = @"
            SELECT 
                fs_kd_grup_jaminan AS GrupJaminanId, 
                fs_nm_grup_jaminan AS GrupJaminanName, 
                fb_karyawan AS IsKaryawan, 
                fs_keterangan AS Keterangan
            FROM 
                ta_grup_jaminan";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<GrupJaminanModel>(sql);
        var result = new ListDataResult<GrupJaminanModel>(list);
        return result;
    }
}

public class GrupJaminanDalTest
{
    private readonly GrupJaminanDal _sut;

    public GrupJaminanDalTest()
    {
        _sut = new GrupJaminanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupJaminanModel("A", "B", true, "C");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupJaminanModel("A", "B", true, "C");
        _sut.Update(expected);
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupJaminanModel("A", "B", true, "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupJaminanModel("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.GetData2(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new GrupJaminanModel("A", "B", true, "C");
        _sut.Insert(expected);
        var actual = _sut.ListData2().Value;
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}