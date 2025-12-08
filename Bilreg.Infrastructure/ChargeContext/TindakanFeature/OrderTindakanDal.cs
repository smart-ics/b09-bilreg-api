using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface IOrderTindakanDal :
    IInsert<OrderTindakanDto>,
    IUpdate<OrderTindakanDto>,
    IDelete<IOrderTindakanKey>,
    IGetData<OrderTindakanDto, IOrderTindakanKey>,
    IListData<OrderTindakanDto, ILayananKey> 
{
}

public class OrderTindakanDal : IOrderTindakanDal
{
    private readonly DatabaseOptions _opt;

    public OrderTindakanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderTindakanDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OrderTindakan(
                OrderId, OrderDate, PpaId, PpaName, 
                RegId, PasienId, PasienName, LayananId, LayananName, 
                StatusOrder, TarifId, TindakanName, CrtUser, CrtDate, 
                UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @OrderId, @OrderDate, @PpaId, @PpaName, 
                @RegId, @PasienId, @PasienName, @LayananId, @LayananName, 
                @StatusOrder, @TarifId, @TindakanName, @CrtUser, @CrtDate, 
                @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", dto.OrderDate, SqlDbType.DateTime);
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@StatusOrder", dto.StatusOrder, SqlDbType.Int);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TindakanName", dto.TindakanName, SqlDbType.VarChar);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OrderTindakanDto dto)
    {
        const string sql = """
           UPDATE 
                BILRG_OrderTindakan
           SET
              OrderDate = @OrderDate, 
              PpaId = @PpaId, 
              PpaName = @PpaName, 
              RegId = @RegId, 
              PasienId = @PasienId, 
              PasienName = @PasienName, 
              LayananId = @LayananId, 
              LayananName = @LayananName, 
              StatusOrder = @StatusOrder,
              TarifId = @TarifId, 
              TindakanName = @TindakanName,
              CrtUser = @CrtUser,
              CrtDate = @CrtDate,
              UpdUser = @UpdUser,
              UpdDate = @UpdDate,
              VodUser = @VodUser,
              VodDate = @VodDate
           WHERE
              OrderId = @OrderId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", dto.OrderDate, SqlDbType.DateTime); 
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@StatusOrder", dto.StatusOrder, SqlDbType.Int);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TindakanName", dto.TindakanName, SqlDbType.VarChar);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderTindakanKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_OrderTindakan
           WHERE
             OrderId = @OrderId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OrderTindakanDto GetData(IOrderTindakanKey key)
    {
        const string sql = """
           SELECT
               aa.OrderId, aa.OrderDate, aa.PpaId, aa.PpaName, 
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.StatusOrder, aa.TarifId, aa.TindakanName, aa.CrtUser, aa.CrtDate, 
               aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
           FROM
               BILRG_OrderTindakan aa
           WHERE
               aa.OrderId = @OrderId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderId", key.OrderId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderTindakanDto>(sql, dp);
    }

    public IEnumerable<OrderTindakanDto> ListData(ILayananKey filter)
    {
        const string sql = """
           SELECT
               aa.OrderId, aa.OrderDate, aa.PpaId, aa.PpaName, 
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.StatusOrder, aa.TarifId, aa.TindakanName, aa.CrtUser, aa.CrtDate, 
               aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
           FROM
               BILRG_OrderTindakan aa
           WHERE
               aa.LayananId = @LayananId 
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@LayananId", filter.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderTindakanDto>(sql, dp);
    }
}