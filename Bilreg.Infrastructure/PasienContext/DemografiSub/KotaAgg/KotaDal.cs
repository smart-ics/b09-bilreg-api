using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiSub.KotaAgg;
using Bilreg.Domain.PasienContext.DemografiSub.KotaAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KotaAgg;

public class KotaDal: IKotaDal
{
    private readonly DatabaseOptions _opt;

    public KotaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KotaModel model)
    {
        const string sql = @"
            INSERT INTO ta_kota (fs_kd_kota, fs_nm_kota)
            VALUES (@fs_kd_kota, @fs_nm_kota)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KotaModel model)
    {
        const string sql = @"
            UPDATE ta_kota
            SET fs_nm_kota = @fs_nm_kota
            WHERE fs_kd_kota = @fs_kd_kota";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", model.KotaId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kota", model.KotaName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKotaKey key)
    {
        const string sql = @"
            DELETE FROM ta_kota 
            WHERE fs_kd_kota = @fs_kd_kota";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KotaModel GetData(IKotaKey key)
    {
        const string sql = @"
            SELECT fs_kd_kota, fs_nm_kota
            FROM ta_kota
            WHERE fs_kd_kota = @fs_kd_kota";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kota", key.KotaId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KotaDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<KotaModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_kota, fs_nm_kota
            FROM ta_kota";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KotaDto>(sql);
        return result?.Select(x => x.ToModel())!;
    }
}

public class KotaDalTest
{
    private readonly KotaDal _sut;
    
    public KotaDalTest()
    {
        _sut = new KotaDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KotaModel.Create("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KotaModel.Create("A", "B");
        _sut.Update(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KotaModel.Create("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KotaModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = KotaModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        _ = actual.Select(x => x.Should().BeEquivalentTo(expected));
    }
}