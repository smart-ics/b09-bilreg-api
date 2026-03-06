using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasienDal :
    IInsert<PasienDto>,
    IUpdate<PasienDto>,
    IDelete<IPasienKey>,
    IGetData<PasienDto, IPasienKey>,
    IListData<PasienDto, DateTime>
{
    IEnumerable<PasienDto> ListDataByName(Dictionary<string, string[]> listName);
}

public class PasienDal : IPasienDal
{
    private readonly DatabaseOptions _opt;

    public PasienDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienDto model)
    {
        const string sql = """
            INSERT INTO tc_mr(
                fs_mr, fs_nm_pasien, fd_tgl_lahir, fs_jns_kelamin,
                fs_nm_alias, fs_temp_lahir, fs_nm_ibu_kandung, fs_gol_darah,
                fs_alm_pasien, fs_alm2_pasien, fs_alm3_pasien, fs_kota_pasien, 
                fs_kd_pos_pasien, fs_kd_kelurahan, 
                --
                fs_jenis_id, fs_kd_identitas, fs_no_kk, 
                fs_email, fs_tlp_pasien, fs_no_hp,
                --
                fs_nm_keluarga, fs_hub_keluarga, fs_telp_keluarga, 
                fs_alm1_keluarga, fs_alm2_keluarga, 
                fs_kota_keluarga, fs_kd_pos_keluarga,
                --
                fs_kd_agama, fs_kd_suku, fs_kd_status_kawin_dk, 
                fs_kd_pendidikan_dk, fs_kd_pekerjaan_dk,
                --
                fd_tgl_mr, fb_aktif) 
            VALUES (
                @fs_mr, @fs_nm_pasien, @fd_tgl_lahir, @fs_jns_kelamin,
                @fs_nm_alias, @fs_temp_lahir, @fs_nm_ibu_kandung, @fs_gol_darah,
                @fs_alm_pasien, @fs_alm2_pasien, @fs_alm3_pasien, @fs_kota_pasien, 
                @fs_kd_pos_pasien, @fs_kd_kelurahan, 
                --
                @fs_jenis_id, @fs_kd_identitas, @fs_no_kk, 
                @fs_email, @fs_tlp_pasien, @fs_no_hp,
                --
                @fs_nm_keluarga, @fs_hub_keluarga, @fs_telp_keluarga, 
                @fs_alm1_keluarga, @fs_alm2_keluarga, 
                @fs_kota_keluarga, @fs_kd_pos_keluarga,
                --
                @fs_kd_agama, @fs_kd_suku, @fs_kd_status_kawin_dk, 
                @fs_kd_pendidikan_dk, @fs_kd_pekerjaan_dk,
                --
                @fd_tgl_mr, @fb_aktif)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.fd_tgl_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.fs_jns_kelamin, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_alias", model.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.fs_temp_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.fs_nm_ibu_kandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.fs_gol_darah, SqlDbType.VarChar);
        
        dp.AddParam("@fs_alm_pasien", model.fs_alm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.fs_alm2_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.fs_alm3_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.fs_kota_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.fs_kd_pos_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.fs_kd_kelurahan, SqlDbType.VarChar);
        
        dp.AddParam("@fs_jenis_id", model.fs_jenis_id, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.fs_kd_identitas, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.fs_no_kk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.fs_email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.fs_tlp_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.fs_no_hp, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_keluarga", model.fs_nm_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.fs_hub_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.fs_telp_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.fs_alm1_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.fs_alm2_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.fs_kota_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.fs_kd_pos_keluarga, SqlDbType.VarChar);
        
        dp.AddParam("@fs_kd_agama", model.fs_kd_agama, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.fs_kd_suku, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_status_kawin_dk", model.fs_kd_status_kawin_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.fs_kd_pendidikan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.fs_kd_pekerjaan_dk, SqlDbType.VarChar);
        
        dp.AddParam("@fd_tgl_mr", model.fd_tgl_mr, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienDto model)
    {
        const string sql = """
            UPDATE tc_mr
            SET 
                fs_nm_pasien = @fs_nm_pasien,
                fd_tgl_lahir = @fd_tgl_lahir,
                fs_jns_kelamin = @fs_jns_kelamin,
                fs_nm_alias = @fs_nm_alias,
                fs_temp_lahir = @fs_temp_lahir,
                fs_nm_ibu_kandung = @fs_nm_ibu_kandung,
                fs_gol_darah = @fs_gol_darah,
                fs_alm_pasien = @fs_alm_pasien,
                fs_alm2_pasien = @fs_alm2_pasien,
                fs_alm3_pasien = @fs_alm3_pasien,
                fs_kota_pasien = @fs_kota_pasien,
                fs_kd_pos_pasien = @fs_kd_pos_pasien,
                fs_kd_kelurahan = @fs_kd_kelurahan,
                --
                fs_jenis_id = @fs_jenis_id,
                fs_kd_identitas = @fs_kd_identitas,
                fs_no_kk = @fs_no_kk,
                fs_email = @fs_email,
                fs_tlp_pasien = @fs_tlp_pasien,
                fs_no_hp = @fs_no_hp,
                --
                fs_nm_keluarga = @fs_nm_keluarga,
                fs_hub_keluarga = @fs_hub_keluarga,
                fs_telp_keluarga = @fs_telp_keluarga,
                fs_alm1_keluarga = @fs_alm1_keluarga,
                fs_alm2_keluarga = @fs_alm2_keluarga,
                fs_kota_keluarga = @fs_kota_keluarga,
                fs_kd_pos_keluarga = @fs_kd_pos_keluarga,
                --
                fs_kd_agama = @fs_kd_agama,
                fs_kd_suku = @fs_kd_suku,
                fs_kd_status_kawin_dk = @fs_kd_status_kawin_dk,
                fs_kd_pendidikan_dk = @fs_kd_pendidikan_dk,
                fs_kd_pekerjaan_dk = @fs_kd_pekerjaan_dk,
                --
                fd_tgl_mr = @fd_tgl_mr,
                fb_aktif = @fb_aktif
            WHERE 
                fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", model.fs_mr, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.fs_nm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_lahir", model.fd_tgl_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_jns_kelamin", model.fs_jns_kelamin, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_alias", model.fs_nm_alias, SqlDbType.VarChar);
        dp.AddParam("@fs_temp_lahir", model.fs_temp_lahir, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_ibu_kandung", model.fs_nm_ibu_kandung, SqlDbType.VarChar);
        dp.AddParam("@fs_gol_darah", model.fs_gol_darah, SqlDbType.VarChar);
        
        dp.AddParam("@fs_alm_pasien", model.fs_alm_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.fs_alm2_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm3_pasien", model.fs_alm3_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.fs_kota_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_pasien", model.fs_kd_pos_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelurahan", model.fs_kd_kelurahan, SqlDbType.VarChar);
        
        dp.AddParam("@fs_jenis_id", model.fs_jenis_id, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_identitas", model.fs_kd_identitas, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kk", model.fs_no_kk, SqlDbType.VarChar);
        dp.AddParam("@fs_email", model.fs_email, SqlDbType.VarChar);
        dp.AddParam("@fs_tlp_pasien", model.fs_tlp_pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_no_hp", model.fs_no_hp, SqlDbType.VarChar);
        
        dp.AddParam("@fs_nm_keluarga", model.fs_nm_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_hub_keluarga", model.fs_hub_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_telp_keluarga", model.fs_telp_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_keluarga", model.fs_alm1_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_keluarga", model.fs_alm2_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_keluarga", model.fs_kota_keluarga, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pos_keluarga", model.fs_kd_pos_keluarga, SqlDbType.VarChar);
        
        dp.AddParam("@fs_kd_agama", model.fs_kd_agama, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_suku", model.fs_kd_suku, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_status_kawin_dk", model.fs_kd_status_kawin_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pendidikan_dk", model.fs_kd_pendidikan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pekerjaan_dk", model.fs_kd_pekerjaan_dk, SqlDbType.VarChar);
        
        dp.AddParam("@fd_tgl_mr", model.fd_tgl_mr, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", model.fb_aktif, SqlDbType.Bit);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM tc_mr
            WHERE fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienDto GetData(IPasienKey key)
    {
        const string sql = """
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fd_tgl_lahir, aa.fs_jns_kelamin,
                aa.fs_nm_alias, aa.fs_temp_lahir, aa.fs_nm_ibu_kandung, aa.fs_gol_darah,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, aa.fs_kota_pasien, 
                aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                --
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
                --
                aa.fs_nm_keluarga, aa.fs_hub_keluarga, aa.fs_telp_keluarga, 
                aa.fs_alm1_keluarga, aa.fs_alm2_keluarga, 
                aa.fs_kota_keluarga, aa.fs_kd_pos_keluarga,
                --
                aa.fs_kd_agama, aa.fs_kd_suku, aa.fs_kd_status_kawin_dk, 
                aa.fs_kd_pendidikan_dk, aa.fs_kd_pekerjaan_dk,
                --
                aa.fd_tgl_mr, aa.fb_aktif,
                --
                ISNULL(bb.fs_nm_kelurahan, '-') AS fs_nm_kelurahan,
                ISNULL(bb.fs_kd_kecamatan, '-') AS fs_kd_kecamatan,
                ISNULL(cc.fs_nm_kecamatan, '-') AS fs_nm_kecamatan,
                ISNULL(cc.fs_kd_kabupaten, '-') AS fs_kd_kabupaten,
                ISNULL(dd.fs_nm_kabupaten, '-') AS fs_nm_kabupaten,
                ISNULL(dd.fs_kd_propinsi, '-') AS fs_kd_propinsi,
                ISNULL(ee.fs_nm_propinsi, '-') AS fs_nm_propinsi,
                ISNULL(ff.fs_nm_agama, '-') AS fs_nm_agama,
                ISNULL(gg.fs_nm_suku, '-') AS fs_nm_suku,
                ISNULL(hh.fs_nm_status_kawin_dk, '-') AS fs_nm_status_kawin_dk,
                ISNULL(ii.fs_nm_pendidikan_dk, '-') AS fs_nm_pendidikan_dk,
                ISNULL(jj.fs_nm_pekerjaan_dk, '-') AS fs_nm_pekerjaan_dk
            FROM 
                tc_mr aa
                LEFT JOIN ta_kelurahan bb ON aa.fs_kd_kelurahan = bb.fs_kd_kelurahan
                LEFT JOIN ta_kecamatan cc ON bb.fs_kd_kecamatan = cc.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten dd ON cc.fs_kd_kabupaten = dd.fs_kd_kabupaten
                LEFT JOIN ta_propinsi ee ON dd.fs_kd_propinsi = ee.fs_kd_propinsi
                LEFT JOIN ta_agama ff ON aa.fs_kd_agama = ff.fs_kd_agama
                LEFT JOIN ta_suku gg ON aa.fs_kd_suku = gg.fs_kd_suku
                LEFT JOIN ta_status_kawin_dk hh ON aa.fs_kd_status_kawin_dk = hh.fs_kd_status_kawin_dk
                LEFT JOIN ta_pendidikan_dk ii ON aa.fs_kd_pendidikan_dk = ii.fs_kd_pendidikan_dk
                LEFT JOIN ta_pekerjaan_dk jj ON aa.fs_kd_pekerjaan_dk = jj.fs_kd_pekerjaan_dk
            WHERE 
                fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_mr", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienDto>(sql, dp);
    }

    public IEnumerable<PasienDto> ListData(DateTime tglLahir)
    {
        const string sql = """
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fd_tgl_lahir, aa.fs_jns_kelamin,
                aa.fs_nm_alias, aa.fs_temp_lahir, aa.fs_nm_ibu_kandung, aa.fs_gol_darah,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, aa.fs_kota_pasien, 
                aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                --
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
                --
                aa.fs_nm_keluarga, aa.fs_hub_keluarga, aa.fs_telp_keluarga, 
                aa.fs_alm1_keluarga, aa.fs_alm2_keluarga, 
                aa.fs_kota_keluarga, aa.fs_kd_pos_keluarga,
                --
                aa.fs_kd_agama, aa.fs_kd_suku, aa.fs_kd_status_kawin_dk, 
                aa.fs_kd_pendidikan_dk, aa.fs_kd_pekerjaan_dk,
                --
                aa.fd_tgl_mr, aa.fb_aktif,
                --
                ISNULL(bb.fs_nm_kelurahan, '-') AS fs_nm_kelurahan,
                ISNULL(bb.fs_kd_kecamatan, '-') AS fs_kd_kecamatan,
                ISNULL(cc.fs_nm_kecamatan, '-') AS fs_nm_kecamatan,
                ISNULL(cc.fs_kd_kabupaten, '-') AS fs_kd_kabupaten,
                ISNULL(dd.fs_nm_kabupaten, '-') AS fs_nm_kabupaten,
                ISNULL(dd.fs_kd_propinsi, '-') AS fs_kd_propinsi,
                ISNULL(ee.fs_nm_propinsi, '-') AS fs_nm_propinsi,
                ISNULL(ff.fs_nm_agama, '-') AS fs_nm_agama,
                ISNULL(gg.fs_nm_suku, '-') AS fs_nm_suku,
                ISNULL(hh.fs_nm_status_kawin_dk, '-') AS fs_nm_status_kawin_dk,
                ISNULL(ii.fs_nm_pendidikan_dk, '-') AS fs_nm_pendidikan_dk,
                ISNULL(jj.fs_nm_pekerjaan_dk, '-') AS fs_nm_pekerjaan_dk
            FROM 
                tc_mr aa
                LEFT JOIN ta_kelurahan bb ON aa.fs_kd_kelurahan = bb.fs_kd_kelurahan
                LEFT JOIN ta_kecamatan cc ON bb.fs_kd_kecamatan = cc.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten dd ON cc.fs_kd_kabupaten = dd.fs_kd_kabupaten
                LEFT JOIN ta_propinsi ee ON dd.fs_kd_propinsi = ee.fs_kd_propinsi
                LEFT JOIN ta_agama ff ON aa.fs_kd_agama = ff.fs_kd_agama
                LEFT JOIN ta_suku gg ON aa.fs_kd_suku = gg.fs_kd_suku
                LEFT JOIN ta_status_kawin_dk hh ON aa.fs_kd_status_kawin_dk = hh.fs_kd_status_kawin_dk
                LEFT JOIN ta_pendidikan_dk ii ON aa.fs_kd_pendidikan_dk = ii.fs_kd_pendidikan_dk
                LEFT JOIN ta_pekerjaan_dk jj ON aa.fs_kd_pekerjaan_dk = jj.fs_kd_pekerjaan_dk
            WHERE 
                aa.fd_tgl_lahir = @TglLahir
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TglLahir", tglLahir.ToString("yyyy-MM-dd"), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienDto>(sql, dp);
    }
    
    private static string EscapeForContains(string term)
    {
        return "\"" + term.Replace("\"", "\"\"") + "*\"";
    }
    public IEnumerable<PasienDto> ListDataByName(Dictionary<string, string[]> listName)
    {
        var containers = listName
            .Select(item =>
            {
                var listVariant = item.Value.Select(EscapeForContains);
                return $"CONTAINS(fs_nm_pasien, '{string.Join(" OR ", listVariant)}')";
            });

        var whereClause = string.Join(" AND ", containers);
        
        var sql = $"""
            SELECT 
                aa.fs_mr, aa.fs_nm_pasien, aa.fd_tgl_lahir, aa.fs_jns_kelamin,
                aa.fs_nm_alias, aa.fs_temp_lahir, aa.fs_nm_ibu_kandung, aa.fs_gol_darah,
                aa.fs_alm_pasien, aa.fs_alm2_pasien, aa.fs_alm3_pasien, aa.fs_kota_pasien, 
                aa.fs_kd_pos_pasien, aa.fs_kd_kelurahan, 
                --
                aa.fs_jenis_id, aa.fs_kd_identitas, aa.fs_no_kk, 
                aa.fs_email, aa.fs_tlp_pasien, aa.fs_no_hp,
                --
                aa.fs_nm_keluarga, aa.fs_hub_keluarga, aa.fs_telp_keluarga, 
                aa.fs_alm1_keluarga, aa.fs_alm2_keluarga, 
                aa.fs_kota_keluarga, aa.fs_kd_pos_keluarga,
                --
                aa.fs_kd_agama, aa.fs_kd_suku, aa.fs_kd_status_kawin_dk, 
                aa.fs_kd_pendidikan_dk, aa.fs_kd_pekerjaan_dk,
                --
                aa.fd_tgl_mr, aa.fb_aktif,
                --
                ISNULL(bb.fs_nm_kelurahan, '-') AS fs_nm_kelurahan,
                ISNULL(bb.fs_kd_kecamatan, '-') AS fs_kd_kecamatan,
                ISNULL(cc.fs_nm_kecamatan, '-') AS fs_nm_kecamatan,
                ISNULL(cc.fs_kd_kabupaten, '-') AS fs_kd_kabupaten,
                ISNULL(dd.fs_nm_kabupaten, '-') AS fs_nm_kabupaten,
                ISNULL(dd.fs_kd_propinsi, '-') AS fs_kd_propinsi,
                ISNULL(ee.fs_nm_propinsi, '-') AS fs_nm_propinsi,
                ISNULL(ff.fs_nm_agama, '-') AS fs_nm_agama,
                ISNULL(gg.fs_nm_suku, '-') AS fs_nm_suku,
                ISNULL(hh.fs_nm_status_kawin_dk, '-') AS fs_nm_status_kawin_dk,
                ISNULL(ii.fs_nm_pendidikan_dk, '-') AS fs_nm_pendidikan_dk,
                ISNULL(jj.fs_nm_pekerjaan_dk, '-') AS fs_nm_pekerjaan_dk
            FROM 
                tc_mr aa
                LEFT JOIN ta_kelurahan bb ON aa.fs_kd_kelurahan = bb.fs_kd_kelurahan
                LEFT JOIN ta_kecamatan cc ON bb.fs_kd_kecamatan = cc.fs_kd_kecamatan
                LEFT JOIN ta_kabupaten dd ON cc.fs_kd_kabupaten = dd.fs_kd_kabupaten
                LEFT JOIN ta_propinsi ee ON dd.fs_kd_propinsi = ee.fs_kd_propinsi
                LEFT JOIN ta_agama ff ON aa.fs_kd_agama = ff.fs_kd_agama
                LEFT JOIN ta_suku gg ON aa.fs_kd_suku = gg.fs_kd_suku
                LEFT JOIN ta_status_kawin_dk hh ON aa.fs_kd_status_kawin_dk = hh.fs_kd_status_kawin_dk
                LEFT JOIN ta_pendidikan_dk ii ON aa.fs_kd_pendidikan_dk = ii.fs_kd_pendidikan_dk
                LEFT JOIN ta_pekerjaan_dk jj ON aa.fs_kd_pekerjaan_dk = jj.fs_kd_pekerjaan_dk
            WHERE {whereClause}
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var results = conn.Read<PasienDto>(sql) ?? [];
        return results;
    }
}

