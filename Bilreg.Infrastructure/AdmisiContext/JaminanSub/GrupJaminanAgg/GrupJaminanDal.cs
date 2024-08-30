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
            INSERT INTO ta_grup_jaminan (fs_kd_grup_jaminan, fs_nm_grup_jaminan, fb_karyawan, fs_keterangan)
            VALUES (@fs_kd_grup_jaminan, @fs_nm_grup_jaminan, @fb_karyawan, @fs_keterangan)";

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
                fs_keterangan = @fs_keterangan
            WHERE fs_kd_grup_jaminan = @fs_kd_grup_jaminan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", model.GrupJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_grup_jaminan", model.GrupJaminanName, SqlDbType.VarChar);
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

    public GrupJaminanModel GetData(IGrupJaminanKey key)
    {
        const string sql = @"
            SELECT fs_kd_grup_jaminan, fs_nm_grup_jaminan, fb_karyawan, fs_keterangan
            FROM ta_grup_jaminan
            WHERE fs_kd_grup_jaminan = @fs_kd_grup_jaminan";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_jaminan", key.GrupJaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<GrupJaminanDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<GrupJaminanModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_grup_jaminan, fs_nm_grup_jaminan, fb_karyawan, fs_keterangan
            FROM ta_grup_jaminan";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<GrupJaminanDto>(sql);
        return result?.Select(x => x.ToModel())!;
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
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _sut.Update(expected);
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = GrupJaminanModel.Create("A", "B", "C");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}