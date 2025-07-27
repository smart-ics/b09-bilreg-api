using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KelurahanAgg;

public class KelurahanDal : IKelurahanDal
{
    private readonly DatabaseOptions _opt;

    public KelurahanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelurahanType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_kelurahan(fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_kecamatan)
            VALUES 
                (@fs_kd_kelurahan, @fs_nm_kelurahan, @fs_kd_kecamatan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", model.Kecamatan.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelurahanType model)
    {
        const string sql = @"
            UPDATE ta_kelurahan
            SET fs_nm_kelurahan = @fs_nm_kelurahan,
                fs_kd_kecamatan = @fs_kd_kecamatan
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", model.KelurahanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", model.KelurahanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", model.Kecamatan.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKelurahanKey key)
    {
        const string sql = @"
            DELETE FROM ta_kelurahan
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<KelurahanType> GetData(IKelurahanKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kelurahan, aa.fs_nm_kelurahan, aa.fs_kd_kecamatan,
                ISNULL(bb.fs_nm_kecamatan, '-') fs_nm_kecamatan,
                ISNULL(bb.fs_kd_kabupaten, '-') fs_kd_kabupaten,
                ISNULL(cc.fs_nm_kabupaten, '-') fs_nm_kabupaten,
                ISNULL(cc.fs_kd_propinsi, '-') fs_kd_propinsi,
                ISNULL(dd.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kelurahan aa
                LEFT JOIN ta_kecamatan bb ON aa.fs_kd_kecamatan = bb.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten cc ON bb.fs_kd_kabupaten = cc.fs_kd_kabupaten
                LEFT JOIN ta_propinsi dd ON cc.fs_kd_propinsi = dd.fs_kd_propinsi
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", key.KelurahanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.ReadSingle<KelurahanDto>(sql, dp))
            .Map(x => x.ToModel());
    }

    public MayBe<IEnumerable<KelurahanType>> ListData(IKecamatanKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kelurahan, aa.fs_nm_kelurahan, aa.fs_kd_kecamatan,
                ISNULL(bb.fs_nm_kecamatan, '-') fs_nm_kecamatan,
                ISNULL(bb.fs_kd_kabupaten, '-') fs_kd_kabupaten,
                ISNULL(cc.fs_nm_kabupaten, '-') fs_nm_kabupaten,
                ISNULL(cc.fs_kd_propinsi, '-') fs_kd_propinsi,
                ISNULL(dd.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kelurahan aa
                LEFT JOIN ta_kecamatan bb ON aa.fs_kd_kecamatan = bb.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten cc ON bb.fs_kd_kabupaten = cc.fs_kd_kabupaten
                LEFT JOIN ta_propinsi dd ON cc.fs_kd_propinsi = dd.fs_kd_propinsi
            WHERE 
                aa.fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", filter.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<KelurahanDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
    }

}

public class KelurahanDalTest
{
    private readonly KelurahanDal _sut;

    public KelurahanDalTest()
    {
        _sut = new KelurahanDal(ConnStringHelper.GetTestEnv());
    }

    private static KelurahanType Faker() =>
        new KelurahanType("A", "B",
            KecamatanType.Default.ToReff(),
            KabupatenType.Default.ToReff(),
            PropinsiType.Default);
    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(KelurahanType.Key("A"));
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.GetData(expected).Value;
        actual.Should().BeEquivalentTo(expected);
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = Faker();
        _sut.Insert(expected);
        var actual = _sut.ListData(KecamatanType.Key("-")).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}