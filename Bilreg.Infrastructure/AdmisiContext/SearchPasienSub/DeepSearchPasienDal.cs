using Bilreg.Application.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.Intrinsics.Arm;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class DeepSearchPasienDal : IDeepSearchPasienDal
{
    private readonly DatabaseOptions _opt;

    public DeepSearchPasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public MayBe<IEnumerable<SearchPasienModel>> ListData(string keyword)
    {
        var sql = @$"{SelectClause()}
            WHERE
                aa.fs_mr LIKE @keyword 
	            OR aa.fd_tgl_lahir LIKE @keyword
	            OR cc.NoID LIKE @keyword";
        var dp = new DynamicParameters();
        dp.AddParam("@keyword", keyword, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<SearchPasienDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }

    public MayBe<IEnumerable<SearchPasienModel>> ListData(IEnumerable<string> filter)
    {
        var listFilter = filter.Skip(1)
            .Select(x => $"OR aa.fs_nm_pasien LIKE '%{x}%'")
            .ToList();

        var sql = @$"{SelectClause()}
            WHERE
                aa.fs_nm_pasien LIKE '%{filter.First()}%'
            {string.Join(" ", listFilter)}";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<SearchPasienDto>(sql))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;

    }

    private static string SelectClause() => @"
        SELECT
	        aa.fs_mr AS PasienId, aa.fs_nm_pasien AS PasienName,
	        aa.fd_tgl_lahir AS Tgllahir, aa.fs_nm_ibu_kandung AS IbuKandung,
	        aa.fs_alm_pasien AS AlamatPasien, 
	        aa.fs_kota_pasien AS Kota, aa.fs_kd_pos_pasien AS KodePos,
	        ISNULL(bb.fs_nm_jenis_kelamin, '')  AS GenderName,
	        ISNULL(cc.JenisId, '') AS JenisId,
	        ISNULL(cc.NoId, '') AS NoId,
	        ISNULL(dd.fs_sex_dk, '') AS GenderId,
	        ISNULL(dd.fs_nm_jenis_kelamin, '') AS GenderName,
	        '' as RegId, '' AS BookingId
        FROM
	        tc_mr aa
	        LEFT JOIN ta_jenis_kelamin bb ON aa.fs_jns_kelamin = bb.fs_kd_jenis_kelamin
	        LEFT JOIN tc_mr_id cc ON aa.fs_mr = cc.fs_mr AND cc.JenisID = 'KTP'";
}
