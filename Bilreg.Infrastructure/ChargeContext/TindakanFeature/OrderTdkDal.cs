using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface IOrderTdkDal :
    IInsert<OrderTdkDto>,
    IUpdate<OrderTdkDto>,
    IDelete<IOrderTdkKey>,
    IGetData<OrderTdkDto, IOrderTdkKey>,
    IListData<OrderTdkDto, IPasienKey> 
{
}

public class OrderTdkDal : IOrderTdkDal
{
    private readonly DatabaseOptions _opt;

    public OrderTdkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(OrderTdkDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OrderTdk(
                OrderTdkId, OrderTdkDate, PasienId, PasienName, 
                RegId, PpaId, PpaName, LayananId, LayananName, 
                TarifId, TarifName, FreeTextOrder, StatusOrder,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @OrderTdkId, @OrderTdkDate, @PasienId, @PasienName, 
                @RegId, @PpaId, @PpaName, @LayananId, @LayananName, 
                @TarifId, @TarifName, @FreeTextOrder, @StatusOrder,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);
        dp.AddParam("@OrderTdkDate", dto.OrderTdkDate, SqlDbType.DateTime);

        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@FreeTextOrder", dto.FreeTextOrder, SqlDbType.VarChar);
        
        dp.AddParam("@StatusOrder", dto.StatusOrder, SqlDbType.Int);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OrderTdkDto dto)
    {
        const string sql = """
           UPDATE 
                BILRG_OrderTdk
           SET
                OrderTdkDate = @OrderTdkDate, 
                PasienId = @PasienId, 
                PasienName = @PasienName, 
                RegId = @RegId, 
                PpaId = @PpaId, 
                PpaName = @PpaName, 
                LayananId = @LayananId, 
                LayananName = @LayananName, 
                TarifId = @TarifId, 
                TarifName = @TarifName,
                FreeTextOrder = @FreeTextOrder,
                StatusOrder = @StatusOrder,
                CrtUser = @CrtUser,
                CrtDate = @CrtDate,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
           WHERE
                OrderTdkId = @OrderTdkId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);
        dp.AddParam("@OrderTdkDate", dto.OrderTdkDate, SqlDbType.DateTime);

        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        
        dp.AddParam("@PpaId", dto.PpaId, SqlDbType.VarChar);
        dp.AddParam("@PpaName", dto.PpaName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@FreeTextOrder", dto.FreeTextOrder, SqlDbType.VarChar);
        
        dp.AddParam("@StatusOrder", dto.StatusOrder, SqlDbType.Int);

        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderTdkKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_OrderTdk
           WHERE
             OrderTdkId = @OrderTdkId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderTdkId", key.OrderTdkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OrderTdkDto GetData(IOrderTdkKey key)
    {
        const string sql = """
           SELECT
               aa.OrderTdkId, aa.OrderTdkDate, 
               aa.PasienId, aa.PasienName, aa.RegId,  
               aa.PpaId, aa.PpaName, 
               aa.LayananId, aa.LayananName, 
               aa.TarifId, aa.TarifName, aa.FreeTextOrder, aa.StatusOrder,  
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fd_tgl_lahir, '') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender
           FROM
               BILRG_OrderTdk aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
           WHERE
               aa.OrderTdkId = @OrderTdkId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@OrderTdkId", key.OrderTdkId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<OrderTdkDto>(sql, dp);
    }

    public IEnumerable<OrderTdkDto> ListData(IPasienKey filter)
    {
        const string sql = """
           SELECT
               aa.OrderTdkId, aa.OrderTdkDate, 
               aa.PasienId, aa.PasienName, aa.RegId,  
               aa.PpaId, aa.PpaName, 
               aa.LayananId, aa.LayananName, 
               aa.TarifId, aa.TarifName, aa.FreeTextOrder, aa.StatusOrder,  
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.fd_tgl_lahir, '') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender
           FROM
               BILRG_OrderTdk aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
           WHERE
               aa.PasienId = @PasienId 
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrderTdkDto>(sql, dp);
    }
}