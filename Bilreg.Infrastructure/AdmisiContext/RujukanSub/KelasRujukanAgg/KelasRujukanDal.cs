using Bilreg.Application.AdmisiContext.RujukanSub.KelasRujukanAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.KelasRujukanAgg;
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
using Nuna.Lib.DataAccessHelper;
using FluentAssertions;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.KelasRujukanAgg;

public class KelasRujukanDal : IKelasRujukanDal
{
    private readonly DatabaseOptions _opt;

    public KelasRujukanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public KelasRujukanModel GetData(IKelasRujukanKey key)
    {
        const string sql = @"
            SELECT fs_kd_kelas_rs, fs_nm_kelas_rs, fn_nilai
            FROM tc_kelas_rs
            WHERE fs_kd_kelas_rs = @fs_kd_kelas_rs";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelas_rs", key.KelasRujukanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KelasRujukanDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<KelasRujukanModel> ListData()
    {
        const string sql = @"
            SELECT fs_kd_kelas_rs, fs_nm_kelas_rs, fn_nilai
            FROM tc_kelas_rs";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KelasRujukanDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}

public class KelasRujukanDto
{
    public string fs_kd_kelas_rs { get; set; }
    public string fs_nm_kelas_rs { get; set; }
    public decimal fn_nilai { get; set; }

    public KelasRujukanModel ToModel() => KelasRujukanModel.Create(fs_kd_kelas_rs, fs_nm_kelas_rs, fn_nilai);
}


public class KelasRujukanDalTest
{
    private readonly KelasRujukanDal _sut;

    public KelasRujukanDalTest()
    {
        _sut = new KelasRujukanDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GetDataTest()
    {
        // ARRANGE
        var testData = KelasRujukanModel.Create("1", "Tingkat I", 5);

        // ACT
        var actual = _sut.GetData(testData);

        // ASSERT
        actual.Should().BeEquivalentTo(testData);
    }

    [Fact]
    public void ListDataTest()
    {
        // ACT
        var actual = _sut.ListData().ToList();

        // ASSERT
        actual.Should().Contain(x => x.KelasRujukanId == "1" && x.KelasRujukanName == "Tingkat I" && x.Nilai == 5);
        actual.Should().Contain(x => x.KelasRujukanId == "2" && x.KelasRujukanName == "Tingkat II" && x.Nilai == 4);
        
    }
}


