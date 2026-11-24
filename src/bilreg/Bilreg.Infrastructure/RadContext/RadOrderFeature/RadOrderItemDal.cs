using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.RadContext.RadOrderFeature;

public interface IRadOrderItemTableDal : IRadOrderItemDal
{
}

public class RadOrderItemDal : IRadOrderItemTableDal
{
    private readonly DatabaseOptions _opt;

    public RadOrderItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<RadOrderItemDto> ListData(IRadOrderKey key)
    {
        const string sql = """
            SELECT OrderId, ItemNo, TarifId, TarifName, Modality, Urgency
            FROM BILRG_RadOrderItem
            WHERE OrderId = @OrderId
            ORDER BY ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RadOrderItemDto>(sql, dp) ?? Enumerable.Empty<RadOrderItemDto>();
    }

    public void Insert(IEnumerable<RadOrderItemDto> items)
    {
        var fetched = items?.ToList() ?? [];
        if (fetched.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_RadOrderItem (
                OrderId, ItemNo, TarifId, TarifName, Modality, Urgency
            ) VALUES (
                @OrderId, @ItemNo, @TarifId, @TarifName, @Modality, @Urgency
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, fetched);
    }

    public void Delete(IRadOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_RadOrderItem
            WHERE OrderId = @OrderId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
