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
                aa.FS_KD_LAYANAN,
                aa.FS_NM_LAYANAN,
                aa.FB_AKTIF,
                bb.FS_KD_INSTALASI,
                ISNULL(bb.FS_NM_INSTALASI,''),
                cc.FS_KD_LAYANAN_DK,
                ISNULL(cc.FS_NM_LAYANAN_DK,''),
                dd.FS_KD_LAYANAN_TIPE_DK,
                ISNULL(dd.FS_NM_LAYANAN_TIPE_DK,''),
                ee.FS_KD_SMF,
                ISNULL(ee.FS_NM_SMF,''),
                ff.FS_KD_PEG,
                ISNULL(ff.FS_NM_PEG,'') 
            FROM 
                ta_layanan aa
            LEFT JOIN 
                ta_instalasi bb ON aa.FS_KD_INSTALASI = bb.FS_KD_INSTALASI
            LEFT JOIN 
                ta_layanan_dk cc ON aa.FS_KD_LAYANAN_DK = cc.FS_KD_LAYANAN_DK
            LEFT JOIN 
                ta_layanan_tipe_dk dd ON aa.FS_KD_LAYANAN_TIPE_DK = dd.FS_KD_LAYANAN_TIPE_DK
            LEFT JOIN 
                ta_smf ee ON aa.FS_KD_SMF = ee.FS_KD_SMF
            LEFT JOIN 
                td_peg ff ON aa.FS_KD_MEDIS = ff.FS_KD_PEG
            WHERE aa.FS_KD_LAYANAN = @FS_KD_LAYANAN     
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
            aa.FS_KD_LAYANAN,
            aa.FS_NM_LAYANAN,
            aa.FB_AKTIF,
            bb.FS_KD_INSTALASI,
            ISNULL(bb.FS_NM_INSTALASI, '') AS FS_NM_INSTALASI,
            cc.FS_KD_LAYANAN_DK,
            ISNULL(cc.FS_NM_LAYANAN_DK, '') AS FS_NM_LAYANAN_DK,
            dd.FS_KD_LAYANAN_TIPE_DK,
            ISNULL(dd.FS_NM_LAYANAN_TIPE_DK, '') AS FS_NM_LAYANAN_TIPE_DK,
            ee.FS_KD_SMF,
            ISNULL(ee.FS_NM_SMF, '') AS FS_NM_SMF,
            ff.FS_KD_PEG,
            ISNULL(ff.FS_NM_PEG, '') AS FS_NM_PEG
        FROM 
            ta_layanan aa
        LEFT JOIN 
            ta_instalasi bb ON aa.FS_KD_INSTALASI = bb.FS_KD_INSTALASI
        LEFT JOIN 
            ta_layanan_dk cc ON aa.FS_KD_LAYANAN_DK = cc.FS_KD_LAYANAN_DK
        LEFT JOIN 
            ta_layanan_tipe_dk dd ON aa.FS_KD_LAYANAN_TIPE_DK = dd.FS_KD_LAYANAN_TIPE_DK
        LEFT JOIN 
            ta_smf ee ON aa.FS_KD_SMF = ee.FS_KD_SMF
        LEFT JOIN 
            td_peg ff ON aa.FS_KD_MEDIS = ff.FS_KD_PEG
    ";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<LayananDto>(sql);
        return result;
    }
}

public class LayananDto : LayananModel
{
        public string FS_KD_LAYANAN{ get => LayananId; set => LayananId = value; }
        public string FS_NM_LAYANAN{ get => LayananName; set => LayananName = value; }
        public bool FB_AKTIF{get => IsAktif; set => IsAktif  = value; }
        public string FS_KD_INSTALASI{ get => InstalasiId ; set => InstalasiId = value; }
        public string FS_NM_INSTALASI{ get => InstalasiName; set => InstalasiName = value; }
        public string FS_KD_LAYANAN_DK{ get => LayananDkId ; set => LayananDkId = value; }
        public string FS_NM_LAYANAN_DK{ get => LayananDkName; set => LayananDkName = value; }
        public string FS_KD_LAYANAN_TIPE_DK{ get => LayananTipeDkId; set => LayananTipeDkId = value; }
        public string FS_NM_LAYANAN_TIPE_DK{ get => LayananTipeDkName; set => LayananTipeDkName = value; }
        public string FS_KD_SMF{ get => SmfId; set => SmfId = value; }
        public string FS_NM_SMF{ get => SmfName; set => SmfName = value; }
        public string FS_KD_PEG{ get => PetugasMedisId; set => PetugasMedisId = value; }
        public string FS_NM_PEG{ get => PetugasMedisName; set => PetugasMedisName = value; }
        public LayananDto() : base(string.Empty, string.Empty)
    {
    }
}