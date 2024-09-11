using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.LayananSub.LayananAgg;

public class LayananDal : ILayananDal
{
    private readonly DatabaseOptions _opt;

    public LayananDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(LayananModel model)
    {
        const string sql = @"
            INSERT INTO ta_layanan (
                FS_KD_LAYANAN, 
                FS_NM_LAYANAN, 
                FB_AKTIF, 
                FS_KD_INSTALASI, 
                FS_KD_LAYANAN_DK, 
                FS_KD_LAYANAN_TIPE_DK, 
                FS_KD_SMF, 
                FS_KD_MEDIS
            ) 
            VALUES (
                @FS_KD_LAYANAN, 
                @FS_NM_LAYANAN, 
                @FB_AKTIF, 
                @FS_KD_INSTALASI, 
                @FS_KD_LAYANAN_DK, 
                @FS_KD_LAYANAN_TIPE_DK, 
                @FS_KD_SMF, 
                @FS_KD_PEG        
            );";

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LAYANAN", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@FS_NM_LAYANAN", model.LayananName, SqlDbType.VarChar);
        dp.AddParam("@FB_AKTIF", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@FS_KD_INSTALASI", model.InstalasiId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN_DK", model.LayananDkId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN_TIPE_DK", model.LayananTipeDkId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_SMF", model.SmfId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PEG", model.PetugasMedisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(LayananModel model)
    {
        const string sql = @"
        UPDATE ta_layanan
        SET 
            FS_NM_LAYANAN =@FS_NM_LAYANAN,  
            FB_AKTIF =@FB_AKTIF,  
            FS_KD_INSTALASI =@FS_KD_INSTALASI,  
            FS_KD_LAYANAN_DK =@FS_KD_LAYANAN_DK,  
            FS_KD_LAYANAN_TIPE_DK =@FS_KD_LAYANAN_TIPE_DK,  
            FS_KD_SMF =@FS_KD_SMF,  
            FS_KD_MEDIS =@FS_KD_PEG
        WHERE     
            FS_KD_LAYANAN = @FS_KD_LAYANAN
        ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LAYANAN", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@FS_NM_LAYANAN", model.LayananName, SqlDbType.VarChar);
        dp.AddParam("@FB_AKTIF", model.IsAktif, SqlDbType.Bit);
        dp.AddParam("@FS_KD_INSTALASI", model.InstalasiId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN_DK", model.LayananDkId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN_TIPE_DK", model.LayananTipeDkId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_SMF", model.SmfId, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PEG", model.PetugasMedisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ILayananKey key)
    {
        const string sql = @"
            DELETE FROM ta_layanan
            WHERE FS_KD_LAYANAN = @FS_KD_LAYANAN ";

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LAYANAN", key.LayananId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public LayananModel GetData(ILayananKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_layanan, aa.fs_nm_layanan, aa.fb_aktif,
                aa.fs_kd_instalasi, aa.fs_kd_layanan_dk,
                aa.fs_kd_layanan_tipe_dk, aa.fs_kd_smf,
                aa.fs_kd_peg,
                ISNULL(bb.fs_nm_instalasi,'') AS fs_nm_instalasi,
                ISNULL(cc.fs_nm_layanan_dk,'') AS fs_nm_layanan_dk,
                ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS fs_nm_layanan_tipe_dk,
                ISNULL(ee.fs_nm_smf,'') AS fs_nm_smf,
                ISNULL(ff.fs_nm_peg,'') AS fs_nm_peg 
            FROM 
                ta_layanan aa
                LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
                LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
                LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
                LEFT JOIN ta_smf ee ON aa.fs_kd_smf = ee.fs_kd_smf
                LEFT JOIN td_peg ff ON aa.fs_kd_medis = ff.fs_kd_peg
            WHERE aa.fs_kd_layanan = @fs_kd_layanan     
        ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_LAYANAN", key.LayananId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<LayananDto>(sql, dp);
        return result;
    }

    public IEnumerable<LayananModel> ListData()
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_layanan, aa.fs_nm_layanan, aa.fb_aktif,
                aa.fs_kd_instalasi, aa.fs_kd_layanan_dk,
                aa.fs_kd_layanan_tipe_dk, aa.fs_kd_smf,
                aa.fs_kd_medis,
                ISNULL(bb.fs_nm_instalasi,'') AS fs_nm_instalasi,
                ISNULL(cc.fs_nm_layanan_dk,'') AS fs_nm_layanan_dk,
                ISNULL(dd.fs_nm_layanan_tipe_dk,'') AS fs_nm_layanan_tipe_dk,
                ISNULL(ee.fs_nm_smf,'') AS fs_nm_smf,
                ISNULL(ff.fs_nm_peg,'') AS fs_nm_peg 
            FROM 
                ta_layanan aa
                LEFT JOIN ta_instalasi bb ON aa.fs_kd_instalasi = bb.fs_kd_instalasi
                LEFT JOIN ta_layanan_dk cc ON aa.fs_kd_layanan_dk = cc.fs_kd_layanan_dk
                LEFT JOIN ta_layanan_tipe_dk dd ON aa.fs_kd_layanan_tipe_dk = dd.fs_kd_layanan_tipe_dk
                LEFT JOIN ta_smf ee ON aa.fs_kd_smf = ee.fs_kd_smf
                LEFT JOIN td_peg ff ON aa.fs_kd_medis = ff.fs_kd_peg ";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<LayananDto>(sql);
        return result;
    }
}

public class LayananDto : LayananModel
{
        public string fs_kd_layanan{ get => LayananId; set => LayananId = value; }
        public string fs_nm_layanan{ get => LayananName; set => LayananName = value; }
        public bool fb_aktif{get => IsAktif; set => IsAktif  = value; }
        public string fs_kd_instalasi{ get => InstalasiId ; set => InstalasiId = value; }
        public string fs_nm_instalasi{ get => InstalasiName; set => InstalasiName = value; }
        public string fs_kd_layanan_dk{ get => LayananDkId ; set => LayananDkId = value; }
        public string fs_nm_layanan_dk{ get => LayananDkName; set => LayananDkName = value; }
        public string fs_kd_layanan_tipe_dk{ get => LayananTipeDkId; set => LayananTipeDkId = value; }
        public string fs_nm_layanan_tipe_dk{ get => LayananTipeDkName; set => LayananTipeDkName = value; }
        public string fs_kd_smf{ get => SmfId; set => SmfId = value; }
        public string fs_nm_smf{ get => SmfName; set => SmfName = value; }
        public string fs_kd_peg{ get => PetugasMedisId; set => PetugasMedisId = value; }
        public string fs_nm_peg{ get => PetugasMedisName; set => PetugasMedisName = value; }
        public LayananDto() : base(string.Empty, string.Empty)
    {
    }
}