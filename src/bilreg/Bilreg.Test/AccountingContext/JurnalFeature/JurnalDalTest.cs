using System.Data.SqlClient;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.AccountingContext.JurnalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.AccountingContext.JurnalFeature;

public class JurnalDalTest
{
    private readonly JurnalDal _sut = new(ConnStringHelper.GetTestEnv());

    public JurnalDalTest()
    {
        EnsureTableExists();
    }

    private static void EnsureTableExists()
    {
        const string sql = """
            IF OBJECT_ID('tc_mr', 'U') IS NULL
            BEGIN
                CREATE TABLE tc_mr
                (
                    fs_mr VARCHAR(15) NOT NULL,
                    fs_nm_pasien VARCHAR(50) NOT NULL DEFAULT('')
                )
            END

            IF OBJECT_ID('t_jurnal_hdr', 'U') IS NULL
            BEGIN
                CREATE TABLE t_jurnal_hdr
                (
                    fs_kd_jurnal VARCHAR(26) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_jurnal DEFAULT(''),
                    fd_tgl_jurnal VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fd_tgl_jurnal DEFAULT('3000-01-01'),
                    fs_jam_jurnal VARCHAR(8) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_jam_jurnal DEFAULT('00:00:00'),
                    fs_kd_petugas VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_petugas DEFAULT(''),

                    fs_keterangan VARCHAR(250) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_keterangan DEFAULT(''),
                    fs_no_bukti1 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti1 DEFAULT(''),
                    fs_no_bukti2 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti2 DEFAULT(''),
                    fs_no_bukti3 VARCHAR(30) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_no_bukti3 DEFAULT(''),

                    fs_kd_reg VARCHAR(10) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_reg DEFAULT(''),
                    fs_kd_mr VARCHAR(15) NOT NULL CONSTRAINT DF_t_jurnal_hdr_fs_kd_mr DEFAULT(''),

                    CONSTRAINT PK_t_jurnal_hdr PRIMARY KEY CLUSTERED(fs_kd_jurnal)
                )
            END
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql);
    }

    private static JurnalDto Faker()
        => new JurnalDto(
            fs_kd_jurnal: "JUR001",
            fd_tgl_jurnal: "2025-01-01",
            fs_jam_jurnal: "10:00:00",
            fs_kd_petugas: "USR001",
            fs_keterangan: "Jurnal Testing",
            fs_no_bukti1: "INV001",
            fs_no_bukti2: "BK001",
            fs_no_bukti3: "REF001",
            fs_kd_reg: "REG001",
            fs_kd_mr: "MR001",
            fs_nm_pasien: "John Doe"
        );

    private static IJurnalKey FakerKey()
        => JurnalType.Default with { JurnalId = "JUR001" };

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(),
            opt => opt.Excluding(x => x.fs_nm_pasien));
    }

    [Fact]
    public void GivenExistingJurnal_WhenUpdateTimestamp_ThenFdTglAndFsJamUpdated()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var initial = Faker();
        _sut.Insert(initial);
        var targetDate = new DateTime(2026, 6, 17, 14, 30, 15);

        // When
        _sut.UpdateTimestamp(initial.fs_kd_jurnal, targetDate);

        // Then
        var actual = _sut.GetData(FakerKey());
        actual.fd_tgl_jurnal.Should().Be("2026-06-17");
        actual.fs_jam_jurnal.Should().Be("14:30:15");
    }

    [Fact]
    public void GivenFinalizedJurnal_WhenRevertTimestampToSentinel_ThenFdTglAndFsJamReverted()
    {
        // Given
        using var trans = TransHelper.NewScope();
        var initial = Faker();
        _sut.Insert(initial);
        var targetDate = new DateTime(2026, 6, 17, 14, 30, 15);
        _sut.UpdateTimestamp(initial.fs_kd_jurnal, targetDate);

        var sentinelDate = new DateTime(3000, 1, 1, 0, 0, 0);

        // When
        _sut.UpdateTimestamp(initial.fs_kd_jurnal, sentinelDate);

        // Then
        var actual = _sut.GetData(FakerKey());
        actual.fd_tgl_jurnal.Should().Be("3000-01-01");
        actual.fs_jam_jurnal.Should().Be("00:00:00");
    }

    [Fact]
    public void GivenServiceCollection_WhenRegistered_ThenIJurnalDalCanBeResolved()
    {
        // Given
        var services = new ServiceCollection();
        services.AddScoped<IOptions<DatabaseOptions>>(_ => ConnStringHelper.GetTestEnv());
        services.AddScoped<IJurnalDal, JurnalDal>();
        services.AddScoped<Bilreg.Application.AccountingContext.JurnalFeature.IJurnalDal, JurnalDal>();

        var provider = services.BuildServiceProvider();

        // When
        var infraDal = provider.GetService<IJurnalDal>();
        var appDal = provider.GetService<Bilreg.Application.AccountingContext.JurnalFeature.IJurnalDal>();

        // Then
        infraDal.Should().NotBeNull();
        appDal.Should().NotBeNull();
        infraDal.Should().BeOfType<JurnalDal>();
        appDal.Should().BeOfType<JurnalDal>();
    }
}
