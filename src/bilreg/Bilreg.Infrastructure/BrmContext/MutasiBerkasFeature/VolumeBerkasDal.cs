using System.Data;
using System.Data.SqlClient;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public interface IVolumeBerkasDal :
    IInsert<VolumeBerkasDto>,
    IUpdate<VolumeBerkasDto>
{
    void Upsert(VolumeBerkasDto dto);
    VolumeBerkasDto? GetData(string regId);
    VolumeBerkasDto? GetByMr(string mrId);
    void Delete(string regId);
}

public class VolumeBerkasDal : IVolumeBerkasDal
{
    private readonly DatabaseOptions _opt;

    public VolumeBerkasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(VolumeBerkasDto dto)
    {
        const string sql = """
            INSERT INTO TC_MR8 (
                FS_KD_REG, FS_MR, FN_QTY_BERKAS_PINJAM, FN_QTY_BERKAS_KEMBALI,
                FN_QTY_BERKAS_PINJAM_RI, FN_QTY_BERKAS_KEMBALI_RI,
                FN_QTY_BERKAS_PINJAM_RJ, FN_QTY_BERKAS_KEMBALI_RJ,
                FD_TGL_LAST, FS_JAM_LAST
            ) VALUES (
                @FS_KD_REG, @FS_MR, @FN_QTY_BERKAS_PINJAM, @FN_QTY_BERKAS_KEMBALI,
                @FN_QTY_BERKAS_PINJAM_RI, @FN_QTY_BERKAS_KEMBALI_RI,
                @FN_QTY_BERKAS_PINJAM_RJ, @FN_QTY_BERKAS_KEMBALI_RJ,
                @FD_TGL_LAST, @FS_JAM_LAST
            )
            """;

        var tglLast = string.IsNullOrWhiteSpace(dto.FD_TGL_LAST) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_LAST;
        var jamLast = string.IsNullOrWhiteSpace(dto.FS_JAM_LAST) ? DateTime.Now.ToString("HH:mm:ss") : dto.FS_JAM_LAST;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_REG", dto.FS_KD_REG, SqlDbType.VarChar);
        dp.AddParam("@FS_MR", dto.FS_MR, SqlDbType.VarChar);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM", dto.FN_QTY_BERKAS_PINJAM, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI", dto.FN_QTY_BERKAS_KEMBALI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RI", dto.FN_QTY_BERKAS_PINJAM_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RI", dto.FN_QTY_BERKAS_KEMBALI_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RJ", dto.FN_QTY_BERKAS_PINJAM_RJ, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RJ", dto.FN_QTY_BERKAS_KEMBALI_RJ, SqlDbType.Decimal);
        dp.AddParam("@FD_TGL_LAST", tglLast, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_LAST", jamLast, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(VolumeBerkasDto dto)
    {
        const string sql = """
            UPDATE TC_MR8
            SET
                FS_MR = @FS_MR,
                FN_QTY_BERKAS_PINJAM = @FN_QTY_BERKAS_PINJAM,
                FN_QTY_BERKAS_KEMBALI = @FN_QTY_BERKAS_KEMBALI,
                FN_QTY_BERKAS_PINJAM_RI = @FN_QTY_BERKAS_PINJAM_RI,
                FN_QTY_BERKAS_KEMBALI_RI = @FN_QTY_BERKAS_KEMBALI_RI,
                FN_QTY_BERKAS_PINJAM_RJ = @FN_QTY_BERKAS_PINJAM_RJ,
                FN_QTY_BERKAS_KEMBALI_RJ = @FN_QTY_BERKAS_KEMBALI_RJ,
                FD_TGL_LAST = @FD_TGL_LAST,
                FS_JAM_LAST = @FS_JAM_LAST
            WHERE
                FS_KD_REG = @FS_KD_REG
            """;

        var tglLast = string.IsNullOrWhiteSpace(dto.FD_TGL_LAST) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_LAST;
        var jamLast = string.IsNullOrWhiteSpace(dto.FS_JAM_LAST) ? DateTime.Now.ToString("HH:mm:ss") : dto.FS_JAM_LAST;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_REG", dto.FS_KD_REG, SqlDbType.VarChar);
        dp.AddParam("@FS_MR", dto.FS_MR, SqlDbType.VarChar);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM", dto.FN_QTY_BERKAS_PINJAM, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI", dto.FN_QTY_BERKAS_KEMBALI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RI", dto.FN_QTY_BERKAS_PINJAM_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RI", dto.FN_QTY_BERKAS_KEMBALI_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RJ", dto.FN_QTY_BERKAS_PINJAM_RJ, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RJ", dto.FN_QTY_BERKAS_KEMBALI_RJ, SqlDbType.Decimal);
        dp.AddParam("@FD_TGL_LAST", tglLast, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_LAST", jamLast, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Upsert(VolumeBerkasDto dto)
    {
        const string sql = """
            IF EXISTS (SELECT 1 FROM TC_MR8 WHERE FS_KD_REG = @FS_KD_REG)
            BEGIN
                UPDATE TC_MR8
                SET
                    FS_MR = @FS_MR,
                    FN_QTY_BERKAS_PINJAM = @FN_QTY_BERKAS_PINJAM,
                    FN_QTY_BERKAS_KEMBALI = @FN_QTY_BERKAS_KEMBALI,
                    FN_QTY_BERKAS_PINJAM_RI = @FN_QTY_BERKAS_PINJAM_RI,
                    FN_QTY_BERKAS_KEMBALI_RI = @FN_QTY_BERKAS_KEMBALI_RI,
                    FN_QTY_BERKAS_PINJAM_RJ = @FN_QTY_BERKAS_PINJAM_RJ,
                    FN_QTY_BERKAS_KEMBALI_RJ = @FN_QTY_BERKAS_KEMBALI_RJ,
                    FD_TGL_LAST = @FD_TGL_LAST,
                    FS_JAM_LAST = @FS_JAM_LAST
                WHERE
                    FS_KD_REG = @FS_KD_REG
            END
            ELSE
            BEGIN
                INSERT INTO TC_MR8 (
                    FS_KD_REG, FS_MR, FN_QTY_BERKAS_PINJAM, FN_QTY_BERKAS_KEMBALI,
                    FN_QTY_BERKAS_PINJAM_RI, FN_QTY_BERKAS_KEMBALI_RI,
                    FN_QTY_BERKAS_PINJAM_RJ, FN_QTY_BERKAS_KEMBALI_RJ,
                    FD_TGL_LAST, FS_JAM_LAST
                ) VALUES (
                    @FS_KD_REG, @FS_MR, @FN_QTY_BERKAS_PINJAM, @FN_QTY_BERKAS_KEMBALI,
                    @FN_QTY_BERKAS_PINJAM_RI, @FN_QTY_BERKAS_KEMBALI_RI,
                    @FN_QTY_BERKAS_PINJAM_RJ, @FN_QTY_BERKAS_KEMBALI_RJ,
                    @FD_TGL_LAST, @FS_JAM_LAST
                )
            END
            """;

        var tglLast = string.IsNullOrWhiteSpace(dto.FD_TGL_LAST) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_LAST;
        var jamLast = string.IsNullOrWhiteSpace(dto.FS_JAM_LAST) ? DateTime.Now.ToString("HH:mm:ss") : dto.FS_JAM_LAST;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_REG", dto.FS_KD_REG, SqlDbType.VarChar);
        dp.AddParam("@FS_MR", dto.FS_MR, SqlDbType.VarChar);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM", dto.FN_QTY_BERKAS_PINJAM, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI", dto.FN_QTY_BERKAS_KEMBALI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RI", dto.FN_QTY_BERKAS_PINJAM_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RI", dto.FN_QTY_BERKAS_KEMBALI_RI, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_PINJAM_RJ", dto.FN_QTY_BERKAS_PINJAM_RJ, SqlDbType.Decimal);
        dp.AddParam("@FN_QTY_BERKAS_KEMBALI_RJ", dto.FN_QTY_BERKAS_KEMBALI_RJ, SqlDbType.Decimal);
        dp.AddParam("@FD_TGL_LAST", tglLast, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_LAST", jamLast, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public VolumeBerkasDto? GetData(string regId)
    {
        const string sql = """
            SELECT
                FS_KD_REG, FS_MR, FN_QTY_BERKAS_PINJAM, FN_QTY_BERKAS_KEMBALI,
                FN_QTY_BERKAS_PINJAM_RI, FN_QTY_BERKAS_KEMBALI_RI,
                FN_QTY_BERKAS_PINJAM_RJ, FN_QTY_BERKAS_KEMBALI_RJ,
                FD_TGL_LAST, FS_JAM_LAST
            FROM TC_MR8
            WHERE FS_KD_REG = @FS_KD_REG
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_REG", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<VolumeBerkasDto>(sql, dp);
    }

    public VolumeBerkasDto? GetByMr(string mrId)
    {
        const string sql = """
            SELECT TOP 1
                FS_KD_REG, FS_MR, FN_QTY_BERKAS_PINJAM, FN_QTY_BERKAS_KEMBALI,
                FN_QTY_BERKAS_PINJAM_RI, FN_QTY_BERKAS_KEMBALI_RI,
                FN_QTY_BERKAS_PINJAM_RJ, FN_QTY_BERKAS_KEMBALI_RJ,
                FD_TGL_LAST, FS_JAM_LAST
            FROM TC_MR8
            WHERE FS_MR = @FS_MR
            ORDER BY FD_TGL_LAST DESC, FS_JAM_LAST DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_MR", mrId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<VolumeBerkasDto>(sql, dp);
    }

    public void Delete(string regId)
    {
        const string sql = """
            DELETE FROM TC_MR8
            WHERE FS_KD_REG = @FS_KD_REG
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_REG", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
