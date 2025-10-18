using Bilreg.Application.AdmisiContext.SearchPasienSub;
using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class DeepSearchPasienDal : IDeepSearchPasienDal
{
    private readonly DatabaseOptions _opt;
    private readonly DynamicParameters dp;
    public DeepSearchPasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
        dp = new DynamicParameters();
    }
    
    public MayBe<IEnumerable<SearchPasienType>> ListData(IEnumerable<SearchPasienType> filter)
    {
        string sql = string.Empty;
        if (filter.First().PasienId != "-")
            sql = $@"{SelectClause()} {WhereClausePasienId(filter.First().PasienId)}";

        
        if (filter.First().TglLahir.Year != 3000)
            sql = $@"{SelectClause()} {WhereClauseTglLahir(filter.First().TglLahir.ToString("yyyy-MM-dd"))}";

        if (filter.First().PasienName != "-")
        {
            var namaArray = filter
                .Select(x => x.PasienName)
                .Distinct()
                .ToArray();
            sql = $@"{SelectClause()} {WhereClausePasienName(namaArray)}";

        }

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<SearchPasienDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }

    private string WhereClauseTglLahir(string tglLahir)
    {
        var result = 
            @"WHERE
                        aa.fd_tgl_lahir = @Keyword";
        //var dp = new DynamicParameters();
        dp.AddParam("@Keyword", tglLahir, SqlDbType.VarChar);
        return result;
    }
    private string WhereClausePasienId(string pasienId)
    {
        var result =
            @$"WHERE
                        aa.fs_mr LIKE '%{ pasienId }%'";
        
        return result;
    }

    private string WhereClausePasienName(IEnumerable<string> pasienNames)
    {
        var listFilter = pasienNames.Skip(1)
            .Select(x => $"OR aa.fs_nm_pasien LIKE '%{x}%'")
            .ToList();

        var result = @$"
                WHERE
                    aa.fs_nm_pasien LIKE '%{pasienNames.First()}%'
                {string.Join(" ", listFilter)}";

        return result;
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
	        LEFT JOIN tc_mr_id cc ON aa.fs_mr = cc.fs_mr AND cc.JenisID = 'KTP'
            LEFT JOIN ta_jenis_kelamin dd ON aa.fs_jns_kelamin = dd.fs_kd_jenis_kelamin";

    
}
