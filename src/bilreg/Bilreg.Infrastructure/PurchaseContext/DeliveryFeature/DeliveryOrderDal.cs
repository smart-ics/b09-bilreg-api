using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PurchaseContext.DeliveryFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PurchaseContext.DeliveryFeature;

public interface IDeliveryOrderDal :
    IInsert<DeliveryOrderDto>,
    IUpdate<DeliveryOrderDto>,
    IDelete<IDeliveryOrderKey>,
    IGetData<DeliveryOrderDto, IDeliveryOrderKey>
{
}

public class DeliveryOrderDal : IDeliveryOrderDal
{
    private readonly DatabaseOptions _opt;

    public DeliveryOrderDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(DeliveryOrderDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_DeliveryOrder (
                DeliveryOrderId, DoNo,
                SupplierId, SupplierName, PoReffId,
                DoDate, State, Notes,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @DeliveryOrderId, @DoNo,
                @SupplierId, @SupplierName, @PoReffId,
                @DoDate, @State, @Notes,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var dp = BuildParam(dto);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(DeliveryOrderDto dto)
    {
        const string sql = """
            UPDATE BILRG_DeliveryOrder SET
                SupplierId = @SupplierId,
                SupplierName = @SupplierName,
                PoReffId = @PoReffId,
                DoDate = @DoDate,
                State = @State,
                Notes = @Notes,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE
                DeliveryOrderId = @DeliveryOrderId
            """;
        var dp = BuildParam(dto);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IDeliveryOrderKey key)
    {
        const string sql = """
            DELETE FROM BILRG_DeliveryOrder
            WHERE DeliveryOrderId = @DeliveryOrderId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@DeliveryOrderId", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public DeliveryOrderDto GetData(IDeliveryOrderKey key)
    {
        const string sql = """
            SELECT
                aa.DeliveryOrderId,
                aa.DoNo,
                aa.SupplierId,
                aa.SupplierName,
                aa.PoReffId,
                aa.DoDate,
                aa.State,
                aa.Notes,
                aa.CrtUser,
                aa.CrtDate,
                aa.UpdUser,
                aa.UpdDate,
                aa.VodUser,
                aa.VodDate
            FROM
                BILRG_DeliveryOrder aa
            WHERE
                aa.DeliveryOrderId = @DeliveryOrderId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@DeliveryOrderId", key.DeliveryOrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<DeliveryOrderDto>(sql, dp);
    }

    private static DynamicParameters BuildParam(DeliveryOrderDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@DeliveryOrderId", dto.DeliveryOrderId, SqlDbType.VarChar);
        dp.AddParam("@DoNo", dto.DoNo, SqlDbType.VarChar);
        dp.AddParam("@SupplierId", dto.SupplierId, SqlDbType.VarChar);
        dp.AddParam("@SupplierName", dto.SupplierName, SqlDbType.VarChar);
        dp.AddParam("@PoReffId", dto.PoReffId, SqlDbType.VarChar);
        dp.AddParam("@DoDate", dto.DoDate, SqlDbType.DateTime);
        dp.AddParam("@State", (int)dto.State, SqlDbType.Int);
        dp.AddParam("@Notes", dto.Notes, SqlDbType.VarChar);
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        return dp;
    }
}
