using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiFeature;

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
