using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KecamatanAgg;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiFeature;

public interface IKelurahanDal :
    IInsert<KelurahanDto>,
    IUpdate<KelurahanDto>,
    IDelete<IKelurahanKey>,
    IGetData<KelurahanDto, IKelurahanKey>,
    IListData<KelurahanDto, IKecamatanKey>,
    IListData<KelurahanDto, string>
{
}
public class KelurahanDal : IKelurahanDal
{
    private readonly DatabaseOptions _opt;

    public KelurahanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KelurahanDto dto)
    {
        const string sql = @"
            INSERT INTO 
                ta_kelurahan(fs_kd_kelurahan, fs_nm_kelurahan, fs_kd_kecamatan)
            VALUES 
                (@fs_kd_kelurahan, @fs_nm_kelurahan, @fs_kd_kecamatan)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", dto.fs_kd_kelurahan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", dto.fs_nm_kelurahan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", dto.fs_kd_kecamatan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KelurahanDto dto)
    {
        const string sql = @"
            UPDATE ta_kelurahan
            SET fs_nm_kelurahan = @fs_nm_kelurahan,
                fs_kd_kecamatan = @fs_kd_kecamatan
            WHERE fs_kd_kelurahan = @fs_kd_kelurahan";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kelurahan", dto.fs_kd_kelurahan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kelurahan", dto.fs_nm_kelurahan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kecamatan", dto.fs_kd_kecamatan, SqlDbType.VarChar);

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

    public KelurahanDto GetData(IKelurahanKey key)
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
        return conn.ReadSingle<KelurahanDto>(sql, dp);
    }

    public IEnumerable<KelurahanDto> ListData(IKecamatanKey filter)
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
        return conn.Read<KelurahanDto>(sql, dp);
    }
    private static string EscapeForContains(string term)
    {
        return "\"" + term.Replace("\"", "\"\"") + "*\"";
    }
    public IEnumerable<KelurahanDto> ListData(string filter)
    {
        var clearFilter = EscapeForContains(filter);
        var containerKel = $"CONTAINS(fs_nm_kelurahan, '{clearFilter}')";
        var containerKec = $"CONTAINS(fs_nm_kecamatan, '{clearFilter}')";
        
        var sql = $"""
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
                {containerKel}
                OR {containerKec}
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<KelurahanDto>(sql) ?? [];
    }
}
