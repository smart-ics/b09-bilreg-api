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
using Moq;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.TipeLayananDkAgg;

public class TipeLayananDkDal : ITipeLayananDkDal
{
    private readonly DatabaseOptions _opt;

    public TipeLayananDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public TipeLayananDkModel GetData(ITipeLayananDkKey key)
    {
        // Query
        const string sql = @"
                SELECT fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk
                FROM ta_layanan_tipe_dk
                WHERE fs_kd_layanan_tipe_dk = @fs_kd_layanan_tipe_dk";

        // Param
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_layanan_tipe_dk", key.TipeLayananDkId, SqlDbType.VarChar);

        // Execute

        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<TipeLayananDkDto>(sql, dp);
        return result?.ToModel()!;
    }

    public IEnumerable<TipeLayananDkModel> ListData()
    {
        //Query
        const string sql = @"
                SELECT fs_kd_layanan_tipe_dk, fs_nm_layanan_tipe_dk
                FROM ta_layanan_tipe_dk";
        //Execute
        var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<TipeLayananDkDto>(sql);
        return result?.Select(x => x.ToModel())!;
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
    private readonly Mock<ITipeLayananDkDal> _tipeLayananDkDal;

    public TipeLayananDkDalTest()
    {
        _tipeLayananDkDal = new Mock<ITipeLayananDkDal>();
        _sut = new TipeLayananDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GivenNonExistData_ThenReturnNullTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = TipeLayananDkModel.Create("A", "B");

        // Ambil data
        var actual = _sut.GetData(expected);

        // Assert
        actual.Should().BeNull();
    }

    [Fact]
    public void GivenEmptyData_ThenReturnEmptyListTest()
    {
        using var trans = TransHelper.NewScope();

        // Simulasikan tidak ada data
        _tipeLayananDkDal.Setup(x => x.ListData())
            .Returns(Enumerable.Empty<TipeLayananDkModel>());

        // Ambil data
        var actual = _tipeLayananDkDal.Object.ListData().ToList();

        // Assert
        actual.Should().BeEmpty();
    }

}

