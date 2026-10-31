using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpResourceDal :
    IInsert<OrderOpResourceDto>,
    IInsertBulk<OrderOpResourceDto>,
    IDelete<IOrderOpKey>,
    IListData<OrderOpResourceDto, IOrderOpKey>
{
    void Insert(OrderOpResourceDto dto);
    void DeleteByOrderOpId(string orderOpId);
    IEnumerable<OrderOpResourceDto> ListByOrderOpId(string orderOpId);
}

public class OrderOpResourceDal : IOrderOpResourceDal
{
    private readonly DatabaseOptions _opt;

    public OrderOpResourceDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderOpResourceDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OrderOpResource(
                OrderOpResourceId, OrderOpId, ResourceType, ItemName, Jumlah, Satuan, Keterangan)
            VALUES(
                @OrderOpResourceId, @OrderOpId, @ResourceType, @ItemName, @Jumlah, @Satuan, @Keterangan)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpResourceId", dto.OrderOpResourceId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@ResourceType", dto.ResourceType, SqlDbType.Int);
        dp.AddParam("@ItemName", dto.ItemName, SqlDbType.VarChar);
        dp.AddParam("@Jumlah", dto.Jumlah, SqlDbType.Int);
        dp.AddParam("@Satuan", dto.Satuan, SqlDbType.VarChar);
        dp.AddParam("@Keterangan", dto.Keterangan, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<OrderOpResourceDto> listDto)
    {
        var fetched = listDto.ToList();
        if (fetched.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("OrderOpResourceId", "OrderOpResourceId");
        bcp.AddMap("OrderOpId", "OrderOpId");
        bcp.AddMap("ResourceType", "ResourceType");
        bcp.AddMap("ItemName", "ItemName");
        bcp.AddMap("Jumlah", "Jumlah");
        bcp.AddMap("Satuan", "Satuan");
        bcp.AddMap("Keterangan", "Keterangan");

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_OrderOpResource";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void DeleteByOrderOpId(string orderOpId)
    {
        const string sql = """
            DELETE FROM BILRG_OrderOpResource
            WHERE OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderOpKey key)
    {
        DeleteByOrderOpId(key.OrderOpId);
    }

    public IEnumerable<OrderOpResourceDto> ListByOrderOpId(string orderOpId)
    {
        const string sql = """
            SELECT
                OrderOpResourceId, OrderOpId, ResourceType, ItemName, Jumlah, Satuan, Keterangan
            FROM
                BILRG_OrderOpResource
            WHERE
                OrderOpId = @OrderOpId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", orderOpId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderOpResourceDto>(sql, dp) ?? [];
    }

    public IEnumerable<OrderOpResourceDto> ListData(IOrderOpKey filter)
    {
        return ListByOrderOpId(filter.OrderOpId);
    }
}
