using Bilreg.Application.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SmfAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.SmfAgg;
public class SmfDal : ISmfDal
{
    private readonly DatabaseOptions _opt;

    public SmfDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SmfModel model)
    {
        // QUERY
        const string sql = @"
                INSERT INTO ta_smf(fs_kd_smf, fs_nm_smf)
                VALUES (@fs_kd_smf, @fs_nm_smf)";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_smf", model.SmfName, SqlDbType.VarChar);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SmfModel model)
    {
        // QUERY
        const string sql = @"
                UPDATE ta_smf
                SET fs_nm_smf = @fs_nm_smf
                WHERE fs_kd_smf = @fs_kd_smf";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", model.SmfId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_smf", model.SmfName, SqlDbType.VarChar);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISmfKey key)
    {
        // QUERY
        const string sql = @"
                DELETE FROM ta_smf
                WHERE fs_kd_smf = @fs_kd_smf";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", key.SmfId, SqlDbType.VarChar);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SmfModel GetData(ISmfKey key)
    {
        const string sql = @"
                SELECT fs_kd_smf, fs_nm_smf
                FROM ta_smf
                WHERE fs_kd_smf = @fs_kd_smf";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_smf", key.SmfId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        SmfDto result = conn.ReadSingle<SmfDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<SmfModel> ListData()
    {
        const string sql = @"
                SELECT fs_kd_smf, fs_nm_smf
                FROM ta_smf";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<SmfDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}
public class SmfDto
{
    public string fs_kd_smf { get; set; }
    public string fs_nm_smf { get; set; }
    public SmfModel ToModel() => SmfModel.Create(fs_kd_smf, fs_nm_smf);
}

public class SmfDalTest
{
    private readonly SmfDal _sut;

    public SmfDalTest()
    {
        _sut = new SmfDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(SmfModel.Create("A", "B"));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(SmfModel.Create("A", "B"));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(SmfModel.Create("A", "B"));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = SmfModel.Create("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistDate_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = SmfModel.Create("A", "B");
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<SmfModel> { SmfModel.Create("A", "B") };
        _sut.Insert(SmfModel.Create("A", "B"));
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(expected);
    }
}

