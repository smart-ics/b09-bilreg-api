using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananAgg;

public class LayananDal : ILayananDal
{
    private readonly DatabaseOptions _opt;

    public LayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public MayBe<LayananType> GetData(ILayananKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_layanan AS LayananId, aa.fs_nm_layanan AS LayananName, 
                aa.fb_aktif AS IsAktif, aa.fs_kd_instalasi AS InstalasiId, 
                aa.fs_kd_layanan_dk AS LayananDkId, aa.fs_kd_layanan_tipe_dk AS LayananTipeDkId, 
                aa.fs_kd_smf AS SmfId,  
                ISNULL(bb.fs_nm_instalasi,'') AS InstalasiName,
                ISNULL(bb.fs_kd_instalasi_Dk,'') AS InstalasiDkId,
	            ISNULL(cc.fs_nm_layanan_dk,'') AS LayananDkName,
                ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS LayananTipeDKName,
	            ISNULL(ee.fs_nm_instalasi_dk,'') AS InstalasiDKName
            FROM 
                ta_layanan aa
                LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
                LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
                LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
                LEFT JOIN ta_instalasi_dk ee ON bb.fs_kd_instalasi_dk = ee.fs_Kd_instalasi_dk
            WHERE
                aa.fs_kd_layanan = @key
        ";
        var dp = new DynamicParameters();
        dp.AddParam("@key", key.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.ReadSingle<LayananDto>(sql, dp))
            .Map(x => x.ToModel());
        return datas;
    }

    public MayBe<IEnumerable<LayananType>> ListData()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_layanan AS LayananId, aa.fs_nm_layanan AS LayananName, 
                aa.fb_aktif AS IsAktif, aa.fs_kd_instalasi AS InstalasiId, 
                aa.fs_kd_layanan_dk AS LayananDkId, aa.fs_kd_layanan_tipe_dk AS LayananTipeDkId, 
                aa.fs_kd_smf AS SmfId,  
                ISNULL(bb.fs_nm_instalasi,'') AS InstalasiName,
                ISNULL(bb.fs_kd_instalasi_Dk,'') AS InstalasiDkId,
	            ISNULL(cc.fs_nm_layanan_dk,'') AS LayananDkName,
                ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS LayananTipeDKName,
	            ISNULL(ee.fs_nm_instalasi_dk,'') AS InstalasiDKName
            FROM 
                ta_layanan aa
                LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
                LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
                LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
                LEFT JOIN ta_instalasi_dk ee ON bb.fs_kd_instalasi_dk = ee.fs_Kd_instalasi_dk
            ";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var datas = MayBe
            .From(conn.Read<LayananDto>(sql))
            .Map(x => x.Select(y => y.ToModel()));
        return datas;
    }


    //
    //     public void Insert(LayananModel model)
    //     {
    //         const string sql = @"
    //             INSERT INTO ta_layanan (
    //                 FS_KD_LAYANAN, 
    //                 FS_NM_LAYANAN, 
    //                 FB_AKTIF, 
    //                 FS_KD_INSTALASI, 
    //                 FS_KD_LAYANAN_DK, 
    //                 FS_KD_LAYANAN_TIPE_DK, 
    //                 FS_KD_SMF, 
    //                 FS_KD_MEDIS
    //             ) 
    //             VALUES (
    //                 @FS_KD_LAYANAN, 
    //                 @FS_NM_LAYANAN, 
    //                 @FB_AKTIF, 
    //                 @FS_KD_INSTALASI, 
    //                 @FS_KD_LAYANAN_DK, 
    //                 @FS_KD_LAYANAN_TIPE_DK, 
    //                 @FS_KD_SMF, 
    //                 @FS_KD_PEG        
    //             );";
    //
    //         var dp = new DynamicParameters();
    //         dp.AddParam("@FS_KD_LAYANAN", model.LayananId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_NM_LAYANAN", model.LayananName, SqlDbType.VarChar);
    //         dp.AddParam("@FB_AKTIF", model.IsAktif, SqlDbType.Bit);
    //         dp.AddParam("@FS_KD_INSTALASI", model.InstalasiId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_LAYANAN_DK", model.LayananDkId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_LAYANAN_TIPE_DK", model.LayananTipeDkId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_SMF", model.SmfId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_PEG", model.PetugasMedisId, SqlDbType.VarChar);
    //
    //         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         conn.Execute(sql, dp);
    //     }
    //
    //     public void Update(LayananModel model)
    //     {
    //         const string sql = @"
    //         UPDATE ta_layanan
    //         SET 
    //             FS_NM_LAYANAN =@FS_NM_LAYANAN,  
    //             FB_AKTIF =@FB_AKTIF,  
    //             FS_KD_INSTALASI =@FS_KD_INSTALASI,  
    //             FS_KD_LAYANAN_DK =@FS_KD_LAYANAN_DK,  
    //             FS_KD_LAYANAN_TIPE_DK =@FS_KD_LAYANAN_TIPE_DK,  
    //             FS_KD_SMF =@FS_KD_SMF,  
    //             FS_KD_MEDIS =@FS_KD_PEG
    //         WHERE     
    //             FS_KD_LAYANAN = @FS_KD_LAYANAN
    //         ";
    //         
    //         var dp = new DynamicParameters();
    //         dp.AddParam("@FS_KD_LAYANAN", model.LayananId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_NM_LAYANAN", model.LayananName, SqlDbType.VarChar);
    //         dp.AddParam("@FB_AKTIF", model.IsAktif, SqlDbType.Bit);
    //         dp.AddParam("@FS_KD_INSTALASI", model.InstalasiId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_LAYANAN_DK", model.LayananDkId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_LAYANAN_TIPE_DK", model.LayananTipeDkId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_SMF", model.SmfId, SqlDbType.VarChar);
    //         dp.AddParam("@FS_KD_PEG", model.PetugasMedisId, SqlDbType.VarChar);
    //
    //         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         conn.Execute(sql, dp);
    //     }
    //
    //     public void Delete(ILayananKey key)
    //     {
    //         const string sql = @"
    //             DELETE FROM ta_layanan
    //             WHERE FS_KD_LAYANAN = @FS_KD_LAYANAN ";
    //
    //         var dp = new DynamicParameters();
    //         dp.AddParam("@FS_KD_LAYANAN", key.LayananId, SqlDbType.VarChar);
    //         
    //         using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
    //         conn.Execute(sql, dp);
    //     }
    //
    
}

