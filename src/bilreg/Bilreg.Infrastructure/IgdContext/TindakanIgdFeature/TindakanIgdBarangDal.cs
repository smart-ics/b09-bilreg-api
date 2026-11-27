using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public interface ITindakanIgdBarangDal :
    IInsertBulk<TindakanIgdBarangDto>,
    IDelete<ITindakanIgdKey>,
    IListData<TindakanIgdBarangDto, ITindakanIgdKey>
{
    void MarkVoid(ITindakanIgdKey key);
    void UpdateBilling(string tindakanIgdId, int noUrut, string trsBillingId);
}

public class TindakanIgdBarangDal : ITindakanIgdBarangDal
{
    private readonly DatabaseOptions _opt;

    public TindakanIgdBarangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<TindakanIgdBarangDto> listModel)
    {
        var list = listModel?.ToList() ?? [];
        if (list.Count == 0) return;

        const string sql = """
            INSERT INTO BILRG_TindakanIgdBarang (
                TindakanIgdId, NoUrut, BrgId, SatuanBomId, QtyBom,
                SatuanKecilId, QtyKecilRequested, QtyKecilDeducted, QtyKecilPending,
                IsChargeBarang, PakaiBrgId, TrsBillingId, IsVoid)
            VALUES (
                @TindakanIgdId, @NoUrut, @BrgId, @SatuanBomId, @QtyBom,
                @SatuanKecilId, @QtyKecilRequested, @QtyKecilDeducted, @QtyKecilPending,
                @IsChargeBarang, @PakaiBrgId, @TrsBillingId, @IsVoid)
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        foreach (var dto in list)
        {
            var dp = new DynamicParameters();
            dp.AddParam("@TindakanIgdId", dto.TindakanIgdId, SqlDbType.VarChar);
            dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
            dp.AddParam("@BrgId", dto.BrgId, SqlDbType.VarChar);
            dp.AddParam("@SatuanBomId", dto.SatuanBomId, SqlDbType.VarChar);
            dp.AddParam("@QtyBom", dto.QtyBom, SqlDbType.Decimal);
            dp.AddParam("@SatuanKecilId", dto.SatuanKecilId, SqlDbType.VarChar);
            dp.AddParam("@QtyKecilRequested", dto.QtyKecilRequested, SqlDbType.Decimal);
            dp.AddParam("@QtyKecilDeducted", dto.QtyKecilDeducted, SqlDbType.Decimal);
            dp.AddParam("@QtyKecilPending", dto.QtyKecilPending, SqlDbType.Decimal);
            dp.AddParam("@IsChargeBarang", dto.IsChargeBarang, SqlDbType.Bit);
            dp.AddParam("@PakaiBrgId", dto.PakaiBrgId ?? string.Empty, SqlDbType.VarChar);
            dp.AddParam("@TrsBillingId", dto.TrsBillingId ?? string.Empty, SqlDbType.VarChar);
            dp.AddParam("@IsVoid", dto.IsVoid, SqlDbType.Bit);
            conn.Execute(sql, dp);
        }
    }

    public void Delete(ITindakanIgdKey key)
    {
        const string sql = "DELETE BILRG_TindakanIgdBarang WHERE TindakanIgdId = @TindakanIgdId";
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", key.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TindakanIgdBarangDto> ListData(ITindakanIgdKey filter)
    {
        const string sql = """
            SELECT TindakanIgdId, NoUrut, BrgId, SatuanBomId, QtyBom,
                SatuanKecilId, QtyKecilRequested, QtyKecilDeducted, QtyKecilPending,
                IsChargeBarang, PakaiBrgId, TrsBillingId, IsVoid
            FROM BILRG_TindakanIgdBarang
            WHERE TindakanIgdId = @TindakanIgdId
            ORDER BY NoUrut
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", filter.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanIgdBarangDto>(sql, dp) ?? [];
    }

    public void MarkVoid(ITindakanIgdKey key)
    {
        const string sql = """
            UPDATE BILRG_TindakanIgdBarang
            SET IsVoid = 1
            WHERE TindakanIgdId = @TindakanIgdId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", key.TindakanIgdId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void UpdateBilling(string tindakanIgdId, int noUrut, string trsBillingId)
    {
        const string sql = """
            UPDATE BILRG_TindakanIgdBarang
            SET TrsBillingId = @TrsBillingId
            WHERE TindakanIgdId = @TindakanIgdId
              AND NoUrut = @NoUrut
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanIgdId", tindakanIgdId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);
        dp.AddParam("@TrsBillingId", trsBillingId ?? string.Empty, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
