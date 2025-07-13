using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Domain.PasienContext.DemografiSub.PropinsiAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.PropinsiAgg;

public class PropinsiDal : IPropinsiDal
{
    private readonly DatabaseOptions _opt;

    public PropinsiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PropinsiType type)
    {
        const string sql = @"
            INSERT INTO ta_propinsi
                (fs_kd_propinsi, fs_nm_propinsi)
            VALUES(
                @fs_kd_propinsi, @fs_nm_propinsi)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", type.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", type.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PropinsiType type)
    {
        const string sql = @"
            UPDATE ta_propinsi
            SET fs_nm_propinsi  = @fs_nm_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", type.PropinsiId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_propinsi", type.PropinsiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPropinsiKey key)
    {
        const string sql = @"
            DELETE FROM ta_propinsi
            WHERE fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PropinsiType GetData(IPropinsiKey key)
    {
        const string sql = @"
            SELECT 
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM 
                ta_propinsi
            WHERE 
                fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", key.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<PropinsiType>(sql, dp);
        return result; //.ToModel();
    }

    public IEnumerable<PropinsiType> ListData()
    {
        const string sql = @"
            SELECT 
                fs_kd_propinsi AS PropinsiId, 
                fs_nm_propinsi AS PropinsiName
            FROM 
                ta_propinsi ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<PropinsiType>(sql);
        return result;
    }
}

public class PropinsiDalTest
{
    private readonly PropinsiDal _sut;

    public PropinsiDalTest()
    {
        _sut = new PropinsiDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(new PropinsiType("A", "B"));
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(new PropinsiType("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(new PropinsiType("A","A1"));
    }
    
    [Fact]
    public void GetTest()
    {
        using var trans = TransHelper.NewScope();
        var exp = new PropinsiType("A", "B");
        _sut.Insert(exp);
        var actual = _sut.GetData(exp);
        actual.Should().BeEquivalentTo(exp);
    }
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var exp = new PropinsiType("A", "B");
        _sut.Insert(exp);
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(new List<PropinsiType>(){exp});
    }
}