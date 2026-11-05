using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsDirectDalTest
{
    private const string TrsId = "BL-TEST-DAL-001";
    private readonly TrsDirectDal _sut = new(ConnStringHelper.GetTestEnv());

    private static void InsertSampleDirect(string trsId)
    {
        const string sql = """
            INSERT INTO ta_trs_direct (
                FS_KD_TRS, FD_TGL_TRS, FS_JAM_TRS, fd_tgl_jam_trs, fn_genkeu)
            VALUES (
                @trsId, '3000-01-01', '00:00:00', '3000-01-01 00:00:00', 0)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@trsId", trsId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        conn.Execute(sql, dp);
    }

    private static (string tgl, string jam, string tglJam, decimal genKeu) GetRow(string trsId)
    {
        const string sql = """
            SELECT
                ISNULL(FD_TGL_TRS, '') AS tgl,
                ISNULL(FS_JAM_TRS, '') AS jam,
                ISNULL(fd_tgl_jam_trs, '') AS tglJam,
                ISNULL(fn_genkeu, 0) AS genKeu
            FROM ta_trs_direct
            WHERE FS_KD_TRS = @trsId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@trsId", trsId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(ConnStringHelper.GetTestEnv().Value));
        return conn.QuerySingle<(string, string, string, decimal)>(sql, dp);
    }

    [Fact]
    public void GivenExistingDirectRow_WhenUpdateTimestamp_ThenColumnsAreUpdated()
    {
        // Given
        using var trans = TransHelper.NewScope();
        InsertSampleDirect(TrsId);
        var targetDate = new DateTime(2026, 6, 17, 14, 30, 15);
        const decimal targetGenKeu = 5m;

        // When
        _sut.UpdateTimestamp(TrsId, targetDate, targetGenKeu);

        // Then
        var (tgl, jam, tglJam, genKeu) = GetRow(TrsId);
        tgl.Should().Be("2026-06-17");
        jam.Should().Be("14:30:15");
        tglJam.Should().Be("2026-06-17 14:30:15");
        genKeu.Should().Be(5m);
    }

    [Fact]
    public void GivenFinalizedDirectRow_WhenRevertTimestampToSentinel_ThenColumnsAreReverted()
    {
        // Given
        using var trans = TransHelper.NewScope();
        const string trsId = "BL-TEST-DAL-002";
        InsertSampleDirect(trsId);
        var finalDate = new DateTime(2026, 6, 17, 14, 30, 15);
        _sut.UpdateTimestamp(trsId, finalDate, 5m);

        var sentinelDate = new DateTime(3000, 1, 1, 0, 0, 0);

        // When
        _sut.UpdateTimestamp(trsId, sentinelDate, 0m);

        // Then
        var (tgl, jam, tglJam, genKeu) = GetRow(trsId);
        tgl.Should().Be("3000-01-01");
        jam.Should().Be("00:00:00");
        tglJam.Should().Be("3000-01-01 00:00:00");
        genKeu.Should().Be(0m);
    }

    [Fact]
    public void GivenServiceCollection_WhenRegistered_ThenITrsDirectDalCanBeResolved()
    {
        // Given
        var services = new ServiceCollection();
        services.AddScoped<IOptions<DatabaseOptions>>(_ => ConnStringHelper.GetTestEnv());
        services.AddScoped<ITrsDirectDal, TrsDirectDal>();
        services.AddScoped<Bilreg.Application.PaymentContext.TrsBillingFeature.ITrsDirectDal, TrsDirectDal>();

        var provider = services.BuildServiceProvider();

        // When
        var infraDal = provider.GetService<ITrsDirectDal>();
        var appDal = provider.GetService<Bilreg.Application.PaymentContext.TrsBillingFeature.ITrsDirectDal>();

        // Then
        infraDal.Should().NotBeNull();
        appDal.Should().NotBeNull();
        infraDal.Should().BeOfType<TrsDirectDal>();
        appDal.Should().BeOfType<TrsDirectDal>();
    }
}
