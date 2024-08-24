using Bilreg.Infrastructure.Helpers;
using Dapper;
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
using FluentAssertions;
using Bilreg.Application.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.TipeLayananDkAgg;
using Bilreg.Infrastructure.AdmisiContext.LayananSub.TipeLayananDkAgg;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.TipeLayananDkAgg;

public class TipeLayananDkDal : ITipeLayananDkDal
{
    private readonly DatabaseOptions _opt;

    public TipeLayananDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeLayananDkModel model)
    {
        const string sql = @"
                INSERT INTO ta_layanan_tipe_dk(fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk)
                VALUES (@fs_kd_layanan_tipe_dk, @fs_nm_layanan_tipe_dk)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan_tipe_dk", model.TipeLayananDkId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_layanan_tipe_dk", model.TipeLayananDkName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeLayananDkModel GetData(ITipeLayananDkKey key)
    {
        const string sql = @"
                SELECT fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk
                FROM ta_layanan_tipe_dk
                WHERE fs_kd_layanan_tipe_dk = @fs_kd_layanan_tipe_dk";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan_tipe_dk", key.TipeLayananDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        TipeLayananDkDto result = conn.ReadSingle<TipeLayananDkDto>(sql, dp);
        return result?.ToModel();
    }

    public IEnumerable<TipeLayananDkModel> ListData()
    {
        const string sql = @"
                SELECT fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk
                FROM ta_layanan_tipe_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<TipeLayananDkDto>(sql);
        return result?.Select(x => x.ToModel());
    }
}

public class TipeLayananDkDto
{
    public string fs_kd_layanan_tipe_dk { get; set; }
    public string fs_nm_layanan_tipe_dk { get; set; }
    public TipeLayananDkModel ToModel() => TipeLayananDkModel.Create(fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk);
}

public class TipeLayananDkDalTest
{
private readonly TipeLayananDkDal _sut;

public TipeLayananDkDalTest()
{
    _sut = new TipeLayananDkDal(ConnStringHelper.GetTestEnv());
}
[Fact]
public void GetDataTest()
{
    using var trans = TransHelper.NewScope();
    var expected = TipeLayananDkModel.Create("1", "UNIT BEDAH");
    _sut.GetData(expected);
    var actual = _sut.GetData(expected);
    actual.Should().BeEquivalentTo(expected);
}

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();

        // Insert data baru
        var expected1 = TipeLayananDkModel.Create("9", "Pijet");
        var expected2 = TipeLayananDkModel.Create("10", "Kretek");
        _sut.Insert(expected1);
        _sut.Insert(expected2);

        // Ambil data
        var actual = _sut.ListData().ToList();

        // Assert
        actual.Should().Contain(x => x.TipeLayananDkId == "9" && x.TipeLayananDkName == "Pijet");
        actual.Should().Contain(x => x.TipeLayananDkId == "10" && x.TipeLayananDkName == "Kretek");
    }

}
