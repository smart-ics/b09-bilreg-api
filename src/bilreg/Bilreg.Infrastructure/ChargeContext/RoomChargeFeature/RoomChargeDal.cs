using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeDal : IRoomChargeDal
{
    private readonly DatabaseOptions _opt;

    public RoomChargeDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    public void Insert(RoomChargeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        const string sql = """
            INSERT INTO ta_trs_roomcharge (
                fs_kd_trs, fs_kd_pakai_bed, fd_tgl_trs, fs_jam_trs,
                fs_kd_petugas, fs_kd_reg, fs_kd_layanan, fs_kd_bed,
                fn_tarif, fn_diskon, fn_total, fs_ket, fn_qty,
                fb_gen_nm_kelas, fs_kd_tipe_jaminan, fn_nilai_klaim, fs_kd_trs_dx)
            VALUES (
                @fs_kd_trs, @fs_kd_pakai_bed, @fd_tgl_trs, @fs_jam_trs,
                @fs_kd_petugas, @fs_kd_reg, @fs_kd_layanan, @fs_kd_bed,
                @fn_tarif, @fn_diskon, @fn_total, @fs_ket, @fn_qty,
                @fb_gen_nm_kelas, @fs_kd_tipe_jaminan, @fn_nilai_klaim, @fs_kd_trs_dx)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));

        if (string.Equals(dto.RoomChargeStatus, "Reversed", StringComparison.OrdinalIgnoreCase))
        {
            RecordVoidLog(conn, dto);
        }
    }

    public void Update(RoomChargeDto dto)
    {
        ArgumentNullException.ThrowIfNull(dto);
        const string sql = """
            UPDATE ta_trs_roomcharge
            SET
                fs_kd_pakai_bed = @fs_kd_pakai_bed,
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fs_kd_bed = @fs_kd_bed,
                fn_tarif = @fn_tarif,
                fn_diskon = @fn_diskon,
                fn_total = @fn_total,
                fs_ket = @fs_ket,
                fn_qty = @fn_qty,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan
            WHERE fs_kd_trs = @fs_kd_trs
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, MapParams(dto));

        if (string.Equals(dto.RoomChargeStatus, "Reversed", StringComparison.OrdinalIgnoreCase))
        {
            RecordVoidLog(conn, dto);
        }
    }

    public void Delete(IRoomChargeKey key, string userId, DateTime occurredAt)
    {
        ArgumentNullException.ThrowIfNull(key);

        var existing = GetData(key);
        if (existing is null)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        RecordVoidLog(conn, existing, userId, occurredAt);

        const string sqlDelete = """
            DELETE FROM ta_trs_roomcharge2 WHERE fs_kd_trs = @fs_kd_trs;
            DELETE FROM ta_trs_roomcharge WHERE fs_kd_trs = @fs_kd_trs;
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", SafeTruncate(key.RoomChargeId, 15), SqlDbType.VarChar);
        conn.Execute(sqlDelete, dp);
    }

    public RoomChargeDto GetData(IRoomChargeKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return GetData(key.RoomChargeId)!;
    }

    public RoomChargeDto? GetData(string roomChargeId)
    {
        var sql = $"""
            {SelectSql()}
            WHERE aa.fs_kd_trs = @RoomChargeId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@RoomChargeId", SafeTruncate(roomChargeId, 15), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RoomChargeDto>(sql, dp);
    }

    public RoomChargeDto? GetByPakaiBedAndDate(string pakaiBedId, string chargeCycleDate)
    {
        var sql = $"""
            {SelectSql()}
            WHERE aa.fs_kd_pakai_bed = @PakaiBedId
              AND aa.fd_tgl_trs = @ChargeCycleDate
              AND ISNULL(lg.fb_status_void, 0) = 0
              AND aa.fs_ket NOT LIKE 'Reversed%'
              AND aa.fs_ket NOT LIKE 'Void%'
            ORDER BY aa.fd_tgl_trs DESC, aa.fs_jam_trs DESC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", SafeTruncate(pakaiBedId, 12), SqlDbType.VarChar);
        dp.AddParam("@ChargeCycleDate", SafeTruncate(chargeCycleDate, 10), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<RoomChargeDto>(sql, dp);
        return list?.FirstOrDefault();
    }

    public IEnumerable<RoomChargeDto> ListDataByReg(string regId)
    {
        var sql = $"""
            {SelectSql()}
            WHERE aa.fs_kd_reg = @RegId
            ORDER BY aa.fd_tgl_trs ASC, aa.fs_jam_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", SafeTruncate(regId, 10), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp) ?? [];
    }

    public IEnumerable<RoomChargeDto> ListActiveByBangsalAndDate(string bangsalId, string chargeCycleDate)
    {
        var sql = $"""
            {SelectSql()}
            WHERE aa.fs_kd_layanan = @BangsalId
              AND aa.fd_tgl_trs = @ChargeCycleDate
              AND ISNULL(lg.fb_status_void, 0) = 0
              AND aa.fs_ket NOT LIKE 'Reversed%'
              AND aa.fs_ket NOT LIKE 'Void%'
            ORDER BY aa.fs_jam_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@BangsalId", SafeTruncate(bangsalId, 5), SqlDbType.VarChar);
        dp.AddParam("@ChargeCycleDate", SafeTruncate(chargeCycleDate, 10), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomChargeDto>(sql, dp) ?? [];
    }

    private static string SelectSql() => """
        SELECT 
            aa.fs_kd_trs AS RoomChargeId,
            aa.fs_kd_pakai_bed AS PakaiBedId,
            aa.fs_kd_reg AS RegId,
            ISNULL(reg.fs_mr, '') AS PasienId,
            aa.fs_kd_layanan AS BangsalId,
            ISNULL(lay.fs_nm_layanan, '') AS BangsalName,
            ISNULL(bed.fs_kd_kamar, '') AS KamarId,
            ISNULL(kmr.fs_nm_kamar, '') AS KamarName,
            aa.fs_kd_bed AS BedId,
            ISNULL(bed.fs_nm_bed, aa.fs_kd_bed) AS BedCode,
            ISNULL(CONVERT(VARCHAR(10), aa.fd_tgl_trs, 120), '') AS ChargeCycleDate,
            ISNULL(TRY_CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + CASE WHEN aa.fs_jam_trs = '' THEN '00:00:00' ELSE aa.fs_jam_trs END, 120), '1900-01-01') AS TimeCharge,
            'DailyCutOff' AS ChargeTriggerType,
            ISNULL(kmr.fs_kd_kelas, '') AS PhysicalClassId,
            ISNULL(kls_kmr.fs_nm_kelas, '') AS PhysicalClassName,
            ISNULL(reg.fs_kd_kelas, ISNULL(aa.fs_kd_tipe_jaminan, '')) AS PayerClassId,
            ISNULL(kls_reg.fs_nm_kelas, '') AS PayerClassName,
            COALESCE(NULLIF(tb.fs_kd_kelas, ''), NULLIF(kmr.fs_kd_kelas, ''), NULLIF(reg.fs_kd_kelas, ''), NULLIF(aa.fs_kd_tipe_jaminan, ''), '') AS BillingClassId,
            COALESCE(NULLIF(kls_tb.fs_nm_kelas, ''), NULLIF(kls_kmr.fs_nm_kelas, ''), NULLIF(kls_reg.fs_nm_kelas, ''), '') AS BillingClassName,
            CAST(0 AS BIT) AS IsTitipKelas,
            CAST(0 AS BIT) AS IsNaikKelas,
            CAST(0 AS DECIMAL(18,2)) AS DifferentialAmount,
            CAST(0 AS BIT) AS IsRoomingIn,
            '' AS MotherPakaiBedId,
            aa.fn_qty AS ChargedUnits,
            aa.fn_tarif AS BaseTariff,
            aa.fn_diskon AS DiscountAmount,
            aa.fn_total AS TotalAmount,
            CASE 
                WHEN ISNULL(lg.fb_status_void, 0) = 1 THEN 'Reversed'
                WHEN aa.fs_ket LIKE 'Reversed%' OR aa.fs_ket LIKE 'Void%' OR aa.fs_ket LIKE '%Void%' THEN 'Reversed'
                ELSE 'Posted'
            END AS RoomChargeStatus,
            '' AS BillingItemRefId,
            '' AS ReversalOfRoomChargeId,
            ISNULL(aa.fs_ket, '') AS ReversalReason,
            ISNULL(aa.fs_kd_petugas, '') AS CreatedBy,
            ISNULL(TRY_CONVERT(DATETIME, aa.fd_tgl_trs + ' ' + CASE WHEN aa.fs_jam_trs = '' THEN '00:00:00' ELSE aa.fs_jam_trs END, 120), '1900-01-01') AS CreatedAt,
            ISNULL(lg.fs_kd_upd, '') AS UpdatedBy,
            ISNULL(TRY_CONVERT(DATETIME, lg.fd_tgl_upd + ' ' + CASE WHEN lg.fs_jam_upd = '' THEN '00:00:00' ELSE lg.fs_jam_upd END, 120), '1900-01-01') AS UpdatedAt
        FROM ta_trs_roomcharge aa
        LEFT JOIN ta_trs_bed tb ON aa.fs_kd_pakai_bed = tb.fs_kd_trs
        LEFT JOIN ta_registrasi reg ON aa.fs_kd_reg = reg.fs_kd_reg
        LEFT JOIN ta_bed bed ON aa.fs_kd_bed = bed.fs_kd_bed
        LEFT JOIN ta_kamar kmr ON bed.fs_kd_kamar = kmr.fs_kd_kamar
        LEFT JOIN ta_layanan lay ON aa.fs_kd_layanan = lay.fs_kd_layanan
        LEFT JOIN ta_kelas kls_tb ON tb.fs_kd_kelas = kls_tb.fs_kd_kelas
        LEFT JOIN ta_kelas kls_kmr ON kmr.fs_kd_kelas = kls_kmr.fs_kd_kelas
        LEFT JOIN ta_kelas kls_reg ON reg.fs_kd_kelas = kls_reg.fs_kd_kelas
        LEFT JOIN (
            SELECT fs_kd_trs, 
                   MAX(CASE WHEN fb_status_void = 1 THEN 1 ELSE 0 END) AS fb_status_void,
                   MAX(fd_tgl_upd) AS fd_tgl_upd,
                   MAX(fs_jam_upd) AS fs_jam_upd,
                   MAX(fs_kd_upd) AS fs_kd_upd
            FROM ta_trs_roomcharge_log
            GROUP BY fs_kd_trs
        ) lg ON aa.fs_kd_trs = lg.fs_kd_trs
        """;

    private static DynamicParameters MapParams(RoomChargeDto dto)
    {
        var trsId = SafeTruncate(dto.RoomChargeId, 15);
        var tglTrs = string.IsNullOrWhiteSpace(dto.ChargeCycleDate)
            ? (dto.TimeCharge == default ? DateTime.Now : dto.TimeCharge).ToString("yyyy-MM-dd")
            : SafeTruncate(dto.ChargeCycleDate, 10);
        var jamTrs = dto.TimeCharge == default ? "00:00:00" : dto.TimeCharge.ToString("HH:mm:ss");

        var isReversed = string.Equals(dto.RoomChargeStatus, "Reversed", StringComparison.OrdinalIgnoreCase);
        var ket = isReversed
            ? (string.IsNullOrWhiteSpace(dto.ReversalReason) ? "Reversed" : $"Reversed: {dto.ReversalReason}")
            : (string.IsNullOrWhiteSpace(dto.ReversalReason) ? (dto.RoomChargeStatus ?? "Posted") : dto.ReversalReason);

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_pakai_bed", SafeTruncate(dto.PakaiBedId, 12), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", tglTrs, SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", SafeTruncate(jamTrs, 8), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", SafeTruncate(dto.CreatedBy, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", SafeTruncate(dto.BangsalId, 5), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_bed", SafeTruncate(dto.BedId, 8), SqlDbType.VarChar);
        dp.AddParam("@fn_tarif", dto.BaseTariff, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.DiscountAmount, SqlDbType.Decimal);
        dp.AddParam("@fn_total", dto.TotalAmount, SqlDbType.Decimal);
        dp.AddParam("@fs_ket", SafeTruncate(ket, 40), SqlDbType.VarChar);
        dp.AddParam("@fn_qty", dto.ChargedUnits, SqlDbType.Decimal);
        dp.AddParam("@fb_gen_nm_kelas", true, SqlDbType.Bit);
        dp.AddParam("@fs_kd_tipe_jaminan", SafeTruncate(dto.PayerClassId, 5), SqlDbType.VarChar);
        dp.AddParam("@fn_nilai_klaim", 0m, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_trs_dx", string.Empty, SqlDbType.VarChar);

        return dp;
    }

    private static void RecordVoidLog(SqlConnection conn, RoomChargeDto dto, string? userId = null, DateTime? occurredAt = null)
    {
        try
        {
            const string insertLogSql = """
                INSERT INTO ta_trs_roomcharge_log (
                    fs_kd_trs, fd_tgl_trs, fd_tgl_upd, fs_jam_upd,
                    fs_kd_petugas, fs_kd_upd, fs_kd_reg,
                    fs_kd_layanan, fs_kd_bed,
                    fn_tarif, fn_qty, fb_status_void)
                VALUES (
                    @fs_kd_trs, @fd_tgl_trs, @fd_tgl_upd, @fs_jam_upd,
                    @fs_kd_petugas, @fs_kd_upd, @fs_kd_reg,
                    @fs_kd_layanan, @fs_kd_bed,
                    @fn_tarif, @fn_qty, @fb_status_void)
                """;

            var trsId = SafeTruncate(dto.RoomChargeId, 15);
            var tglTrs = string.IsNullOrWhiteSpace(dto.ChargeCycleDate)
                ? (dto.TimeCharge == default ? DateTime.Now : dto.TimeCharge).ToString("yyyy-MM-dd")
                : SafeTruncate(dto.ChargeCycleDate, 10);
            var updTime = occurredAt ?? ((dto.UpdatedAt == default || dto.UpdatedAt.Year < 1900)
                ? DateTime.Now
                : dto.UpdatedAt);
            var updUser = !string.IsNullOrWhiteSpace(userId)
                ? userId
                : (string.IsNullOrWhiteSpace(dto.UpdatedBy)
                    ? (string.IsNullOrWhiteSpace(dto.CreatedBy) ? "SYSTEM" : dto.CreatedBy)
                    : dto.UpdatedBy);

            var dpLog = new DynamicParameters();
            dpLog.AddParam("@fs_kd_trs", trsId, SqlDbType.VarChar);
            dpLog.AddParam("@fd_tgl_trs", tglTrs, SqlDbType.VarChar);
            dpLog.AddParam("@fd_tgl_upd", updTime.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
            dpLog.AddParam("@fs_jam_upd", updTime.ToString("HH:mm:ss"), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_petugas", SafeTruncate(dto.CreatedBy, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_upd", SafeTruncate(updUser, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_reg", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_layanan", SafeTruncate(dto.BangsalId, 5), SqlDbType.VarChar);
            dpLog.AddParam("@fs_kd_bed", SafeTruncate(dto.BedId, 8), SqlDbType.VarChar);
            dpLog.AddParam("@fn_tarif", dto.BaseTariff, SqlDbType.Decimal);
            dpLog.AddParam("@fn_qty", dto.ChargedUnits, SqlDbType.Decimal);
            dpLog.AddParam("@fb_status_void", true, SqlDbType.Bit);
            conn.Execute(insertLogSql, dpLog);
        }
        catch
        {
            // Avoid failing primary flow if log row already exists
        }
    }
}
