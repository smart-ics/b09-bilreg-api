using System.Data;
using System.Data.SqlClient;
using System.Text;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.InventoryContext.PakaiBrgFeature;

public interface ITindakanStokPendingDal :
    IInsert<TindakanStokPendingDto>,
    IInsertBulk<TindakanStokPendingDto>
{
    TindakanStokPendingDto? GetData(string pendingId);
    IEnumerable<TindakanStokPendingDto> ListByTindakanId(string tindakanId);
    IEnumerable<TindakanStokPendingDto> ListFifoAktif(string brgId, string layananId);
    void MarkSettled(string pendingId, string pakaiBrgId, decimal qtySettled, DateTime settledDate, string updUser = "");
    void MarkVoid(string pendingId, DateTime voidDate, string updUser = "");
    void MarkVoidByTindakanId(string tindakanId, DateTime voidDate, string updUser = "");
}

public class TindakanStokPendingDal : ITindakanStokPendingDal
{
    private static readonly DateTime DefaultFutureDate = new(3000, 1, 1);
    private readonly DatabaseOptions _opt;

    public TindakanStokPendingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static string SafeTruncate(string? val, int maxLen)
    {
        if (string.IsNullOrWhiteSpace(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }

    private static DynamicParameters CreateParameters(TindakanStokPendingDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PendingId", SafeTruncate(dto.PendingId, 26), SqlDbType.VarChar);
        dp.AddParam("@TindakanId", SafeTruncate(dto.TindakanId, 26), SqlDbType.VarChar);
        dp.AddParam("@TindakanBarangId", SafeTruncate(dto.TindakanBarangId, 26), SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@RegId", SafeTruncate(dto.RegId, 10), SqlDbType.VarChar);
        dp.AddParam("@LayananId", SafeTruncate(dto.LayananId, 5), SqlDbType.VarChar);
        dp.AddParam("@BrgId", SafeTruncate(dto.BrgId, 13), SqlDbType.VarChar);
        dp.AddParam("@SatuanKecilId", SafeTruncate(dto.SatuanKecilId, 5), SqlDbType.VarChar);
        dp.AddParam("@QtyPending", dto.QtyPending, SqlDbType.Decimal);
        dp.AddParam("@QtySettled", dto.QtySettled, SqlDbType.Decimal);
        dp.AddParam("@StatusPotStok", dto.StatusPotStok, SqlDbType.Bit);
        dp.AddParam("@PakaiBrgId", SafeTruncate(dto.PakaiBrgId, 13), SqlDbType.VarChar);
        dp.AddParam("@IsVoid", dto.IsVoid, SqlDbType.Bit);
        dp.AddParam("@SettledDate", dto.SettledDate == default ? DefaultFutureDate : dto.SettledDate, SqlDbType.DateTime);
        dp.AddParam("@VoidDate", dto.VoidDate == default ? DefaultFutureDate : dto.VoidDate, SqlDbType.DateTime);
        dp.AddParam("@CrtUser", SafeTruncate(dto.CrtUser, 50), SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate == default ? DateTime.Now : dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", SafeTruncate(dto.UpdUser, 50), SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate == default ? DefaultFutureDate : dto.UpdDate, SqlDbType.DateTime);
        return dp;
    }

    public void Insert(TindakanStokPendingDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_TindakanStokPending (
                PendingId, TindakanId, TindakanBarangId, NoUrut, RegId, LayananId, BrgId,
                SatuanKecilId, QtyPending, QtySettled, StatusPotStok, PakaiBrgId, IsVoid,
                SettledDate, VoidDate, CrtUser, CrtDate, UpdUser, UpdDate
            )
            VALUES (
                @PendingId, @TindakanId, @TindakanBarangId, @NoUrut, @RegId, @LayananId, @BrgId,
                @SatuanKecilId, @QtyPending, @QtySettled, @StatusPotStok, @PakaiBrgId, @IsVoid,
                @SettledDate, @VoidDate, @CrtUser, @CrtDate, @UpdUser, @UpdDate
            )
            """;

        var dp = CreateParameters(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<TindakanStokPendingDto> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_TindakanStokPending (
                PendingId, TindakanId, TindakanBarangId, NoUrut, RegId, LayananId, BrgId,
                SatuanKecilId, QtyPending, QtySettled, StatusPotStok, PakaiBrgId, IsVoid,
                SettledDate, VoidDate, CrtUser, CrtDate, UpdUser, UpdDate
            )
            VALUES (
                @PendingId, @TindakanId, @TindakanBarangId, @NoUrut, @RegId, @LayananId, @BrgId,
                @SatuanKecilId, @QtyPending, @QtySettled, @StatusPotStok, @PakaiBrgId, @IsVoid,
                @SettledDate, @VoidDate, @CrtUser, @CrtDate, @UpdUser, @UpdDate
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var dto in list)
        {
            var dp = CreateParameters(dto);
            conn.Execute(sql, dp);
        }
    }

    public TindakanStokPendingDto? GetData(string pendingId)
    {
        const string sql = """
            SELECT 
                PendingId, TindakanId, TindakanBarangId, NoUrut, RegId, LayananId, BrgId,
                SatuanKecilId, QtyPending, QtySettled, StatusPotStok, PakaiBrgId, IsVoid,
                SettledDate, VoidDate, CrtUser, CrtDate, UpdUser, UpdDate
            FROM 
                BILRG_TindakanStokPending
            WHERE 
                PendingId = @PendingId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PendingId", pendingId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanStokPendingDto>(sql, dp);
    }

    public IEnumerable<TindakanStokPendingDto> ListByTindakanId(string tindakanId)
    {
        const string sql = """
            SELECT 
                PendingId, TindakanId, TindakanBarangId, NoUrut, RegId, LayananId, BrgId,
                SatuanKecilId, QtyPending, QtySettled, StatusPotStok, PakaiBrgId, IsVoid,
                SettledDate, VoidDate, CrtUser, CrtDate, UpdUser, UpdDate
            FROM 
                BILRG_TindakanStokPending
            WHERE 
                TindakanId = @TindakanId
            ORDER BY 
                NoUrut ASC, PendingId ASC
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", tindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanStokPendingDto>(sql, dp) ?? [];
    }

    public IEnumerable<TindakanStokPendingDto> ListFifoAktif(string brgId, string layananId)
    {
        var sb = new StringBuilder("""
            SELECT 
                PendingId, TindakanId, TindakanBarangId, NoUrut, RegId, LayananId, BrgId,
                SatuanKecilId, QtyPending, QtySettled, StatusPotStok, PakaiBrgId, IsVoid,
                SettledDate, VoidDate, CrtUser, CrtDate, UpdUser, UpdDate
            FROM 
                BILRG_TindakanStokPending
            WHERE 
                StatusPotStok = 0 
                AND IsVoid = 0
            """);

        var dp = new DynamicParameters();
        if (!string.IsNullOrWhiteSpace(brgId))
        {
            sb.Append(" AND BrgId = @BrgId");
            dp.AddParam("@BrgId", brgId, SqlDbType.VarChar);
        }
        if (!string.IsNullOrWhiteSpace(layananId))
        {
            sb.Append(" AND LayananId = @LayananId");
            dp.AddParam("@LayananId", layananId, SqlDbType.VarChar);
        }

        sb.Append(" ORDER BY CrtDate ASC, PendingId ASC");

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanStokPendingDto>(sb.ToString(), dp) ?? [];
    }

    public void MarkSettled(string pendingId, string pakaiBrgId, decimal qtySettled, DateTime settledDate, string updUser = "")
    {
        const string sql = """
            UPDATE BILRG_TindakanStokPending
            SET 
                StatusPotStok = 1,
                PakaiBrgId = @PakaiBrgId,
                QtySettled = CASE WHEN @QtySettled > 0 THEN @QtySettled ELSE QtyPending END,
                SettledDate = @SettledDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE 
                PendingId = @PendingId
            """;

        var effectiveSettledDate = settledDate == default ? DateTime.Now : settledDate;
        var dp = new DynamicParameters();
        dp.AddParam("@PendingId", pendingId, SqlDbType.VarChar);
        dp.AddParam("@PakaiBrgId", pakaiBrgId, SqlDbType.VarChar);
        dp.AddParam("@QtySettled", qtySettled, SqlDbType.Decimal);
        dp.AddParam("@SettledDate", effectiveSettledDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", updUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", DateTime.Now, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void MarkVoid(string pendingId, DateTime voidDate, string updUser = "")
    {
        const string sql = """
            UPDATE BILRG_TindakanStokPending
            SET 
                IsVoid = 1,
                VoidDate = @VoidDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE 
                PendingId = @PendingId
            """;

        var effectiveVoidDate = voidDate == default ? DateTime.Now : voidDate;
        var dp = new DynamicParameters();
        dp.AddParam("@PendingId", pendingId, SqlDbType.VarChar);
        dp.AddParam("@VoidDate", effectiveVoidDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", updUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", DateTime.Now, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void MarkVoidByTindakanId(string tindakanId, DateTime voidDate, string updUser = "")
    {
        const string sql = """
            UPDATE BILRG_TindakanStokPending
            SET 
                IsVoid = 1,
                VoidDate = @VoidDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE 
                TindakanId = @TindakanId
                AND IsVoid = 0
            """;

        var effectiveVoidDate = voidDate == default ? DateTime.Now : voidDate;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", tindakanId, SqlDbType.VarChar);
        dp.AddParam("@VoidDate", effectiveVoidDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", updUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", DateTime.Now, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
