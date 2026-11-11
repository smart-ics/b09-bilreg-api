using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Test.ChargeContext.PricingBarangFeature;

/// <summary>
/// Integration tests DAL parameter pembulatan kasir terhadap tabel legacy
/// <c>tz_parameter_sistem</c> (<c>FAR_DBUMUM_PEMBULATAN</c> dan
/// <c>FAR_DBUMUM_PEMBULATAN_ATAS</c>). Skema legacy hanya tersedia pada basis data uji
/// <c>HOSPITAL_PKL</c> (repositori pattern: PricingBarangFeature).
/// Setiap test berjalan di dalam <c>TransHelper.NewScope()</c> yang selalu di-rollback sehingga
/// nilai parameter sistem nyata tidak pernah berubah permanen.
/// </summary>
[Collection("PricingBarangDatabaseTests")]
public class RoundingPolicyDalTest
{
    private const string IncrementKey = RoundingPolicyDal.PembulatanKey;
    private const string ModeKey = RoundingPolicyDal.PembulatanAtasKey;

    private readonly IOptions<DatabaseOptions> _dbOptions;
    private readonly RoundingPolicyDal _sut;
    private readonly RoundingPolicyRepo _repo;

    public RoundingPolicyDalTest()
    {
        _dbOptions = Options.Create(new DatabaseOptions
        {
            ServerName = "dev.smart-ics.com",
            DbName = "HOSPITAL_PKL"
        });
        _sut = new RoundingPolicyDal(_dbOptions);
        _repo = new RoundingPolicyRepo(_sut);
    }

    private void DeleteKeys()
    {
        const string sql = """
            DELETE FROM tz_parameter_sistem
            WHERE fs_kd_parameter IN (@k1, @k2)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { k1 = IncrementKey, k2 = ModeKey });
    }

    private void SetRawValue(string key, string name, string value)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM tz_parameter_sistem WHERE fs_kd_parameter = @k)
            BEGIN
                UPDATE tz_parameter_sistem SET fs_value = @v WHERE fs_kd_parameter = @k
            END
            ELSE
            BEGIN
                INSERT INTO tz_parameter_sistem (fs_kd_parameter, fs_nm_parameter, fs_value)
                VALUES (@k, @n, @v)
            END
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_dbOptions.Value));
        conn.Execute(sql, new { k = key, n = name, v = value });
    }

    [Fact]
    public void GetPolicy_WhenKeysAbsent_ReturnsFallbackDefaults()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();

        var actual = _sut.GetPolicy();

        actual.NilaiPembulatan.Should().Be(100);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.KeAtas);
    }

    [Fact]
    public void SavePolicy_WhenKeysAbsent_InsertsBothParameters()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();

        _sut.SavePolicy(new RoundingPolicyDto(500, RoundingModeEnum.Normal));

        var actual = _sut.GetPolicy();
        actual.NilaiPembulatan.Should().Be(500);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.Normal);
    }

    [Fact]
    public void SavePolicy_Twice_UpdatesExistingParameters()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();

        _sut.SavePolicy(new RoundingPolicyDto(100, RoundingModeEnum.KeAtas));
        _sut.SavePolicy(new RoundingPolicyDto(1000, RoundingModeEnum.Normal));

        var actual = _sut.GetPolicy();
        actual.NilaiPembulatan.Should().Be(1000);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.Normal);
    }

    [Fact]
    public void GetPolicy_WhenModeValueIsZero_ReturnsNormal()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();
        SetRawValue(IncrementKey, "Pembulatan Nilai Tagihan", "500");
        SetRawValue(ModeKey, "Pembulatan Ke Atas", "0");

        var actual = _sut.GetPolicy();

        actual.NilaiPembulatan.Should().Be(500);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.Normal);
    }

    [Fact]
    public void GetPolicy_WhenIncrementInvalid_FallsBackToDefaultIncrement()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();
        SetRawValue(IncrementKey, "Pembulatan Nilai Tagihan", "abc");
        SetRawValue(ModeKey, "Pembulatan Ke Atas", "1");

        var actual = _sut.GetPolicy();

        actual.NilaiPembulatan.Should().Be(100);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.KeAtas);
    }

    [Fact]
    public void Repo_RoundTripsPolicyModel()
    {
        using var trans = TransHelper.NewScope();
        DeleteKeys();

        _repo.SavePolicy(new RoundingPolicyModel(200, RoundingModeEnum.Normal));

        var actual = _repo.GetPolicy();
        actual.NilaiPembulatan.Should().Be(200);
        actual.JenisPembulatan.Should().Be(RoundingModeEnum.Normal);
    }
}
