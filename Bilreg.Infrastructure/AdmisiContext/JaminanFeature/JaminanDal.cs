using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public interface IJaminanDal :
    IInsert<JaminanDto>,
    IUpdate<JaminanDto>,
    IDelete<IJaminanKey>,
    IGetData<JaminanDto, IJaminanKey>,
    IListData<JaminanDto>
{
}

public class JaminanDal : IJaminanDal
{
    private readonly DatabaseOptions _opt;

    public JaminanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JaminanDto dto)
    {
        const string sql = """
           INSERT INTO ta_jaminan(
                fs_kd_jaminan, fs_nm_jaminan, fb_aktif, 
                fs_alm1_jaminan, fs_alm2_jaminan, fs_kota_jaminan, 
                fs_kd_cara_bayar_dk, fs_kd_grup_jaminan, 
                fs_kd_tipe_tarif_rawat_jalan, fs_kd_tipe_tarif_rawat_inap, 
                fs_piut_rawat, fs_piut_obat_rawat, fs_kd_rek_ppdp_jasa_ri, 
                fs_kd_rek_ppdp_obat_ri)
           VALUES( 
                @fs_kd_jaminan, @fs_nm_jaminan, @fb_aktif, 
                @fs_alm1_jaminan, @fs_alm2_jaminan, @fs_kota_jaminan, 
                @fs_kd_cara_bayar_dk, @fs_kd_grup_jaminan, 
                @fs_kd_tipe_tarif_rawat_jalan, @fs_kd_tipe_tarif_rawat_inap, 
                @fs_piut_rawat, @fs_piut_obat_rawat, @fs_kd_rek_ppdp_jasa_ri, 
                @fs_kd_rek_ppdp_obat_ri)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", dto.fs_kd_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", dto.fs_nm_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        
        dp.AddParam("@fs_alm1_jaminan", dto.fs_alm1_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", dto.fs_alm2_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", dto.fs_kota_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_bayar_dk", dto.fs_kd_cara_bayar_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", dto.fs_kd_grup_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_tarif_rawat_jalan", dto.fs_kd_tipe_tarif_rawat_jalan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_tarif_rawat_inap", dto.fs_kd_tipe_tarif_rawat_inap, SqlDbType.VarChar);
        
        dp.AddParam("@fs_piut_rawat", dto.fs_piut_rawat, SqlDbType.VarChar);
        dp.AddParam("@fs_piut_obat_rawat", dto.fs_piut_obat_rawat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_ppdp_jasa_ri", dto.fs_kd_rek_ppdp_jasa_ri, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_ppdp_obat_ri", dto.fs_kd_rek_ppdp_obat_ri, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(JaminanDto dto)
    {
        const string sql = """
           UPDATE 
               ta_jaminan
           SET
               fs_nm_jaminan = @fs_nm_jaminan,
               fb_aktif = @fb_aktif,
               fs_alm1_jaminan = @fs_alm1_jaminan,
               fs_alm2_jaminan = @fs_alm2_jaminan,
               fs_kota_jaminan = @fs_kota_jaminan,
               fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk,
               fs_kd_grup_jaminan = @fs_kd_grup_jaminan,
               fs_kd_tipe_tarif_rawat_jalan = @fs_kd_tipe_tarif_rawat_jalan,
               fs_kd_tipe_tarif_rawat_inap = @fs_kd_tipe_tarif_rawat_inap,
               fs_piut_rawat = @fs_piut_rawat,
               fs_piut_obat_rawat = @fs_piut_obat_rawat,
               fs_kd_rek_ppdp_jasa_ri = @fs_kd_rek_ppdp_jasa_ri,
               fs_kd_rek_ppdp_obat_ri = @fs_kd_rek_ppdp_obat_ri
           WHERE
               fs_kd_jaminan = @fs_kd_jaminan
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", dto.fs_kd_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_jaminan", dto.fs_nm_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);

        dp.AddParam("@fs_alm1_jaminan", dto.fs_alm1_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_jaminan", dto.fs_alm2_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_jaminan", dto.fs_kota_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_cara_bayar_dk", dto.fs_kd_cara_bayar_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_grup_jaminan", dto.fs_kd_grup_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_tarif_rawat_jalan", dto.fs_kd_tipe_tarif_rawat_jalan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_tarif_rawat_inap", dto.fs_kd_tipe_tarif_rawat_inap, SqlDbType.VarChar);
        
        dp.AddParam("@fs_piut_rawat", dto.fs_piut_rawat, SqlDbType.VarChar);
        dp.AddParam("@fs_piut_obat_rawat", dto.fs_piut_obat_rawat, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_ppdp_jasa_ri", dto.fs_kd_rek_ppdp_jasa_ri, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_rek_ppdp_obat_ri", dto.fs_kd_rek_ppdp_obat_ri, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IJaminanKey key)
    {
        const string sql = """
           DELETE FROM 
                ta_jaminan
           WHERE
               fs_kd_jaminan = @fs_kd_jaminan
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public JaminanDto GetData(IJaminanKey key)
    {
        const string sql = """
           SELECT
               aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fb_aktif,  
               aa.fs_alm1_jaminan, aa.fs_alm2_jaminan, aa.fs_kota_jaminan, '-' as fs_kd_pos,
               aa.fs_kd_cara_bayar_dk, aa.fs_kd_grup_jaminan, 
               aa.fs_kd_tipe_tarif_rawat_jalan, aa.fs_kd_tipe_tarif_rawat_inap,
           	   aa.fs_kd_tipe_brg_rawat_jalan, aa.fs_kd_tipe_brg_rawat_inap,
               aa.fs_piut_rawat, aa.fs_piut_obat_rawat, 
               aa.fs_kd_rek_ppdp_jasa_ri, aa.fs_kd_rek_ppdp_obat_ri,
               ISNULL(bb.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk,
               ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
               ISNULL(dd.fs_nm_tarif_tipe,'-') AS fs_nm_tarif_tipe_rawat_jalan,
               ISNULL(ee.fs_nm_tarif_tipe,'-') AS fs_nm_tarif_tipe_rawat_inap,
               ISNULL(ff1.fs_nm_rek, '-') AS fs_nm_piut_rawat,
               ISNULL(ff2.fs_nm_rek, '-') AS fs_nm_piut_obat_rawat, 
               ISNULL(ff3.fs_nm_rek, '-') AS fs_nm_rek_ppdp_jasa_ri, 
               ISNULL(ff4.fs_nm_rek, '-') AS fs_nm_rek_ppdp_obat_ri,
           	   ISNULL(gg1.fs_nm_tipe_barang,'-') AS fs_nm_tipe_brg_rawat_jalan,
           	   ISNULL(gg2.fs_nm_tipe_barang,'-') AS fs_nm_tipe_brg_rawat_inap
           FROM 
               ta_jaminan aa
               LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
               LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
               LEFT JOIN ta_tarif_tipe dd ON aa.fs_kd_tipe_tarif_rawat_jalan = dd.fs_kd_tarif_tipe
               LEFT JOIN ta_tarif_tipe ee ON aa.fs_kd_tipe_tarif_rawat_inap = ee.fs_kd_tarif_tipe
               LEFT JOIN t_rek ff1 ON aa.fs_piut_rawat = ff1.fs_kd_rek
               LEFT JOIN t_rek ff2 ON aa.fs_piut_obat_rawat = ff2.fs_kd_rek
               LEFT JOIN t_rek ff3 ON aa.fs_kd_rek_ppdp_jasa_ri = ff3.fs_kd_rek
               LEFT JOIN t_rek ff4 ON aa.fs_kd_rek_ppdp_obat_ri = ff4.fs_kd_rek
           	   LEFT JOIN tb_tipe_barang gg1 ON aa.fs_kd_tipe_brg_rawat_jalan = gg1.fs_kd_tipe_barang 
           	   LEFT JOIN tb_tipe_barang gg2 ON aa.fs_kd_tipe_brg_rawat_inap = gg2.fs_kd_tipe_barang 
           WHERE 
               aa.fs_kd_jaminan = @fs_kd_jaminan
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_jaminan", key.JaminanId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<JaminanDto>(sql, dp);
        return result;
    }

    public IEnumerable<JaminanDto> ListData()
    {
        const string sql = """
            SELECT
                aa.fs_kd_jaminan, aa.fs_nm_jaminan, aa.fb_aktif,  
                aa.fs_alm1_jaminan, aa.fs_alm2_jaminan, aa.fs_kota_jaminan, '-' as fs_kd_pos,
                aa.fs_kd_cara_bayar_dk, aa.fs_kd_grup_jaminan, 
                aa.fs_kd_tipe_tarif_rawat_jalan, aa.fs_kd_tipe_tarif_rawat_inap,
            	aa.fs_kd_tipe_brg_rawat_jalan, aa.fs_kd_tipe_brg_rawat_inap,
                aa.fs_piut_rawat, aa.fs_piut_obat_rawat, 
                aa.fs_kd_rek_ppdp_jasa_ri, aa.fs_kd_rek_ppdp_obat_ri,
                ISNULL(bb.fs_nm_cara_bayar_dk, '-') fs_nm_cara_bayar_dk,
                ISNULL(cc.fs_nm_grup_jaminan, '-') fs_nm_grup_jaminan,
                ISNULL(dd.fs_nm_tarif_tipe,'-') AS fs_nm_tarif_tipe_rawat_jalan,
                ISNULL(ee.fs_nm_tarif_tipe,'-') AS fs_nm_tarif_tipe_rawat_inap,
                ISNULL(ff1.fs_nm_rek, '-') AS fs_nm_piut_rawat,
                ISNULL(ff2.fs_nm_rek, '-') AS fs_nm_piut_obat_rawat, 
                ISNULL(ff3.fs_nm_rek, '-') AS fs_nm_rek_ppdp_jasa_ri, 
                ISNULL(ff4.fs_nm_rek, '-') AS fs_nm_rek_ppdp_obat_ri,
            	ISNULL(gg1.fs_nm_tipe_barang,'-') AS fs_nm_tipe_brg_rawat_jalan,
            	ISNULL(gg2.fs_nm_tipe_barang,'-') AS fs_nm_tipe_brg_rawat_inap
            FROM 
                ta_jaminan aa
                LEFT JOIN ta_cara_bayar_dk bb ON aa.fs_kd_cara_bayar_dk = bb.fs_kd_cara_bayar_dk
                LEFT JOIN ta_grup_jaminan cc ON aa.fs_kd_grup_jaminan = cc.fs_kd_grup_jaminan
                LEFT JOIN ta_tarif_tipe dd ON aa.fs_kd_tipe_tarif_rawat_jalan = dd.fs_kd_tarif_tipe
                LEFT JOIN ta_tarif_tipe ee ON aa.fs_kd_tipe_tarif_rawat_inap = ee.fs_kd_tarif_tipe
                LEFT JOIN t_rek ff1 ON aa.fs_piut_rawat = ff1.fs_kd_rek
                LEFT JOIN t_rek ff2 ON aa.fs_piut_obat_rawat = ff2.fs_kd_rek
                LEFT JOIN t_rek ff3 ON aa.fs_kd_rek_ppdp_jasa_ri = ff3.fs_kd_rek
                LEFT JOIN t_rek ff4 ON aa.fs_kd_rek_ppdp_obat_ri = ff4.fs_kd_rek
            	LEFT JOIN tb_tipe_barang gg1 ON aa.fs_kd_tipe_brg_rawat_jalan = gg1.fs_kd_tipe_barang 
            	LEFT JOIN tb_tipe_barang gg2 ON aa.fs_kd_tipe_brg_rawat_inap = gg2.fs_kd_tipe_barang 
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JaminanDto>(sql);
    }
}