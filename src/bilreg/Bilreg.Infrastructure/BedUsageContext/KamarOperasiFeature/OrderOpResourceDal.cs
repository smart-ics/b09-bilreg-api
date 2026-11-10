using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpResourceDal
{
    void Insert(OrderOpResourceDto dto);
    void Update(OrderOpResourceDto dto);
    void Delete(string resourceId, string orderOpId);
    void DeleteAll(string orderOpId);
    void DeleteMissingFromTarget(string orderOpId, IEnumerable<string> keepResourceIds);
    OrderOpResourceDto? GetData(string resourceId, string orderOpId);
    OrderOpResourceDto? GetByIdAnyOwner(string resourceId);
    IEnumerable<OrderOpResourceDto> ListData(string orderOpId);
}

public interface IScheduleOpResourceDal : IOrderOpResourceDal
{
}

public class OrderOpResourceDal : IOrderOpResourceDal, IScheduleOpResourceDal
{
    private readonly DatabaseOptions _opt;

    public OrderOpResourceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderOpResourceDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OrderOpResource
            (
                OrderOpResourceId, OrderOpId, ScheduleOpId, ResourceType, Category,
                ItemName, Jumlah, Satuan, Keterangan,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            )
            VALUES
            (
                @OrderOpResourceId, @OrderOpId, @ScheduleOpId, @ResourceType, @Category,
                @ItemName, @Jumlah, @Satuan, @Keterangan,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", dto.OrderOpResourceId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ResourceType", dto.ResourceType, SqlDbType.Int);
        dp.AddParam("@Category", dto.Category ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ItemName", dto.ItemName ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Jumlah", dto.Jumlah, SqlDbType.Int);
        dp.AddParam("@Satuan", dto.Satuan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OrderOpResourceDto dto)
    {
        const string sql = """
            UPDATE BILRG_OrderOpResource
            SET
                ScheduleOpId = @ScheduleOpId,
                ResourceType = @ResourceType,
                Category = @Category,
                ItemName = @ItemName,
                Jumlah = @Jumlah,
                Satuan = @Satuan,
                Keterangan = @Keterangan,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate
            WHERE
                OrderOpResourceId = @OrderOpResourceId
                AND OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", dto.OrderOpResourceId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ResourceType", dto.ResourceType, SqlDbType.Int);
        dp.AddParam("@Category", dto.Category ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@ItemName", dto.ItemName ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Jumlah", dto.Jumlah, SqlDbType.Int);
        dp.AddParam("@Satuan", dto.Satuan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.Keterangan ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UpdUser", dto.UpdUser ?? string.Empty, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rowsAffected = conn.Execute(sql, dp);

        if (rowsAffected == 0)
        {
            var existing = GetByIdAnyOwner(dto.OrderOpResourceId);
            if (existing != null && existing.OrderOpId != dto.OrderOpId)
            {
                throw new InvalidOperationException(
                    $"Resource '{dto.OrderOpResourceId}' belongs to OrderOpId '{existing.OrderOpId}', not '{dto.OrderOpId}'. Cross-order update is rejected.");
            }
            throw new KeyNotFoundException(
                $"Resource '{dto.OrderOpResourceId}' for OrderOpId '{dto.OrderOpId}' was not found.");
        }
    }

    public void Delete(string resourceId, string orderOpId)
    {
        const string sql = """
            DELETE FROM BILRG_OrderOpResource
            WHERE
                OrderOpResourceId = @OrderOpResourceId
                AND OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", resourceId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rowsAffected = conn.Execute(sql, dp);

        if (rowsAffected == 0)
        {
            var existing = GetByIdAnyOwner(resourceId);
            if (existing != null && existing.OrderOpId != orderOpId)
            {
                throw new InvalidOperationException(
                    $"Resource '{resourceId}' belongs to OrderOpId '{existing.OrderOpId}', not '{orderOpId}'. Cross-order delete is rejected.");
            }
        }
    }

    public void DeleteAll(string orderOpId)
    {
        const string sql = """
            DELETE FROM BILRG_OrderOpResource
            WHERE
                OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void DeleteMissingFromTarget(string orderOpId, IEnumerable<string> keepResourceIds)
    {
        var keepList = keepResourceIds?.Where(x => !string.IsNullOrWhiteSpace(x)).ToList() ?? new List<string>();
        if (!keepList.Any())
        {
            DeleteAll(orderOpId);
            return;
        }

        const string sql = """
            DELETE FROM BILRG_OrderOpResource
            WHERE
                OrderOpId = @OrderOpId
                AND OrderOpResourceId NOT IN @KeepResourceIds
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);
        dp.Add("@KeepResourceIds", keepList);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OrderOpResourceDto? GetData(string resourceId, string orderOpId)
    {
        const string sql = """
            SELECT
                aa.OrderOpResourceId,
                aa.OrderOpResourceId AS Id,
                aa.OrderOpId,
                aa.ScheduleOpId,
                aa.ResourceType,
                aa.Category,
                aa.ItemName,
                aa.ItemName AS Name,
                aa.Jumlah,
                aa.Jumlah AS Qty,
                aa.Satuan,
                aa.Satuan AS Unit,
                aa.Keterangan,
                aa.Keterangan AS Notes,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_OrderOpResource aa
            WHERE
                aa.OrderOpResourceId = @OrderOpResourceId
                AND aa.OrderOpId = @OrderOpId
                AND aa.VodDate = '3000-01-01'
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", resourceId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderOpResourceDto>(sql, dp);
    }

    public OrderOpResourceDto? GetByIdAnyOwner(string resourceId)
    {
        const string sql = """
            SELECT
                aa.OrderOpResourceId,
                aa.OrderOpResourceId AS Id,
                aa.OrderOpId,
                aa.ScheduleOpId,
                aa.ResourceType,
                aa.Category,
                aa.ItemName,
                aa.ItemName AS Name,
                aa.Jumlah,
                aa.Jumlah AS Qty,
                aa.Satuan,
                aa.Satuan AS Unit,
                aa.Keterangan,
                aa.Keterangan AS Notes,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_OrderOpResource aa
            WHERE
                aa.OrderOpResourceId = @OrderOpResourceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", resourceId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderOpResourceDto>(sql, dp);
    }

    public IEnumerable<OrderOpResourceDto> ListData(string orderOpId)
    {
        const string sql = """
            SELECT
                aa.OrderOpResourceId,
                aa.OrderOpResourceId AS Id,
                aa.OrderOpId,
                aa.ScheduleOpId,
                aa.ResourceType,
                aa.Category,
                aa.ItemName,
                aa.ItemName AS Name,
                aa.Jumlah,
                aa.Jumlah AS Qty,
                aa.Satuan,
                aa.Satuan AS Unit,
                aa.Keterangan,
                aa.Keterangan AS Notes,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_OrderOpResource aa
            WHERE
                aa.OrderOpId = @OrderOpId
                AND aa.VodDate = '3000-01-01'
            ORDER BY
                aa.OrderOpResourceId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderOpResourceDto>(sql, dp) ?? Enumerable.Empty<OrderOpResourceDto>();
    }
}
