using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BrmContext.MutasiBerkasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BrmContext.MutasiBerkasFeature;

public interface IMutasiBerkasDal :
    IInsert<MutasiBerkasDto>,
    IUpdate<MutasiBerkasDto>,
    IDelete<IMutasiBerkasKey>,
    IGetData<MutasiBerkasDto, IMutasiBerkasKey>
{
    MutasiBerkasDto? GetByKey(string mutasiId);
    IEnumerable<MutasiBerkasDto> ListByTrsBuat(string trsBuatId);
    MutasiBerkasDto? GetLatestActiveByMr(string mrId);
}

public class MutasiBerkasDal : IMutasiBerkasDal
{
    private readonly DatabaseOptions _opt;

    public MutasiBerkasDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(MutasiBerkasDto dto)
    {
        const string sql = """
            INSERT INTO TA_TRS_MR_MUTASI (
                FS_KD_TRS_BUAT, FS_MR, FS_KD_MUTASI, FD_TGL_MUTASI, FS_JAM_MUTASI,
                FS_KD_PETUGAS_MEDIS, FS_KD_LAYANAN, FD_TGL_PRAKTEK, FS_JAM_PRAKTEK, FS_KD_SESION_POLI,
                FN_STATUS, FS_KD_PETUGAS, FN_CETAK, FS_KD_PETUGAS_CETAK, fd_tgl_cetak,
                fs_jam_cetak, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            ) VALUES (
                @FS_KD_TRS_BUAT, @FS_MR, @FS_KD_MUTASI, @FD_TGL_MUTASI, @FS_JAM_MUTASI,
                @FS_KD_PETUGAS_MEDIS, @FS_KD_LAYANAN, @FD_TGL_PRAKTEK, @FS_JAM_PRAKTEK, @FS_KD_SESION_POLI,
                @FN_STATUS, @FS_KD_PETUGAS, @FN_CETAK, @FS_KD_PETUGAS_CETAK, @fd_tgl_cetak,
                @fs_jam_cetak, @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR
            )
            """;

        var crttgl = string.IsNullOrWhiteSpace(dto.CRTTGL) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.CRTTGL;
        var crtjam = string.IsNullOrWhiteSpace(dto.CRTJAM) ? DateTime.Now.ToString("HH:mm:ss") : dto.CRTJAM;
        var crtipa = string.IsNullOrWhiteSpace(dto.CRTIPA) ? "127.0.0.1" : dto.CRTIPA;
        var crtver = string.IsNullOrWhiteSpace(dto.CRTVER) ? "1.0" : dto.CRTVER;
        var crtusr = string.IsNullOrWhiteSpace(dto.CRTUSR)
            ? (string.IsNullOrWhiteSpace(dto.FS_KD_PETUGAS) ? "SYSTEM" : dto.FS_KD_PETUGAS)
            : dto.CRTUSR;

        var updtgl = string.IsNullOrWhiteSpace(dto.UPDTGL) ? crttgl : dto.UPDTGL;
        var updjam = string.IsNullOrWhiteSpace(dto.UPDJAM) ? crtjam : dto.UPDJAM;
        var updipa = string.IsNullOrWhiteSpace(dto.UPDIPA) ? crtipa : dto.UPDIPA;
        var updver = string.IsNullOrWhiteSpace(dto.UPDVER) ? crtver : dto.UPDVER;
        var updusr = string.IsNullOrWhiteSpace(dto.UPDUSR) ? crtusr : dto.UPDUSR;

        var tglMutasi = string.IsNullOrWhiteSpace(dto.FD_TGL_MUTASI) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_MUTASI;
        var jamMutasi = string.IsNullOrWhiteSpace(dto.FS_JAM_MUTASI) ? DateTime.Now.ToString("HH:mm:ss") : dto.FS_JAM_MUTASI;
        var tglPraktek = string.IsNullOrWhiteSpace(dto.FD_TGL_PRAKTEK) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_PRAKTEK;
        var jamPraktek = string.IsNullOrWhiteSpace(dto.FS_JAM_PRAKTEK) ? "00:00" : dto.FS_JAM_PRAKTEK;
        var tglCetak = string.IsNullOrWhiteSpace(dto.fd_tgl_cetak) ? "3000-01-01" : dto.fd_tgl_cetak;
        var jamCetak = string.IsNullOrWhiteSpace(dto.fs_jam_cetak) ? "00:00:00" : dto.fs_jam_cetak;
        var tglVoid = string.IsNullOrWhiteSpace(dto.fd_tgl_void) ? "3000-01-01" : dto.fd_tgl_void;
        var jamVoid = string.IsNullOrWhiteSpace(dto.fs_jam_void) ? "00:00:00" : dto.fs_jam_void;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS_BUAT", dto.FS_KD_TRS_BUAT, SqlDbType.VarChar);
        dp.AddParam("@FS_MR", dto.FS_MR, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_MUTASI", dto.FS_KD_MUTASI, SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_MUTASI", tglMutasi, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_MUTASI", jamMutasi, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PETUGAS_MEDIS", dto.FS_KD_PETUGAS_MEDIS, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN", dto.FS_KD_LAYANAN, SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_PRAKTEK", tglPraktek, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_PRAKTEK", jamPraktek, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_SESION_POLI", dto.FS_KD_SESION_POLI, SqlDbType.VarChar);
        dp.AddParam("@FN_STATUS", dto.FN_STATUS, SqlDbType.Int);
        dp.AddParam("@FS_KD_PETUGAS", dto.FS_KD_PETUGAS, SqlDbType.VarChar);
        dp.AddParam("@FN_CETAK", dto.FN_CETAK, SqlDbType.Int);
        dp.AddParam("@FS_KD_PETUGAS_CETAK", dto.FS_KD_PETUGAS_CETAK, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_cetak", tglCetak, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_cetak", jamCetak, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", tglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", jamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@CRTTGL", crttgl, SqlDbType.VarChar);
        dp.AddParam("@CRTJAM", crtjam, SqlDbType.VarChar);
        dp.AddParam("@CRTIPA", crtipa, SqlDbType.VarChar);
        dp.AddParam("@CRTVER", crtver, SqlDbType.VarChar);
        dp.AddParam("@CRTUSR", crtusr, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", updtgl, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", updjam, SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", updipa, SqlDbType.VarChar);
        dp.AddParam("@UPDVER", updver, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(MutasiBerkasDto dto)
    {
        const string sql = """
            UPDATE TA_TRS_MR_MUTASI
            SET
                FS_KD_TRS_BUAT = @FS_KD_TRS_BUAT,
                FS_MR = @FS_MR,
                FD_TGL_MUTASI = @FD_TGL_MUTASI,
                FS_JAM_MUTASI = @FS_JAM_MUTASI,
                FS_KD_PETUGAS_MEDIS = @FS_KD_PETUGAS_MEDIS,
                FS_KD_LAYANAN = @FS_KD_LAYANAN,
                FD_TGL_PRAKTEK = @FD_TGL_PRAKTEK,
                FS_JAM_PRAKTEK = @FS_JAM_PRAKTEK,
                FS_KD_SESION_POLI = @FS_KD_SESION_POLI,
                FN_STATUS = @FN_STATUS,
                FS_KD_PETUGAS = @FS_KD_PETUGAS,
                FN_CETAK = @FN_CETAK,
                FS_KD_PETUGAS_CETAK = @FS_KD_PETUGAS_CETAK,
                fd_tgl_cetak = @fd_tgl_cetak,
                fs_jam_cetak = @fs_jam_cetak,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                UPDTGL = @UPDTGL,
                UPDJAM = @UPDJAM,
                UPDIPA = @UPDIPA,
                UPDVER = @UPDVER,
                UPDUSR = @UPDUSR
            WHERE
                FS_KD_MUTASI = @FS_KD_MUTASI
            """;

        var updtgl = string.IsNullOrWhiteSpace(dto.UPDTGL) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.UPDTGL;
        var updjam = string.IsNullOrWhiteSpace(dto.UPDJAM) ? DateTime.Now.ToString("HH:mm:ss") : dto.UPDJAM;
        var updipa = string.IsNullOrWhiteSpace(dto.UPDIPA) ? "127.0.0.1" : dto.UPDIPA;
        var updver = string.IsNullOrWhiteSpace(dto.UPDVER) ? "1.0" : dto.UPDVER;
        var updusr = string.IsNullOrWhiteSpace(dto.UPDUSR)
            ? (string.IsNullOrWhiteSpace(dto.FS_KD_PETUGAS) ? "SYSTEM" : dto.FS_KD_PETUGAS)
            : dto.UPDUSR;

        var tglMutasi = string.IsNullOrWhiteSpace(dto.FD_TGL_MUTASI) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_MUTASI;
        var jamMutasi = string.IsNullOrWhiteSpace(dto.FS_JAM_MUTASI) ? DateTime.Now.ToString("HH:mm:ss") : dto.FS_JAM_MUTASI;
        var tglPraktek = string.IsNullOrWhiteSpace(dto.FD_TGL_PRAKTEK) ? DateTime.Now.ToString("yyyy-MM-dd") : dto.FD_TGL_PRAKTEK;
        var jamPraktek = string.IsNullOrWhiteSpace(dto.FS_JAM_PRAKTEK) ? "00:00" : dto.FS_JAM_PRAKTEK;
        var tglCetak = string.IsNullOrWhiteSpace(dto.fd_tgl_cetak) ? "3000-01-01" : dto.fd_tgl_cetak;
        var jamCetak = string.IsNullOrWhiteSpace(dto.fs_jam_cetak) ? "00:00:00" : dto.fs_jam_cetak;
        var tglVoid = string.IsNullOrWhiteSpace(dto.fd_tgl_void) ? "3000-01-01" : dto.fd_tgl_void;
        var jamVoid = string.IsNullOrWhiteSpace(dto.fs_jam_void) ? "00:00:00" : dto.fs_jam_void;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS_BUAT", dto.FS_KD_TRS_BUAT, SqlDbType.VarChar);
        dp.AddParam("@FS_MR", dto.FS_MR, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_MUTASI", dto.FS_KD_MUTASI, SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_MUTASI", tglMutasi, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_MUTASI", jamMutasi, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_PETUGAS_MEDIS", dto.FS_KD_PETUGAS_MEDIS, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_LAYANAN", dto.FS_KD_LAYANAN, SqlDbType.VarChar);
        dp.AddParam("@FD_TGL_PRAKTEK", tglPraktek, SqlDbType.VarChar);
        dp.AddParam("@FS_JAM_PRAKTEK", jamPraktek, SqlDbType.VarChar);
        dp.AddParam("@FS_KD_SESION_POLI", dto.FS_KD_SESION_POLI, SqlDbType.VarChar);
        dp.AddParam("@FN_STATUS", dto.FN_STATUS, SqlDbType.Int);
        dp.AddParam("@FS_KD_PETUGAS", dto.FS_KD_PETUGAS, SqlDbType.VarChar);
        dp.AddParam("@FN_CETAK", dto.FN_CETAK, SqlDbType.Int);
        dp.AddParam("@FS_KD_PETUGAS_CETAK", dto.FS_KD_PETUGAS_CETAK, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_cetak", tglCetak, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_cetak", jamCetak, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", tglVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", jamVoid, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", dto.fs_kd_petugas_void, SqlDbType.VarChar);
        dp.AddParam("@UPDTGL", updtgl, SqlDbType.VarChar);
        dp.AddParam("@UPDJAM", updjam, SqlDbType.VarChar);
        dp.AddParam("@UPDIPA", updipa, SqlDbType.VarChar);
        dp.AddParam("@UPDVER", updver, SqlDbType.VarChar);
        dp.AddParam("@UPDUSR", updusr, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IMutasiBerkasKey key)
    {
        const string sql = """
            DELETE FROM TA_TRS_MR_MUTASI
            WHERE FS_KD_MUTASI = @FS_KD_MUTASI
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_MUTASI", key.MutasiBerkasId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MutasiBerkasDto GetData(IMutasiBerkasKey key)
    {
        return GetByKey(key.MutasiBerkasId)!;
    }

    public MutasiBerkasDto? GetByKey(string mutasiId)
    {
        const string sql = """
            SELECT 
                FS_KD_TRS_BUAT, FS_MR, FS_KD_MUTASI, FD_TGL_MUTASI, FS_JAM_MUTASI,
                FS_KD_PETUGAS_MEDIS, FS_KD_LAYANAN, FD_TGL_PRAKTEK, FS_JAM_PRAKTEK, FS_KD_SESION_POLI,
                FN_STATUS, FS_KD_PETUGAS, FN_CETAK, FS_KD_PETUGAS_CETAK, fd_tgl_cetak,
                fs_jam_cetak, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            FROM TA_TRS_MR_MUTASI
            WHERE FS_KD_MUTASI = @FS_KD_MUTASI
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_MUTASI", mutasiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<MutasiBerkasDto>(sql, dp);
    }

    public IEnumerable<MutasiBerkasDto> ListByTrsBuat(string trsBuatId)
    {
        const string sql = """
            SELECT 
                FS_KD_TRS_BUAT, FS_MR, FS_KD_MUTASI, FD_TGL_MUTASI, FS_JAM_MUTASI,
                FS_KD_PETUGAS_MEDIS, FS_KD_LAYANAN, FD_TGL_PRAKTEK, FS_JAM_PRAKTEK, FS_KD_SESION_POLI,
                FN_STATUS, FS_KD_PETUGAS, FN_CETAK, FS_KD_PETUGAS_CETAK, fd_tgl_cetak,
                fs_jam_cetak, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            FROM TA_TRS_MR_MUTASI
            WHERE FS_KD_TRS_BUAT = @FS_KD_TRS_BUAT
            ORDER BY FD_TGL_MUTASI DESC, FS_JAM_MUTASI DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_KD_TRS_BUAT", trsBuatId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<MutasiBerkasDto>(sql, dp);
    }

    public MutasiBerkasDto? GetLatestActiveByMr(string mrId)
    {
        const string sql = """
            SELECT TOP 1
                FS_KD_TRS_BUAT, FS_MR, FS_KD_MUTASI, FD_TGL_MUTASI, FS_JAM_MUTASI,
                FS_KD_PETUGAS_MEDIS, FS_KD_LAYANAN, FD_TGL_PRAKTEK, FS_JAM_PRAKTEK, FS_KD_SESION_POLI,
                FN_STATUS, FS_KD_PETUGAS, FN_CETAK, FS_KD_PETUGAS_CETAK, fd_tgl_cetak,
                fs_jam_cetak, fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR
            FROM TA_TRS_MR_MUTASI
            WHERE FS_MR = @FS_MR AND fd_tgl_void = '3000-01-01'
            ORDER BY FD_TGL_MUTASI DESC, FS_JAM_MUTASI DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@FS_MR", mrId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<MutasiBerkasDto>(sql, dp);
    }
}
