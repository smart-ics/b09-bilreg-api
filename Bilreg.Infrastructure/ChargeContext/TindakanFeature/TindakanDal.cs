using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;


public interface ITindakanDal :
    IInsert<TindakanDto>,
    IUpdate<TindakanDto>,
    IDelete<ITindakanKey>,
    IGetData<TindakanDto, ITindakanKey>,
    IListData<TindakanDto, IRegKey> 
{
}

public class TindakanDal : ITindakanDal
{
    private readonly DatabaseOptions _opt;

    public TindakanDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TindakanDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_Tindakan(
                TindakanId, TindakanDate, JenisTindakan, OrderId, RegId, PasienId, PasienName, 
                LayananId, LayananName, TarifId, TarifName, Total, 
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @TindakanId, @TindakanDate, @JenisTindakan, @OrderId, @RegId, @PasienId, @PasienName, 
                @LayananId, @LayananName, @TarifId, @TarifName, @Total, 
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@JenisTindakan", dto.JenisTindakan, SqlDbType.Int);
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
        
    }

    public void Update(TindakanDto dto)
    {
        const string sql = """
           UPDATE 
                BILRG_Tindakan
           SET
              TindakanDate = @TindakanDate, 
              JenisTindakan = @JenisTindakan,
              OrderId = @OrderId, 
              RegId = @RegId, 
              PasienId = @PasienId, 
              PasienName = @PasienName, 
              LayananId = @LayananId, 
              LayananName = @LayananName, 
              TarifId = @TarifId, 
              TarifName = @TarifName, 
              Total = @Total, 
              UpdUser = @UpdUser, 
              UpdDate = @UpdDate
           WHERE
              TindakanId = @TindakanId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@JenisTindakan", dto.JenisTindakan, SqlDbType.Int);
        dp.AddParam("@OrderId", dto.OrderId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        dp.AddParam("@TarifId", dto.TarifId, SqlDbType.VarChar);
        dp.AddParam("@TarifName", dto.TarifName, SqlDbType.VarChar);
        dp.AddParam("@Total", dto.Total, SqlDbType.Decimal);
        
        dp.AddParam("@CrtUser", dto.CrtUser, SqlDbType.VarChar);
        dp.AddParam("@CrtDate", dto.CrtDate, SqlDbType.DateTime);
        dp.AddParam("@UpdUser", dto.UpdUser, SqlDbType.VarChar);
        dp.AddParam("@UpdDate", dto.UpdDate, SqlDbType.DateTime);
        dp.AddParam("@VodUser", dto.VodUser, SqlDbType.VarChar);
        dp.AddParam("@VodDate", dto.VodDate, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITindakanKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_Tindakan
           WHERE
             TindakanId = @TindakanId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TindakanDto GetData(ITindakanKey key)
    {
        const string sql = """
           SELECT
               aa.TindakanId, aa.TindakanDate, aa.JenisTindakan, aa.OrderId,  
               aa.RegId, aa.PasienId, aa.PasienName, 
               aa.LayananId, aa.LayananName, aa.TarifId, aa.TarifName, aa.Total, 
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.OrderDate, '') AS OrderDate, 
               ISNULL(bb.TarifId,''), TarifOrderId 
               ISNULL(bb.TarifName,'') AS TarifOrderName
           FROM
               BILRG_Tindakan aa
               LEFT JOIN BILRG_OrderTindakan bb ON aa.OrderId = bb.OrderId
           WHERE
               aa.TindakanId = @TindakanId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TindakanDto>(sql, dp);
    }

    public IEnumerable<TindakanDto> ListData(IRegKey filter)
    {
        const string sql = """
           SELECT
               aa.TindakanId, aa.TindakanDate, aa.JenisTindakan, aa.OrderId,  
               aa.RegId, aa.PasienId, aa.PasienName, 
               aa.LayananId, aa.LayananName, aa.TarifId, aa.TarifName, aa.Total, 
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate,
               ISNULL(bb.OrderDate, '') AS OrderDate, 
               ISNULL(bb.TarifId,''), TarifOrderId 
               ISNULL(bb.TarifName,'') AS TarifOrderName
           FROM
               BILRG_Tindakan aa
               LEFT JOIN BILRG_OrderTindakan bb ON aa.OrderId = bb.OrderId
           WHERE
               aa.RegId = @RegId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanDto>(sql, dp);
    }
}