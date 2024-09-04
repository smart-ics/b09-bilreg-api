using Bilreg.Application.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Domain.BillContext.RekapCetakSub.RekapCetakDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using System.Data;
using System.Data.SqlClient;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RekapCetakSub.RekapCetakDkAgg;
public class RekapCetakDkDal : IRekapCetakDkDal
{
    private DatabaseOptions _opt;

    public RekapCetakDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public RekapCetakDkModel GetData(IRekapCetakDkKey key)
    {
        const string sql = @"
                SELECT 
                    fs_kd_rekap_cetak_dk,fs_nm_rekap_cetak_dk
                FROM
                    ta_rekap_cetak_dk
                WHERE 
                    fs_kd_rekap_cetak_dk = @fs_kd_rekap_cetak_dk
                ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_rekap_cetak_dk", key.RekapCetakDkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RekapCetakDkDto>(sql, dp);  
    }

    public IEnumerable<RekapCetakDkModel> ListData()
    {
        const string sql = @"
                SELECT 
                    fs_kd_rekap_cetak_dk,fs_nm_rekap_cetak_dk
                FROM
                    ta_rekap_cetak_dk
                ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RekapCetakDkDto>(sql);
    }
}

public class RekapCetakDkDto : RekapCetakDkModel
{
    public RekapCetakDkDto() : base(string.Empty, string.Empty)
    {
    }
        
    public string fs_kd_rekap_cetak_dk { get => RekapCetakDkId; set => RekapCetakDkId = value; }
    public string fs_nm_rekap_cetak_dk { get => RekapCetakDkName; set => RekapCetakDkName = value; }
}

public class RekapCetakDkDalTest
{
    private readonly RekapCetakDkDal _sut;

    public RekapCetakDkDalTest()
    {
        _sut = new RekapCetakDkDal(ConnStringHelper.GetTestEnv());
    }

    [Fact]
    public void GetData_ShouldReturnCorrectData_WhenRekapCetakDkIdExists()
    {
        // Arrange
        var expectedId = "AD";
        var expectedName = "Administrasi";
        var testData = new RekapCetakDkModel(expectedId, expectedName);

        // Act
        var actual = _sut.GetData(testData);

        // Assert
        actual.Should().NotBeNull();
        actual.RekapCetakDkId.Should().Be(expectedId);
        actual.RekapCetakDkName.Should().Be(expectedName);
    }

    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();

        var expected = new RekapCetakDkModel("AD", "Administrasi");
        var actual = _sut.ListData();

        actual.Should().ContainEquivalentOf(expected);
    }
}