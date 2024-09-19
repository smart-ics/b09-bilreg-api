using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.JenisTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.JenisTarifAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.JenisTarifAgg;

public class JenisTarifDal : IJenisTarifDal
{
    private readonly DatabaseOptions _opt;

    public JenisTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JenisTarifModel model)
    {
        const string sql = @"
            INSERT INTO ta_jenis_tarif(fs_kd_jenis_tarif, fs_nm_jenis_tarif)
            VALUES (@fs_kd_jenis_tarif, @fs_nm_jenis_tarif)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", model.JenisTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", model.JenisTarifName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JenisTarifModel model)
    {
        const string sql = @"
            UPDATE ta_jenis_tarif
            SET fs_nm_jenis_tarif = @fs_nm_jenis_tarif 
            VALUES fs_kd_jenis_tarif = @fs_kd_jenis_tarif ";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", model.JenisTarifId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jenis_tarif", model.JenisTarifName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJenisTarifKey key)
    {
        const string sql = @"
            DELETE FROM ta_jenis_tarif
            WHERE fs_kd_jenis_tarif = @fs_kd_jenis_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JenisTarifModel GetData(IJenisTarifKey key)
    {
        const string sql = @"
            SELECT fs_kd_jenis_tarif, fs_nm_jenis_tarif
            FROM ta_jenis_tarif
            WHERE fs_kd_jenis_tarif = @fs_kd_jenis_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jenis_tarif", key.JenisTarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<JenisTarifDto>(sql, dp);
    }

    public IEnumerable<JenisTarifModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_jenis_tarif, fs_nm_jenis_tarif
            FROM ta_jenis_tarif";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisTarifDto>(sql);
    }
}

public class JenisTarifDto() : JenisTarifModel(string.Empty, string.Empty)
{
    public string fs_kd_jenis_tarif { get => JenisTarifId; set => JenisTarifId = value; }
    public string fs_nm_jenis_tarif { get => JenisTarifName; set => JenisTarifName = value; }
}

public class JenisTarifDalTest
{
    private readonly JenisTarifDal _sut;

    public JenisTarifDalTest()
    {
        _sut = new JenisTarifDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JenisTarifModel("A", "B");
        _sut.Insert(expected);
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JenisTarifModel("A", "B");
        _sut.Insert(expected);
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JenisTarifModel("A", "B");
        _sut.Delete(expected);
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JenisTarifModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new JenisTarifModel("A", "B");
        _sut.Insert(expected);
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(expected);
    }
    
}