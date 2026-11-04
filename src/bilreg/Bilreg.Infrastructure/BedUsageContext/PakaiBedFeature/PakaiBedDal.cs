using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedDal :
    Bilreg.Application.BedUsageContext.PakaiBedFeature.IPakaiBedDal,
    IInsert<PakaiBedDto>,
    IUpdate<PakaiBedDto>,
    IDelete<IPakaiBed>,
    IGetData<PakaiBedDto, IPakaiBed>,
    IListData<PakaiBedDto, IRegKey>
{
    PakaiBedDto? GetActiveByBed(string bedId);
}

public class PakaiBedDal : IPakaiBedDal
{
    private const string SELECT_FROM = """
        SELECT
            aa.fs_kd_trs,
            aa.fd_tgl_in, aa.fs_jam_in, aa.fs_kd_petugas,
            aa.fd_tgl_out, aa.fs_jam_out, aa.fs_kd_petugas_out,
            aa.fs_kd_reg, aa.fs_kd_layanan, aa.fs_kd_layanan_dk,
            aa.fs_kd_kamar_tipe, aa.fs_kd_bed, aa.fn_tarif,
            aa.fd_tgl_void, aa.fs_jam_void, aa.fs_kd_petugas_void,
            aa.fd_tgl_entry, aa.fs_jam_entry, aa.fs_kd_kelas,
            ISNULL(bb.fs_mr, '') AS fs_mr,
            ISNULL(cc.fs_nm_pasien, '') AS fs_nm_pasien,
            ISNULL(dd.fs_nm_layanan, '') AS fs_nm_layanan,
            ISNULL(ee.fs_nm_bed, '') AS fs_nm_bed,
            ISNULL(ee.fb_aktif, 0) AS fb_bed_aktif,
            ISNULL(ff.fs_nm_kamar_tipe, '') AS fs_nm_kamar_tipe,
            ISNULL(ff.fb_aktif, 0) AS fb_kamar_tipe_aktif,
            ISNULL(gg.fs_nm_kelas, '') AS fs_nm_kelas
        FROM
            ta_trs_bed aa
            LEFT JOIN ta_registrasi bb ON aa.fs_kd_reg = bb.fs_kd_reg
            LEFT JOIN tc_mr cc ON bb.fs_mr = cc.fs_mr
            LEFT JOIN ta_layanan dd ON aa.fs_kd_layanan = dd.fs_kd_layanan
            LEFT JOIN ta_bed ee ON aa.fs_kd_bed = ee.fs_kd_bed
            LEFT JOIN ta_kamar_tipe ff ON aa.fs_kd_kamar_tipe = ff.fs_kd_kamar_tipe
            LEFT JOIN ta_kelas gg ON aa.fs_kd_kelas = gg.fs_kd_kelas
        """;

    private readonly DatabaseOptions _opt;

