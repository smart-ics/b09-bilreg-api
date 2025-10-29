using Bilreg.Domain.AdmisiContext.SearchPasienSub;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.AdmisiContext.SearchPasienSub;

public class QuickSearchPasienDal : IQuickSearchPasienDal
{
    private readonly DatabaseOptions _opt;

    public QuickSearchPasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public MayBe<IEnumerable<SearchPasienType>> ListData(Periode periode)
    {
        const string sql = @"
            SELECT 
				aa.TrackerId, aa.VisitorName as PasienName, aa.TglLahir, 
				aa.VisitDate, aa.RegId, 
				ISNULL(bb.fs_mr,'') AS PasienId,
				ISNULL(bb.fs_kd_booking,'') as BookingId,
				ISNULL(cc.fs_nm_ibu_kandung,'') AS IbuKandung,
				ISNULL(cc.fs_alm_pasien,'') AS AlamatPasien,
				ISNULL(cc.fs_kota_pasien, '') AS Kota, 
				ISNULL(cc.fs_kd_pos_pasien, '') AS KodePos,
				ISNULL(dd.fs_sex_dk,'') AS GenderId,
				ISNULL(dd.fs_nm_jenis_kelamin,'') AS GenderName,
				ISNULL(ee.JenisId, '') AS JenisId,
				ISNULL(ee.NoId, '') AS NoId
			FROM 
				BILRG_PasienTracker aa
				LEFT JOIN ta_registrasi bb ON aa.RegId = bb.fs_kd_reg 
				LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr 
				LEFT JOIN ta_jenis_kelamin dd ON cc.fs_jns_kelamin = dd.fs_kd_jenis_kelamin
				LEFT JOIN tc_mr_id ee ON bb.fs_mr = ee.fs_mr AND ee.JenisID = 'KTP'
			WHERE
				aa.VisitDate BETWEEN @Tgl1 AND @Tgl2";

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", periode.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", periode.Tgl2, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<SearcQuickhPasienDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }
}
