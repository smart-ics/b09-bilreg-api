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
                fs_kd_tipe_jaminan, fb_cover_rj, fs_kd_kelas_ri
                )
            VALUES(
                @fs_kd_polis, @fs_no_polis, @fs_atas_nama, @fd_expired, 
                @fs_kd_tipe_jaminan, @fb_cover_rj, @fs_kd_kelas_ri
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
                fs_kd_kelas_ri = @fs_kd_kelas_ri
            WHERE fs_kd_polis = @fs_kd_polis
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", dto.fs_kd_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", dto.fs_no_polis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", dto.fs_atas_nama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", dto.fd_expired, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", dto.fs_kd_tipe_jaminan, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", dto.fb_cover_rj, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", dto.fs_kd_kelas_ri, SqlDbType.VarChar);

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
                aa.fs_kd_polis, aa.fs_kd_tipe_jaminan, aa.fs_kd_kelas_ri, 
                aa.fs_no_polis, aa.fs_atas_nama, aa.fd_expired, aa.fb_cover_rj, 
                ISNULL(bb.fs_nm_tipe_jaminan, '') fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
            WHERE 
                fs_kd_polis = @fs_kd_polis
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
                aa.fs_kd_polis, aa.fs_no_polis, aa.fs_atas_nama, 
                aa.fs_kd_tipe_jaminan, aa.fd_expired AS fd_tgl_expired,
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
            WHERE 
                bb.fs_mr = @fs_mr
            """;

        var dp = new DynamicParameters();

        dp.AddParam("@fs_mr", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PolisViewDto>(sql, dp);
    }
}