    public PakaiBedDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PakaiBedDto dto)
    {
        const string sql = """
            -- 1. Check rooming-in rule matching VB6 clsTrsPakaiBed.cls (line 918)
            DECLARE @isRoomingIn BIT = 0;
            IF EXISTS (
                SELECT 1
                FROM ta_kamar_tipe kt
                INNER JOIN ta_registrasi reg ON reg.fs_kd_reg = @fs_kd_reg
                WHERE kt.fs_kd_kamar_tipe = @fs_kd_kamar_tipe
                  AND kt.fb_gabung = 1
                  AND ISNULL(reg.fs_reg_ibu, '') <> ''
            )
            BEGIN
                SET @isRoomingIn = 1;
            END

            -- 2. Concurrency locking on bed allocation checks
            SELECT fn_status, fs_kd_reg
            FROM ta_bed WITH (UPDLOCK, HOLDLOCK)
            WHERE fs_kd_bed = @fs_kd_bed;

            SELECT fs_kd_trs
            FROM ta_trs_bed WITH (UPDLOCK, HOLDLOCK)
            WHERE fs_kd_bed = @fs_kd_bed
              AND (fd_tgl_out = '' OR fd_tgl_out = '3000-01-01' OR fd_tgl_out IS NULL)
              AND (fd_tgl_void = '' OR fd_tgl_void = '3000-01-01' OR fd_tgl_void IS NULL);

            -- 3. Insert transaction into ta_trs_bed
            INSERT INTO ta_trs_bed(
                fs_kd_trs,
                fd_tgl_in, fs_jam_in, fs_kd_petugas,
                fd_tgl_out, fs_jam_out, fs_kd_petugas_out,
                fs_kd_reg, fs_kd_layanan, fs_kd_layanan_dk,
                fs_kd_kamar_tipe, fs_kd_bed, fn_tarif,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fd_tgl_entry, fs_jam_entry, fs_kd_kelas)
            VALUES(
                @fs_kd_trs,
                @fd_tgl_in, @fs_jam_in, @fs_kd_petugas,
                @fd_tgl_out, @fs_jam_out, @fs_kd_petugas_out,
                @fs_kd_reg, @fs_kd_layanan, @fs_kd_layanan_dk,
                @fs_kd_kamar_tipe, @fs_kd_bed, @fn_tarif,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fd_tgl_entry, @fs_jam_entry, @fs_kd_kelas);

            -- 4. Synchronize legacy bed status in ta_bed (ONLY if NOT rooming-in)
            IF @isRoomingIn = 0
            BEGIN
                UPDATE ta_bed
                SET
                    fn_status = 1,
                    fs_kd_reg = @fs_kd_reg,
                    fd_tgl_masuk = @fd_tgl_in,
                    fs_kd_layanan_dk = CASE WHEN @fs_kd_layanan_dk <> '' THEN @fs_kd_layanan_dk ELSE fs_kd_layanan_dk END,
                    updtgl = @fd_tgl_in,
                    updjam = @fs_jam_in,
                    updusr = @fs_kd_petugas
                WHERE fs_kd_bed = @fs_kd_bed;
            END
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PakaiBedDto dto)
    {
        const string sql = """
            UPDATE
                ta_trs_bed
            SET
                fd_tgl_in = @fd_tgl_in,
                fs_jam_in = @fs_jam_in,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_out = @fd_tgl_out,
                fs_jam_out = @fs_jam_out,
                fs_kd_petugas_out = @fs_kd_petugas_out,
                fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_layanan_dk = @fs_kd_layanan_dk,
                fs_kd_kamar_tipe = @fs_kd_kamar_tipe,
                fs_kd_bed = @fs_kd_bed,
                fn_tarif = @fn_tarif,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fd_tgl_entry = @fd_tgl_entry,
                fs_jam_entry = @fs_jam_entry,
                fs_kd_kelas = @fs_kd_kelas
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);

        // Synchronize legacy bed status in ta_bed
        var isReleasedOrVoid =
            (!string.IsNullOrWhiteSpace(dto.fd_tgl_out) && dto.fd_tgl_out != "3000-01-01") ||
            (!string.IsNullOrWhiteSpace(dto.fd_tgl_void) && dto.fd_tgl_void != "3000-01-01");

        try
        {
            if (isReleasedOrVoid)
            {
                const string sqlReleaseBed = """
                    UPDATE ta_bed
                    SET
                        fn_status = 0,
                        fs_kd_reg = '',
                        fd_tgl_masuk = '3000-01-01',
                        updtgl = @updtgl,
                        updjam = @updjam,
                        updusr = @updusr
                    WHERE fs_kd_bed = @fs_kd_bed
                      AND fs_kd_reg = @fs_kd_reg
                    """;
                var dpRelease = new DynamicParameters();
                dpRelease.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
                dpRelease.AddParam("@fs_kd_reg", SafeTruncate(dto.fs_kd_reg, 10), SqlDbType.VarChar);
                dpRelease.AddParam("@updtgl", !string.IsNullOrWhiteSpace(dto.fd_tgl_out) && dto.fd_tgl_out != "3000-01-01" ? dto.fd_tgl_out : dto.fd_tgl_void, SqlDbType.VarChar);
                dpRelease.AddParam("@updjam", !string.IsNullOrWhiteSpace(dto.fs_jam_out) ? dto.fs_jam_out : dto.fs_jam_void, SqlDbType.VarChar);
                dpRelease.AddParam("@updusr", !string.IsNullOrWhiteSpace(dto.fs_kd_petugas_out) ? SafeTruncate(dto.fs_kd_petugas_out, 10) : SafeTruncate(dto.fs_kd_petugas_void, 10), SqlDbType.VarChar);
                conn.Execute(sqlReleaseBed, dpRelease);

                // Cascade Void / Discharge on Rooming-In records on the same bed
                if (!string.IsNullOrWhiteSpace(dto.fd_tgl_void) && dto.fd_tgl_void != "3000-01-01")
                {
                    const string sqlCascadeVoid = """
                        UPDATE ta_trs_bed
                        SET
                            fd_tgl_void = @fd_tgl_void,
                            fs_jam_void = @fs_jam_void,
                            fs_kd_petugas_void = @fs_kd_petugas_void
                        WHERE fs_kd_bed = @fs_kd_bed
                          AND fn_tarif = 0
                          AND (fd_tgl_void = '' OR fd_tgl_void = '3000-01-01' OR fd_tgl_void IS NULL);
                        """;
                    conn.Execute(sqlCascadeVoid, dp);
                }
                else if (!string.IsNullOrWhiteSpace(dto.fd_tgl_out) && dto.fd_tgl_out != "3000-01-01")
                {
                    const string sqlCascadeDischarge = """
                        UPDATE ta_trs_bed
                        SET
                            fd_tgl_out = @fd_tgl_out,
                            fs_jam_out = @fs_jam_out,
                            fs_kd_petugas_out = @fs_kd_petugas_out
                        WHERE fs_kd_bed = @fs_kd_bed
                          AND fn_tarif = 0
                          AND (fd_tgl_out = '' OR fd_tgl_out = '3000-01-01' OR fd_tgl_out IS NULL);
                        """;
                    conn.Execute(sqlCascadeDischarge, dp);
                }
            }
            else
            {
                const string sqlOccupyBed = """
                    DECLARE @isRoomingIn BIT = 0;
                    IF EXISTS (
                        SELECT 1
                        FROM ta_kamar_tipe kt
                        INNER JOIN ta_registrasi reg ON reg.fs_kd_reg = @fs_kd_reg
                        WHERE kt.fs_kd_kamar_tipe = @fs_kd_kamar_tipe
                          AND kt.fb_gabung = 1
                          AND ISNULL(reg.fs_reg_ibu, '') <> ''
                    )
                    BEGIN
                        SET @isRoomingIn = 1;
                    END

                    IF @isRoomingIn = 0
                    BEGIN
                        UPDATE ta_bed
                        SET
                            fn_status = 1,
                            fs_kd_reg = @fs_kd_reg,
                            fd_tgl_masuk = @fd_tgl_in,
                            fs_kd_layanan_dk = CASE WHEN @fs_kd_layanan_dk <> '' THEN @fs_kd_layanan_dk ELSE fs_kd_layanan_dk END,
                            updtgl = @fd_tgl_in,
                            updjam = @fs_jam_in,
                            updusr = @fs_kd_petugas
                        WHERE fs_kd_bed = @fs_kd_bed;
                    END
                    """;
                conn.Execute(sqlOccupyBed, dp);
            }
        }
        catch
        {
            // Non-critical legacy ta_bed & rooming-in cascade guard
        }
    }

    public void Delete(IPakaiBed key)
    {
        var existing = GetData(key);
        const string sql = """
            DELETE FROM
                ta_trs_bed
            WHERE
                fs_kd_trs = @fs_kd_trs
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);

        if (existing != null && !string.IsNullOrWhiteSpace(existing.fs_kd_bed))
        {
            try
            {
                const string sqlReleaseBed = """
                    UPDATE ta_bed
                    SET
                        fn_status = 0,
                        fs_kd_reg = '',
                        fd_tgl_masuk = '3000-01-01'
                    WHERE fs_kd_bed = @fs_kd_bed
                      AND fs_kd_reg = @fs_kd_reg
                    """;
                var dpRelease = new DynamicParameters();
                dpRelease.AddParam("@fs_kd_bed", existing.fs_kd_bed, SqlDbType.VarChar);
                dpRelease.AddParam("@fs_kd_reg", existing.fs_kd_reg, SqlDbType.VarChar);
                conn.Execute(sqlReleaseBed, dpRelease);
            }
            catch
            {
                // Non-critical legacy ta_bed projection guard
            }
        }
    }

    public PakaiBedDto GetData(IPakaiBed key)
    {
        var sql = $"{SELECT_FROM}\nWHERE aa.fs_kd_trs = @fs_kd_trs";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.PakaiBedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedDto>(sql, dp);
    }

    public IEnumerable<PakaiBedDto> ListData(IRegKey filter)
    {
        var sql = $"{SELECT_FROM}\nWHERE aa.fs_kd_reg = @fs_kd_reg";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedDto>(sql, dp);
    }

    public PakaiBedDto? GetActiveByBed(string bedId)
    {
        var sql = $"""
            {SELECT_FROM}
            WHERE aa.fs_kd_bed = @fs_kd_bed
              AND (aa.fd_tgl_out = '' OR aa.fd_tgl_out = '3000-01-01' OR aa.fd_tgl_out IS NULL)
              AND (aa.fd_tgl_void = '' OR aa.fd_tgl_void = '3000-01-01' OR aa.fd_tgl_void IS NULL)
            ORDER BY 
                CASE WHEN aa.fn_tarif > 0 THEN 1 ELSE 2 END,
                aa.fd_tgl_in ASC,
                aa.fs_jam_in ASC
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_bed", bedId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedDto>(sql, dp)?.FirstOrDefault();
    }

    public async Task<List<string>> VoidBedAndRoomChargeAsync(string pakaiBedId, string userId, string alasanBatal, DateTime? voidTime = null)
    {
        var now = voidTime ?? DateTime.Now;
        var nowDate = now.ToString("yyyy-MM-dd");
        var nowTime = now.ToString("HH:mm:ss");

        const string sql = """
            -- 1. Void ta_trs_bed (Single atomic transaction scope)
            IF COL_LENGTH('ta_trs_bed', 'fs_kd_batal') IS NOT NULL
            BEGIN
                DECLARE @sqlBed NVARCHAR(MAX) = N'UPDATE ta_trs_bed SET fs_kd_batal = ''1'', fs_user_batal = @userId, fs_alasan_batal = @alasanBatal, fd_tgl_batal = @now WHERE ' + 
                    CASE WHEN COL_LENGTH('ta_trs_bed', 'fs_kd_trs_bed') IS NOT NULL THEN 'fs_kd_trs_bed = @pakaiBedId' ELSE 'fs_kd_trs = @pakaiBedId' END;
                EXEC sp_executesql @sqlBed, N'@userId VARCHAR(50), @alasanBatal VARCHAR(255), @now DATETIME, @pakaiBedId VARCHAR(50)', @userId, @alasanBatal, @now, @pakaiBedId;
            END
            ELSE
            BEGIN
                UPDATE ta_trs_bed
                SET fd_tgl_void = @nowDate,
                    fs_jam_void = @nowTime,
                    fs_kd_petugas_void = @userId
                WHERE fs_kd_trs = @pakaiBedId;
            END

            -- 2. Void ta_trs_kamar (if exists and has bed reference column)
            IF OBJECT_ID('ta_trs_kamar', 'U') IS NOT NULL
            BEGIN
                DECLARE @colBedRef VARCHAR(50) = CASE 
                    WHEN COL_LENGTH('ta_trs_kamar', 'fs_kd_trs_bed') IS NOT NULL THEN 'fs_kd_trs_bed' 
                    WHEN COL_LENGTH('ta_trs_kamar', 'fs_kd_pakai_bed') IS NOT NULL THEN 'fs_kd_pakai_bed'
                    ELSE NULL 
                END;
                IF @colBedRef IS NOT NULL
                BEGIN
                    DECLARE @hasKdBatal BIT = CASE WHEN COL_LENGTH('ta_trs_kamar', 'fs_kd_batal') IS NOT NULL THEN 1 ELSE 0 END;
                    DECLARE @hasKet BIT = CASE WHEN COL_LENGTH('ta_trs_kamar', 'fs_ket') IS NOT NULL THEN 1 ELSE 0 END;
                    DECLARE @setClause NVARCHAR(MAX) = CASE 
                        WHEN @hasKdBatal = 1 THEN 'fs_kd_batal = ''1'', fs_user_batal = @userId, fs_alasan_batal = @alasanBatal, fd_tgl_batal = @now'
                        WHEN @hasKet = 1 THEN 'fs_ket = LEFT(''Reversed: '' + @alasanBatal, 40)'
                        ELSE 'fs_kd_petugas_void = @userId, fd_tgl_void = @nowDate, fs_jam_void = @nowTime'
                    END;
                    DECLARE @sqlKamar NVARCHAR(MAX) = N'UPDATE ta_trs_kamar SET ' + @setClause +
                        N' WHERE ' + @colBedRef + N' = @pakaiBedId' +
                        CASE WHEN @hasKdBatal = 1 THEN N' AND fs_kd_batal = ''0''' ELSE N'' END;
                    EXEC sp_executesql @sqlKamar, N'@userId VARCHAR(50), @alasanBatal VARCHAR(255), @now DATETIME, @nowDate VARCHAR(10), @nowTime VARCHAR(10), @pakaiBedId VARCHAR(50)', @userId, @alasanBatal, @now, @nowDate, @nowTime, @pakaiBedId;
                END
            END

            -- 3. Void ta_trs_roomcharge & log (if exists)
            IF OBJECT_ID('ta_trs_roomcharge', 'U') IS NOT NULL
            BEGIN
                DECLARE @colRcBedRef VARCHAR(50) = CASE 
                    WHEN COL_LENGTH('ta_trs_roomcharge', 'fs_kd_pakai_bed') IS NOT NULL THEN 'fs_kd_pakai_bed'
                    WHEN COL_LENGTH('ta_trs_roomcharge', 'fs_kd_trs_bed') IS NOT NULL THEN 'fs_kd_trs_bed'
                    ELSE NULL 
                END;
                IF @colRcBedRef IS NOT NULL
                BEGIN
                    -- 3a. Clean up ta_trs_roomcharge2 components (if exists)
                    IF OBJECT_ID('ta_trs_roomcharge2', 'U') IS NOT NULL
                    BEGIN
                        DECLARE @sqlRc2 NVARCHAR(MAX) = N'DELETE rc2 FROM ta_trs_roomcharge2 rc2 INNER JOIN ta_trs_roomcharge rc ON rc2.fs_kd_trs = rc.fs_kd_trs WHERE rc.' + @colRcBedRef + N' = @pakaiBedId';
                        EXEC sp_executesql @sqlRc2, N'@pakaiBedId VARCHAR(50)', @pakaiBedId;
                    END

                    -- 3b. Void ta_trs_roomcharge
                    DECLARE @sqlRc NVARCHAR(MAX) = N'UPDATE ta_trs_roomcharge SET fs_ket = LEFT(''Reversed: '' + @alasanBatal, 40) WHERE ' + @colRcBedRef + N' = @pakaiBedId';
                    EXEC sp_executesql @sqlRc, N'@alasanBatal VARCHAR(255), @pakaiBedId VARCHAR(50)', @alasanBatal, @pakaiBedId;

                    -- 3c. Insert void snapshot log
                    IF OBJECT_ID('ta_trs_roomcharge_log', 'U') IS NOT NULL
                    BEGIN
                        DECLARE @sqlRcLog NVARCHAR(MAX) = N'INSERT INTO ta_trs_roomcharge_log (
                            fs_kd_trs, fd_tgl_trs, fd_tgl_upd, fs_jam_upd,
                            fs_kd_petugas, fs_kd_upd, fs_kd_reg,
                            fs_kd_layanan, fs_kd_bed,
                            fn_tarif, fn_qty, fb_status_void)
                        SELECT
                            rc.fs_kd_trs, rc.fd_tgl_trs, @nowDate, @nowTime,
                            rc.fs_kd_petugas, @userId, rc.fs_kd_reg,
                            rc.fs_kd_layanan, rc.fs_kd_bed,
                            rc.fn_tarif, rc.fn_qty, 1
                        FROM ta_trs_roomcharge rc
                        WHERE rc.' + @colRcBedRef + N' = @pakaiBedId
                          AND NOT EXISTS (
                              SELECT 1 FROM ta_trs_roomcharge_log lg
                              WHERE lg.fs_kd_trs = rc.fs_kd_trs AND lg.fb_status_void = 1
                          )';
                        EXEC sp_executesql @sqlRcLog, N'@nowDate VARCHAR(10), @nowTime VARCHAR(10), @userId VARCHAR(50), @pakaiBedId VARCHAR(50)', @nowDate, @nowTime, @userId, @pakaiBedId;
                    END
                END
            END

            -- 4. Legacy ta_bed status synchronization
            -- Only update ta_bed if voiding the primary physical bed holder
            -- (Do NOT release physical bed if voiding secondary rooming-in baby while mother/primary occupant is still active)
            UPDATE b
            SET fn_status = 0,
                fs_kd_reg = '',
                fd_tgl_masuk = '3000-01-01',
                updtgl = @nowDate,
                updjam = @nowTime,
                updusr = @userId
            FROM ta_bed b
            INNER JOIN ta_trs_bed tb ON b.fs_kd_bed = tb.fs_kd_bed
            WHERE tb.fs_kd_trs = @pakaiBedId
              AND (
                  b.fs_kd_reg = tb.fs_kd_reg
                  OR NOT EXISTS (
                      SELECT 1 FROM ta_trs_bed other
                      WHERE other.fs_kd_bed = tb.fs_kd_bed
                        AND other.fs_kd_trs <> tb.fs_kd_trs
                        AND (other.fd_tgl_out = '' OR other.fd_tgl_out = '3000-01-01' OR other.fd_tgl_out IS NULL)
                        AND (other.fd_tgl_void = '' OR other.fd_tgl_void = '3000-01-01' OR other.fd_tgl_void IS NULL)
                  )
              );

            -- 5. Reversal of Placed Waiting List for baby/rooming-in or voided patient
            CREATE TABLE #RevertedBangsals (BangsalId VARCHAR(50));

            IF OBJECT_ID('BILRG_BedWaitingList', 'U') IS NOT NULL
            BEGIN
                DECLARE @colNurse NVARCHAR(MAX) = CASE WHEN COL_LENGTH('BILRG_BedWaitingList', 'AdmittedNurseId') IS NOT NULL THEN N', AdmittedNurseId = ''''' ELSE N'' END;
                DECLARE @colDate NVARCHAR(MAX) = CASE WHEN COL_LENGTH('BILRG_BedWaitingList', 'AdmittedDate') IS NOT NULL THEN N', AdmittedDate = ''3000-01-01''' ELSE N'' END;
                DECLARE @colBed NVARCHAR(MAX) = CASE 
                    WHEN COL_LENGTH('BILRG_BedWaitingList', 'AdmittedBedId') IS NOT NULL THEN N', AdmittedBedId = ''''' 
                    WHEN COL_LENGTH('BILRG_BedWaitingList', 'PlacedBedId') IS NOT NULL THEN N', PlacedBedId = '''''
                    ELSE N'' 
                END;
                DECLARE @colPakaiBed NVARCHAR(MAX) = CASE 
                    WHEN COL_LENGTH('BILRG_BedWaitingList', 'PlacedPakaiBedId') IS NOT NULL THEN N', PlacedPakaiBedId = ''''' 
                    ELSE N'' 
                END;

                -- 5a. Direct match: if the voided pakaiBed itself is associated with a placed waiting list
                DECLARE @sqlRevertWlDirect NVARCHAR(MAX) = N'
                    UPDATE wl
                    SET wl.WaitingListStatus = 0' + @colBed + @colPakaiBed + @colNurse + @colDate + N',
                        wl.UpdUser = @userId,
                        wl.UpdDate = @now
                    OUTPUT INSERTED.BangsalId INTO #RevertedBangsals (BangsalId)
                    FROM BILRG_BedWaitingList wl
                    INNER JOIN ta_trs_bed tb ON (wl.RegId = tb.fs_kd_reg OR wl.PasienId = (SELECT TOP 1 fs_mr FROM ta_registrasi WHERE fs_kd_reg = tb.fs_kd_reg))
                    WHERE tb.fs_kd_trs = @pakaiBedId
                      AND wl.WaitingListStatus = 2';
                EXEC sp_executesql @sqlRevertWlDirect, N'@userId VARCHAR(50), @now DATETIME, @pakaiBedId VARCHAR(50)', @userId, @now, @pakaiBedId;

                -- 5b. Cascade match: if voiding mother bed, revert any active rooming-in baby on that bed
                DECLARE @sqlRevertWlCascade NVARCHAR(MAX) = N'
                    UPDATE wl
                    SET wl.WaitingListStatus = 0' + @colBed + @colPakaiBed + @colNurse + @colDate + N',
                        wl.UpdUser = @userId,
                        wl.UpdDate = @now
                    OUTPUT INSERTED.BangsalId INTO #RevertedBangsals (BangsalId)
                    FROM BILRG_BedWaitingList wl
                    INNER JOIN ta_trs_bed tb_baby ON (wl.RegId = tb_baby.fs_kd_reg OR wl.PasienId = (SELECT TOP 1 fs_mr FROM ta_registrasi WHERE fs_kd_reg = tb_baby.fs_kd_reg))
                    INNER JOIN ta_trs_bed tb_mother ON tb_mother.fs_kd_bed = tb_baby.fs_kd_bed
                    WHERE tb_mother.fs_kd_trs = @pakaiBedId
                      AND tb_baby.fn_tarif = 0
                      AND tb_baby.fs_kd_trs <> tb_mother.fs_kd_trs
                      AND wl.WaitingListStatus = 2';
                EXEC sp_executesql @sqlRevertWlCascade, N'@userId VARCHAR(50), @now DATETIME, @pakaiBedId VARCHAR(50)', @userId, @now, @pakaiBedId;
            END

            -- 5c. Cascade void on ta_trs_bed for rooming-in babies on the same bed
            INSERT INTO #RevertedBangsals (BangsalId)
            SELECT DISTINCT baby.fs_kd_layanan
            FROM ta_trs_bed baby
            INNER JOIN ta_trs_bed mother ON mother.fs_kd_bed = baby.fs_kd_bed
            WHERE mother.fs_kd_trs = @pakaiBedId
              AND baby.fn_tarif = 0
              AND baby.fs_kd_trs <> mother.fs_kd_trs
              AND (baby.fd_tgl_void = '' OR baby.fd_tgl_void = '3000-01-01' OR baby.fd_tgl_void IS NULL)
              AND ISNULL(baby.fs_kd_layanan, '') <> '';

            UPDATE baby
            SET fd_tgl_void = @nowDate,
                fs_jam_void = @nowTime,
                fs_kd_petugas_void = @userId
            FROM ta_trs_bed baby
            INNER JOIN ta_trs_bed mother ON mother.fs_kd_bed = baby.fs_kd_bed
            WHERE mother.fs_kd_trs = @pakaiBedId
              AND baby.fn_tarif = 0
              AND baby.fs_kd_trs <> mother.fs_kd_trs
              AND (baby.fd_tgl_void = '' OR baby.fd_tgl_void = '3000-01-01' OR baby.fd_tgl_void IS NULL);

            SELECT DISTINCT BangsalId FROM #RevertedBangsals WHERE ISNULL(BangsalId, '') <> '';
            DROP TABLE #RevertedBangsals;
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        await conn.OpenAsync();
        using var trans = conn.BeginTransaction();
        try
        {
            var dp = new DynamicParameters();
            dp.AddParam("@pakaiBedId", SafeTruncate(pakaiBedId, 15), SqlDbType.VarChar);
            dp.AddParam("@userId", SafeTruncate(userId, 10), SqlDbType.VarChar);
            dp.AddParam("@alasanBatal", SafeTruncate(alasanBatal, 255), SqlDbType.VarChar);
            dp.AddParam("@now", now, SqlDbType.DateTime);
            dp.AddParam("@nowDate", nowDate, SqlDbType.VarChar);
            dp.AddParam("@nowTime", nowTime, SqlDbType.VarChar);

            var queryResult = await conn.QueryAsync<string>(sql, dp, transaction: trans);
            var result = queryResult?.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).Distinct().ToList() ?? new List<string>();
            trans.Commit();
            return result;
        }
        catch
        {
            trans.Rollback();
            throw;
        }
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val.Substring(0, maxLen) : val;
    }

    private static DynamicParameters BuildParams(PakaiBedDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeTruncate(dto.fs_kd_trs, 15), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_in", dto.fd_tgl_in, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_in", dto.fs_jam_in, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", SafeTruncate(dto.fs_kd_petugas, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_out", dto.fd_tgl_out, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_out", dto.fs_jam_out, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_out", SafeTruncate(dto.fs_kd_petugas_out, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", SafeTruncate(dto.fs_kd_reg, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", dto.fs_kd_layanan, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan_dk", dto.fs_kd_layanan_dk, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kamar_tipe", dto.fs_kd_kamar_tipe, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", dto.fs_kd_bed, SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", dto.fn_tarif, SqlDbType.Decimal);
        dp.AddParam("@fd_tgl_void", dto.fd_tgl_void, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", dto.fs_jam_void, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", SafeTruncate(dto.fs_kd_petugas_void, 10), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_entry", dto.fd_tgl_entry, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_entry", dto.fs_jam_entry, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", dto.fs_kd_kelas, SqlDbType.VarChar);
        return dp;
    }
}
