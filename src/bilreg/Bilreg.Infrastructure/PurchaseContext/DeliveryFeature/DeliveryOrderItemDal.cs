using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderItemDal :
    IInsertBulk<DeliveryOrderItemDto>,
    IDelete<IDeliveryOrderKey>,
    IListData<DeliveryOrderItemDto, IDeliveryOrderKey>
{
}

public class DeliveryOrderItemDal : IDeliveryOrderItemDal
{
    private readonly DatabaseOptions _opt;

    public DeliveryOrderItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<DeliveryOrderItemDto> listModel)
    {
        var list = listModel.ToList();
        if (list.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();

        bcp.AddMap("DeliveryOrderId", "DeliveryOrderId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("BrgId", "BrgId");
        bcp.AddMap("LayananId", "LayananId");
        bcp.AddMap("QtyOrder", "QtyOrder");
        bcp.AddMap("QtyReceived", "QtyReceived");
        bcp.AddMap("SatuanId", "SatuanId");
        bcp.AddMap("Harga", "Harga");
        bcp.AddMap("Diskon", "Diskon");
        bcp.AddMap("Tax", "Tax");
        bcp.AddMap("TglEd", "TglEd");
        bcp.AddMap("NoBatch", "NoBatch");
        bcp.AddMap("State", "State");
        bcp.AddMap("CrtUser", "CrtUser");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("UpdUser", "UpdUser");
        bcp.AddMap("UpdDate", "UpdDate");
        bcp.AddMap("VodUser", "VodUser");
        bcp.AddMap("VodDate", "VodDate");

        bcp.BatchSize = list.Count;
        bcp.DestinationTableName = "BILRG_DeliveryOrderItem";
        bcp.WriteToServer(list.AsDataTable());
    }

    public void Delete(IDeliveryOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_DeliveryOrderItem
            WHERE DeliveryOrderId = @DeliveryOrderId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@DeliveryOrderId", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<DeliveryOrderItemDto> ListData(IDeliveryOrderKey filter)
    {
        const string sql = """
            SELECT
                aa.DeliveryOrderId,
                aa.ItemNo,
                aa.BrgId,
                aa.LayananId,
                aa.QtyOrder,
                aa.QtyReceived,
                aa.SatuanId,
                aa.Harga,
                aa.Diskon,
                aa.Tax,
                aa.TglEd,
                aa.NoBatch,
                aa.State,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_DeliveryOrderItem aa
            WHERE
                aa.DeliveryOrderId = @DeliveryOrderId
            ORDER BY
                aa.ItemNo
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@DeliveryOrderId", filter.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<DeliveryOrderItemDto>(sql, dp);
    }
}
