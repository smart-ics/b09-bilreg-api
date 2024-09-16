using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.ParamContext.ParamSistemAgg;
using Bilreg.Domain.ParamContext;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.ParamContext.ParamSistemAgg;

public class ParamSistemDal : IParamSistemDal
{
    private readonly DatabaseOptions _opt;

    public ParamSistemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ParamSistemModel model)
    {
        const string sql = @"
            INSERT INTO tz_parameter_sistem
                (fs_kd_parameter, fs_nm_parameter, fs_value)
            VALUES (@fs_kd_parameter, @fs_nm_parameter, @fs_value)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", model.ParamSistemId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_parameter", model.ParamSistemName, SqlDbType.VarChar);
        dp.AddParam("@fs_value", model.Value, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ParamSistemModel model)
    {
        const string sql = @"
            UPDATE tz_parameter_sistem
            SET
                fs_kd_parameter = @fs_kd_parameter, 
                fs_nm_parameter = @fs_nm_parameter, 
                fs_value  = @fs_value
            WHERE   
                fs_kd_parameter = @fs_kd_parameter";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", model.ParamSistemId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_parameter", model.ParamSistemName, SqlDbType.VarChar);
        dp.AddParam("@fs_value", model.Value, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IParamSistemKey key)
    {
        const string sql = @"
            DELETE FROM tz_parameter_sistem
            WHERE fs_kd_parameter = @fs_kd_parameter";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key.ParamSistemId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ParamSistemModel GetData(string key)
    {
        const string sql = @"
            SELECT fs_kd_parameter, fs_nm_parameter, fs_value
            FROM tz_parameter_sistem
            WHERE fs_kd_parameter = @fs_kd_parameter ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_parameter", key, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ParamSistemDto>(sql, dp);
    }

    public IEnumerable<ParamSistemModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_parameter, fs_nm_parameter, fs_value 
            FROM tz_parameter_sistem";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ParamSistemDto>(sql);
    }
    
    private class ParamSistemDto() : ParamSistemModel("","","")
    {
        public string fs_kd_parameter
        {
            get => base.ParamSistemId;
            set => ParamSistemId = value;
        }
        public string fs_nm_parameter
        {
            get => ParamSistemName; 
            set => ParamSistemName = value;
        }
        public string fs_value
        {
            get => Value; 
            set => Value = value;
        }
    }
}

public class ParamSistemDalTest
{
    private readonly ParamSistemDal _sut;

    public ParamSistemDalTest()
    {
        _sut = new ParamSistemDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        // Arrange
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Insert(expected);
    }
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Update(expected);
    }
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new ParamSistemModel("A", "B", "C");
        _sut.Insert(expected);
        var actual = _sut.GetData("A");
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected =  new ParamSistemModel("A", "B", "C") ;
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
}