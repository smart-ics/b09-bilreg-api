using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public record BillingLockStatusRaw(
    bool HasRoomCharge = false,
    bool HasCashierPayment = false,
    bool IsTataRekeningLocked = false
);

public interface IPakaiBedBillingLockCheckerDal
{
    Task<BillingLockStatusRaw> CheckBillingLockRawAsync(
        string pakaiBedId,
        string regId,
        CancellationToken cancellationToken = default);
}

public class PakaiBedBillingLockCheckerDal : IPakaiBedBillingLockCheckerDal
{
    public const string CheckBillingLockSql = """
        SELECT
            -- 1. ta_trs_roomcharge / BILRG_RoomCharge (keterikatan sewa kamar pada alokasi bed)
            CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM ta_trs_roomcharge rc
                WHERE rc.fs_kd_pakai_bed = @PakaiBedId
            ) THEN 1 ELSE 0 END AS BIT) AS HasRoomCharge,

            -- 2. ta_trs_billing2 (transaksi pembayaran kasir)
            CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM ta_trs_billing2 b2
                INNER JOIN ta_trs_billing b ON b2.fs_kd_trs = b.fs_kd_trs
                WHERE b.fs_kd_reg = @RegId
                  AND (b2.fs_kd_trs_bayar <> '' OR b2.fn_trs_p > 0)
            ) THEN 1 ELSE 0 END AS BIT) AS HasCashierPayment,

            -- 3. BILRG_TataRekening (status akun billing terkunci/final/lunas)
            CAST(CASE WHEN EXISTS (
                SELECT 1
                FROM BILRG_TataRekening tr
                WHERE tr.RegId = @RegId
                  AND (tr.Status <> 0 OR tr.FinVerifStatus <> 0 OR tr.SettlementInitiated = 1)
            ) THEN 1 ELSE 0 END AS BIT) AS IsTataRekeningLocked
        """;

    private readonly DatabaseOptions _opt;

    public PakaiBedBillingLockCheckerDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public async Task<BillingLockStatusRaw> CheckBillingLockRawAsync(
        string pakaiBedId,
        string regId,
        CancellationToken cancellationToken = default)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", pakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@RegId", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        await conn.OpenAsync(cancellationToken);
        var cmd = new CommandDefinition(CheckBillingLockSql, dp, cancellationToken: cancellationToken);
        var result = await conn.QueryFirstOrDefaultAsync<BillingLockStatusRaw>(cmd);
        return result ?? new BillingLockStatusRaw();
    }
}

public class PakaiBedBillingLockChecker : IPakaiBedBillingLockChecker
{
    private readonly IPakaiBedBillingLockCheckerDal _dal;

    public PakaiBedBillingLockChecker(IPakaiBedBillingLockCheckerDal dal)
    {
        _dal = dal ?? throw new ArgumentNullException(nameof(dal));
    }

    public async Task<BillingLockCheckResult> CheckLockStatusAsync(
        string pakaiBedId,
        string regId,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(pakaiBedId);
        ArgumentException.ThrowIfNullOrWhiteSpace(regId);

        try
        {
            var raw = await _dal.CheckBillingLockRawAsync(pakaiBedId, regId, cancellationToken);

            if (raw.HasRoomCharge)
            {
                return BillingLockCheckResult.Locked(
                    $"Terdapat transaksi sewa kamar aktif pada ta_trs_roomcharge untuk penempatan tempat tidur {pakaiBedId}.");
            }

            if (raw.HasCashierPayment)
            {
                return BillingLockCheckResult.Locked(
                    $"Terdapat transaksi pembayaran kasir pada ta_trs_billing2 untuk registrasi {regId}.");
            }

            if (raw.IsTataRekeningLocked)
            {
                return BillingLockCheckResult.Locked(
                    $"Status tata rekening pada BILRG_TataRekening untuk registrasi {regId} sudah terkunci atau dalam proses penyelesaian.");
            }

            return BillingLockCheckResult.Unlocked();
        }
        catch (Exception ex) when (ex is not ArgumentException && ex is not OperationCanceledException)
        {
            // Fail-Closed principle: If check fails due to infrastructure/DB error, lock the operation
            return BillingLockCheckResult.Locked(
                $"Fail-Closed: Gagal memverifikasi status billing akibat kendala sistem ({ex.Message}).");
        }
    }
}
