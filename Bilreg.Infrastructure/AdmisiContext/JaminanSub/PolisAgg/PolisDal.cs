using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanSub.PolisAgg;

public class PolisDal : IPolisDal
{
    private readonly DatabaseOptions _opt;

    public PolisDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PolisModel model)
    {
        const string sql = @"
            INSERT INTO ta_polis(
                fs_kd_polis, fs_no_polis, fs_atas_nama, fd_expired, 
                fs_kd_tipe_jaminan, fb_cover_rj, fs_kd_kelas_ri)
            VALUES(
                @fs_kd_polis, @fs_no_polis, @fs_atas_nama, @fd_expired, 
                @fs_kd_tipe_jaminan, @fb_cover_rj, @fs_kd_kelas_ri
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_polis", model.PolisId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", model.NoPolis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", model.AtasNama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", model.ExpiredDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", model.IsCoverRajal, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", model.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PolisModel model)
    {
        const string sql = @"
            UPDATE ta_polis
            SET 
                fs_no_polis = @fs_no_polis,
                fs_atas_nama = @fs_atas_nama,
                fd_expired = @fd_expired,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fb_cover_rj = @fb_cover_rj,
                fs_kd_kelas_ri = @fs_kd_kelas_ri
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", model.PolisId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polis", model.NoPolis, SqlDbType.VarChar);
        dp.AddParam("@fs_atas_nama", model.AtasNama, SqlDbType.VarChar);
        dp.AddParam("@fd_expired", model.ExpiredDate.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fb_cover_rj", model.IsCoverRajal, SqlDbType.Bit);
        dp.AddParam("@fs_kd_kelas_ri", model.KelasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPolisKey key)
    {
        const string sql = @"
            DELETE FROM ta_polis
            WHERE fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PolisModel GetData(IPolisKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_polis, aa.fs_no_polis, aa.fs_atas_nama, aa.fd_expired, 
                aa.fs_kd_tipe_jaminan, aa.fb_cover_rj, aa.fs_kd_kelas_ri,
                ISNULL(bb.fs_nm_tipe_jaminan, '') fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas_ri
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
            WHERE 
                fs_kd_polis = @fs_kd_polis";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_kd_polis", key.PolisId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PolisDto>(sql, dp);
    }

    public IEnumerable<PolisModel> ListData(IPasienKey filter)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_polis, aa.fs_no_polis, aa.fs_atas_nama, aa.fd_expired, 
                aa.fs_kd_tipe_jaminan, aa.fb_cover_rj, aa.fs_kd_kelas_ri,
                ISNULL(bb.fs_nm_tipe_jaminan, '') fs_nm_tipe_jaminan,
                ISNULL(cc.fs_nm_kelas, '') fs_nm_kelas_ri
            FROM 
                ta_polis aa
                LEFT JOIN ta_tipe_jaminan bb ON aa.fs_kd_tipe_jaminan = bb.fs_kd_tipe_jaminan
                LEFT JOIN ta_kelas cc ON aa.fs_kd_kelas_ri = cc.fs_kd_kelas
                LEFT JOIN ta_polis_cover dd ON aa.fs_kd_polis = dd.fs_kd_polis
            WHERE 
                dd.fs_mr = @fs_mr";

        var dp = new DynamicParameters();

        dp.AddParam("@fs_mr", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PolisDto>(sql, dp);
    }

    public IEnumerable<PolisModel> ListData(string filter)
    {
        throw new NotImplementedException();
    }
}