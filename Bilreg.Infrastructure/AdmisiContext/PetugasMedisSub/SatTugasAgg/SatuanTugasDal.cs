using Bilreg.Application.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.SatTugasAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PetugasMedisSub.SatTugasAgg;
public class SatuanTugasDal : ISatuanTugasDal
{
    private readonly DatabaseOptions _opt;

    public SatuanTugasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(SatuanTugasModel model)
    {
        // QUERY
        const string sql = @"
                INSERT INTO td_sat_tugas(fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis)
                VALUES (@fs_kd_sat_tugas, @fs_nm_sat_tugas, @fb_sat_medis)";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", model.SatuanTugasId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", model.SatuanTugasName, SqlDbType.VarChar);
        dp.AddParam("@fb_sat_medis", model.IsMedis, SqlDbType.Bit);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(SatuanTugasModel model)
    {
        // QUERY
        const string sql = @"
                UPDATE td_sat_tugas
                SET fs_nm_sat_tugas = @fs_nm_sat_tugas,
                    fb_sat_medis = @fb_sat_medis
                WHERE fs_kd_sat_tugas = @fs_kd_sat_tugas";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", model.SatuanTugasId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_sat_tugas", model.SatuanTugasName, SqlDbType.VarChar);
        dp.AddParam("@fb_sat_medis", model.IsMedis, SqlDbType.Bit);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ISatuanTugasKey key)
    {
        // QUERY
        const string sql = @"
                DELETE FROM td_sat_tugas
                WHERE fs_kd_sat_tugas = @fs_kd_sat_tugas";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", key.SatuanTugasId, SqlDbType.VarChar);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public SatuanTugasModel GetData(ISatuanTugasKey key)
    {
        const string sql = @"
                SELECT fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis
                FROM td_sat_tugas
                WHERE fs_kd_sat_tugas = @fs_kd_sat_tugas";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_sat_tugas", key.SatuanTugasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        SatuanTugasDto result = conn.ReadSingle<SatuanTugasDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<SatuanTugasModel> ListData()
    {
        const string sql = @"
                SELECT fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis
                FROM td_sat_tugas";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<SatuanTugasDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}

public class SatuanTugasDto
{
    public string fs_kd_sat_tugas { get; set; }
    public string fs_nm_sat_tugas { get; set; }
    public bool fb_sat_medis { get; set; }
    public SatuanTugasModel ToModel() => SatuanTugasModel.Create(fs_kd_sat_tugas, fs_nm_sat_tugas, fb_sat_medis);
}

public class SatuanTugasDalTest
{
    private readonly SatuanTugasDal _sut;

    public SatuanTugasDalTest()
    {
        _sut = new SatuanTugasDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(SatuanTugasModel.Create("A", "B", true));
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(SatuanTugasModel.Create("A", "B", false));
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(SatuanTugasModel.Create("A", "B", true));
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = SatuanTugasModel.Create("A", "B", true);
        _sut.Insert(expected);
        var actual = _sut.GetData(expected);
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void GivenNotExistData_ThenReturnNull()
    {
        using var trans = TransHelper.NewScope();
        var expected = SatuanTugasModel.Create("A", "B", true);
        var actual = _sut.GetData(expected);
        actual.Should().BeNull();
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new List<SatuanTugasModel> { SatuanTugasModel.Create("A", "B", true) };
        _sut.Insert(SatuanTugasModel.Create("A", "B", true));
        var actual = _sut.ListData();
        actual.Should().BeEquivalentTo(expected);
    }
}
