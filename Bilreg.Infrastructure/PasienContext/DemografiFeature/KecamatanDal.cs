using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;

public class KecamatanDal : IKecamatanDal
{
    private readonly DatabaseOptions _opt;

    public KecamatanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KecamatanType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_kecamatan(fs_kd_kecamatan, fs_nm_kecamatan, fs_kd_kabupaten)
            VALUES 
                (@fs_kd_kecamatan, @fs_nm_kecamatan, @fs_kd_kabupaten)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kecamatan", model.KecamatanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kabupaten", model.Kabupaten.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KecamatanType model)
    {
        const string sql = @"
            UPDATE ta_kecamatan
            SET fs_nm_kecamatan = @fs_nm_kecamatan,
                fs_kd_kabupaten = @fs_kd_kabupaten
            WHERE fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", model.KecamatanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kecamatan", model.KecamatanName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kabupaten", model.Kabupaten.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKecamatanKey key)
    {
        const string sql = @"
            DELETE FROM ta_kecamatan
            WHERE fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", key.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<KecamatanType> GetData(IKecamatanKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kecamatan, aa.fs_nm_kecamatan, aa.fs_kd_kabupaten,
                ISNULL(bb.fs_nm_kabupaten, '-') fs_nm_kabupaten,
                ISNULL(bb.fs_kd_propinsi, '-') fs_kd_propinsi,
                ISNULL(cc.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kecamatan aa
                LEFT JOIN ta_kabupaten bb ON aa.fs_kd_kabupaten = bb.fs_kd_kabupaten
                LEFT JOIN ta_propinsi cc ON bb.fs_kd_propinsi = cc.fs_kd_propinsi
            WHERE fs_kd_kecamatan = @fs_kd_kecamatan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kecamatan", key.KecamatanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.ReadSingle<KecamatanDto>(sql, dp))
            .Map(x => x.ToModel());
    }

    public MayBe<IEnumerable<KecamatanType>> ListData(IKabupatenKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kecamatan, aa.fs_nm_kecamatan, aa.fs_kd_kabupaten,
                ISNULL(bb.fs_nm_kabupaten, '-') fs_nm_kabupaten,
                ISNULL(bb.fs_kd_propinsi, '-') fs_kd_propinsi,
                ISNULL(cc.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kecamatan aa
                LEFT JOIN ta_kabupaten bb ON aa.fs_kd_kabupaten = bb.fs_kd_kabupaten
                LEFT JOIN ta_propinsi cc ON bb.fs_kd_propinsi = cc.fs_kd_propinsi
            WHERE 
                aa.fs_kd_kabupaten = @fs_kd_kabupaten";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", filter.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<KecamatanDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
    }

}

public class KecamatanDalTest
{
    private readonly KecamatanDal _sut;

    public KecamatanDalTest()
    {
        _sut = new KecamatanDal(ConnStringHelper.GetTestEnv());
    }

    private static KecamatanType Faker() =>
        new KecamatanType("A", "B",
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
        _sut.Delete(KecamatanType.Key("A"));
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
        var actual = _sut.ListData(KabupatenType.Key("-")).Value;
        actual.Should().ContainEquivalentOf(expected);
    }
}