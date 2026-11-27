using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature;

public interface IPolisDal :
    IInsert<PolisDto>,
    IUpdate<PolisDto>,
    IDelete<IPolisKey>,
    IGetData<PolisDto, IPolisKey>,
    IListData<PolisViewDto, IPasienKey>
{
    PolisDto GetDataByNoPeserta(string noPeserta);
}
public class PolisDal : IPolisDal
{
    private readonly DatabaseOptions _opt;

    public PolisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PolisDto dto)
    {
        const string sql = """
            INSERT INTO ta_polis(
                fs_kd_polis, fs_no_polis, fs_atas_nama, fd_expired, 
                fs_kd_tipe_jaminan, fb_cover_rj, fs_kd_kelas_ri,
                fs_kd_bu, fs_departemen, fs_no_payroll,
                crttgl, crtjam, crtipa, crtver, crtusr,
                updtgl, updjam, updipa, updver, updusr
                )
            VALUES(
                @fs_kd_polis, @fs_no_polis, @fs_atas_nama, @fd_expired, 
                @fs_kd_tipe_jaminan, @fb_cover_rj, @fs_kd_kelas_ri,
                @fs_kd_bu, @fs_departemen, @fs_no_payroll,
                @crttgl, @crtjam, @crtipa, @crtver, @crtusr,
                @updtgl, @updjam, @updipa, @updver, @updusr
                )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", dto.fs_kd_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", dto.fs_no_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", dto.fs_atas_nama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", dto.fd_expired, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas_ri", dto.fs_kd_kelas_ri, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", dto.fb_cover_rj, SqlDbType.Bit);
        dp.AddParam("@fs_kd_bu", dto.fs_kd_bu ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_departemen", dto.fs_departemen ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@fs_no_payroll", dto.fs_no_payroll ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@crttgl", dto.crttgl, SqlDbType.VarChar);
        dp.AddParam("@crtjam", dto.crtjam, SqlDbType.VarChar);
        dp.AddParam("@crtipa", dto.crtipa ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@crtver", dto.crtver ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@crtusr", dto.crtusr ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updtgl", dto.updtgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", dto.updjam, SqlDbType.VarChar);
        dp.AddParam("@updipa", dto.updipa ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updver", dto.updver ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PolisDto dto)
    {
        const string sql = """
            UPDATE ta_polis
            SET 
                fs_no_polis = @fs_no_polis,
                fs_atas_nama = @fs_atas_nama,
                fd_expired = @fd_expired,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fb_cover_rj = @fb_cover_rj,
                fs_kd_kelas_ri = @fs_kd_kelas_ri,
                fs_kd_bu = ISNULL(fs_kd_bu, ''),
                fs_departemen = ISNULL(fs_departemen, ''),
                fs_no_payroll = ISNULL(fs_no_payroll, ''),
                updtgl = @updtgl,
                updjam = @updjam,
                updipa = @updipa,
                updver = @updver,
                updusr = @updusr
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var now = DateTime.Now;
        var updTgl = string.IsNullOrWhiteSpace(dto.updtgl) || dto.updtgl == "3000-01-01"
            ? now.ToString("yyyy-MM-dd")
            : dto.updtgl;
        var updJam = string.IsNullOrWhiteSpace(dto.updjam) || dto.updjam == "00:00:00"
            ? now.ToString("HH:mm:ss")
            : dto.updjam;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", dto.fs_kd_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", dto.fs_no_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", dto.fs_atas_nama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", dto.fd_expired, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", dto.fb_cover_rj, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", dto.fs_kd_kelas_ri, SqlDbType.VarChar);
        dp.AddParam("@updtgl", updTgl, SqlDbType.VarChar);
        dp.AddParam("@updjam", updJam, SqlDbType.VarChar);
        dp.AddParam("@updipa", dto.updipa ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updver", dto.updver ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@updusr", dto.updusr ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPolisKey key)
    {
        const string sql = """
            DELETE FROM ta_polis
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PolisDto GetData(IPolisKey key)
    {
        const string sql = """
            SELECT
                ISNULL(aa.fs_kd_polis, '') AS fs_kd_polis,
                ISNULL(aa.fs_kd_tipe_jaminan, '') AS fs_kd_tipe_jaminan,
                ISNULL(aa.fs_kd_kelas_ri, '') AS fs_kd_kelas_ri, 
                ISNULL(aa.fs_no_polis, '') AS fs_no_polis,
                ISNULL(aa.fs_atas_nama, '') AS fs_atas_nama,
                ISNULL(aa.fd_expired, '3000-01-01') AS fd_expired,
                ISNULL(aa.fb_cover_rj, 0) AS fb_cover_rj, 
                ISNULL(bb.fs_nm_tipe_jaminan, '') AS fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') AS fs_nm_kelas,
                ISNULL(aa.fs_kd_bu, '') AS fs_kd_bu,
                ISNULL(aa.fs_departemen, '') AS fs_departemen,
                ISNULL(aa.fs_no_payroll, '') AS fs_no_payroll,
                ISNULL(aa.crttgl, '3000-01-01') AS crttgl,
                ISNULL(aa.crtjam, '00:00:00') AS crtjam,
                ISNULL(aa.crtipa, '') AS crtipa,
                ISNULL(aa.crtver, '') AS crtver,
                ISNULL(aa.crtusr, '') AS crtusr,
                ISNULL(aa.updtgl, '3000-01-01') AS updtgl,
                ISNULL(aa.updjam, '00:00:00') AS updjam,
                ISNULL(aa.updipa, '') AS updipa,
                ISNULL(aa.updver, '') AS updver,
                ISNULL(aa.updusr, '') AS updusr
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
            WHERE 
                aa.fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PolisDto>(sql, dp);
    }

    public IEnumerable<PolisViewDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT
                ISNULL(aa.fs_kd_polis, '') AS fs_kd_polis,
                ISNULL(aa.fs_no_polis, '') AS fs_no_polis,
                ISNULL(aa.fs_atas_nama, '') AS fs_atas_nama, 
                ISNULL(aa.fs_kd_tipe_jaminan, '') AS fs_kd_tipe_jaminan,
                ISNULL(aa.fd_expired, '3000-01-01') AS fd_tgl_expired,
                ISNULL(aa.fs_kd_kelas_ri, '') AS fs_kd_kelas_ri,
                ISNULL(ee.fs_nm_kelas, '') AS fs_nm_kelas,
                ISNULL(bb.fs_mr, '') AS fs_mr,
                ISNULL(cc.fs_nm_pasien, '') AS fs_nm_pasien,
                ISNULL(cc.fd_tgl_lahir, '') AS fd_tgl_lahir,
                ISNULL(cc.fs_jns_kelamin, '') AS fs_jns_kelamin,
                ISNULL(dd.fs_nm_tipe_jaminan, '') AS fs_nm_tipe_jaminan
            FROM 
                ta_polis aa
                LEFT JOIN ta_polis_cover bb ON aa.fs_kd_polis = bb.fs_kd_polis
                LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
                LEFT JOIN ta_tipe_jaminan dd ON aa.fs_kd_tipe_jaminan = dd.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas ee ON aa.fs_kd_kelas_ri = ee.fs_kd_kelas
            WHERE 
                bb.fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_mr", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PolisViewDto>(sql, dp);
    }

    public PolisDto GetDataByNoPeserta(string noPeserta)
    {
        const string sql = """
            SELECT
                ISNULL(aa.fs_kd_polis, '') AS fs_kd_polis,
                ISNULL(aa.fs_kd_tipe_jaminan, '') AS fs_kd_tipe_jaminan,
                ISNULL(aa.fs_kd_kelas_ri, '') AS fs_kd_kelas_ri, 
                ISNULL(aa.fs_no_polis, '') AS fs_no_polis,
                ISNULL(aa.fs_atas_nama, '') AS fs_atas_nama,
                ISNULL(aa.fd_expired, '3000-01-01') AS fd_expired,
                ISNULL(aa.fb_cover_rj, 0) AS fb_cover_rj, 
                ISNULL(bb.fs_nm_tipe_jaminan, '') AS fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') AS fs_nm_kelas,
                ISNULL(aa.fs_kd_bu, '') AS fs_kd_bu,
                ISNULL(aa.fs_departemen, '') AS fs_departemen,
                ISNULL(aa.fs_no_payroll, '') AS fs_no_payroll,
                ISNULL(aa.crttgl, '3000-01-01') AS crttgl,
                ISNULL(aa.crtjam, '00:00:00') AS crtjam,
                ISNULL(aa.crtipa, '') AS crtipa,
                ISNULL(aa.crtver, '') AS crtver,
                ISNULL(aa.crtusr, '') AS crtusr,
                ISNULL(aa.updtgl, '3000-01-01') AS updtgl,
                ISNULL(aa.updjam, '00:00:00') AS updjam,
                ISNULL(aa.updipa, '') AS updipa,
                ISNULL(aa.updver, '') AS updver,
                ISNULL(aa.updusr, '') AS updusr
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
            WHERE 
                aa.fs_no_polis = @fs_no_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_no_polis", noPeserta, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PolisDto>(sql, dp);
    }
}
