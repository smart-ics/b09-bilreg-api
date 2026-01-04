using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;

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
                TindakanId, TindakanDate, OrderTdkId, RegId, PasienId, PasienName, LayananId, LayananName, 
                KelasId, KelasName, TipeTarifId, TipeTarifname, TarifId, TarifName, Total, 
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES(
                @TindakanId, @TindakanDate, @OrderTdkId, @RegId, @PasienId, @PasienName, @LayananId, @LayananName, 
                @KelasId, @KelasName, @TipeTarifId, @TipeTarifname, @TarifId, @TarifName, @Total, 
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);

        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);
        
        dp.AddParam("KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", dto.TipeTarifName, SqlDbType.VarChar);
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
              OrderTdkId = @OrderTdkId, 
              RegId = @RegId, 
              PasienId = @PasienId, 
              PasienName = @PasienName, 
              LayananId = @LayananId, 
              LayananName = @LayananName,
              KelasId = @KelasId,
              KelasName = @KelasName,
              TipeTarifId = @TipeTarifId,
              TipeTarifName = @TipeTarifName,
              TarifId = @TarifId, 
              TarifName = @TarifName, 
              Total = @Total, 
              CrtUser = @CrtUser, 
              CrtDate = @CrtDate,
              UpdUser = @UpdUser, 
              UpdDate = @UpdDate,
              VodUser = @VodUser, 
              VodDate = @VodDate
           WHERE
              TindakanId = @TindakanId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", dto.TindakanId, SqlDbType.VarChar);
        dp.AddParam("@TindakanDate", dto.TindakanDate, SqlDbType.DateTime);
        dp.AddParam("@OrderTdkId", dto.OrderTdkId, SqlDbType.VarChar);

        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@LayananId", dto.LayananId, SqlDbType.VarChar);
        dp.AddParam("@LayananName", dto.LayananName, SqlDbType.VarChar);

        dp.AddParam("KelasId", dto.KelasId, SqlDbType.VarChar);
        dp.AddParam("KelasName", dto.KelasName, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifId", dto.TipeTarifId, SqlDbType.VarChar);
        dp.AddParam("@TipeTarifName", dto.TipeTarifName, SqlDbType.VarChar);
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
               aa.TindakanId, aa.TindakanDate, aa.OrderTdkId,  
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.KelasId, aa.KelasName, aa.TipeTarifId, aa.TipeTarifName, aa.TarifId, aa.TarifName, aa.Total, 
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,  aa.VodUser, aa.VodDate
           FROM
               BILRG_Tindakan aa
           	   LEFT JOIN BILRG_OrderTdk bb ON aa.OrderTdkId = bb.OrderTdkId
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
               aa.TindakanId, aa.TindakanDate, aa.OrderTdkId,  
               aa.RegId, aa.PasienId, aa.PasienName, aa.LayananId, aa.LayananName, 
               aa.KelasId, aa.KelasName, aa.TipeTarifId, aa.TipeTarifName, aa.TarifId, aa.TarifName, aa.Total, 
               aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate,  aa.VodUser, aa.VodDate
           FROM
               BILRG_Tindakan aa
           WHERE
               aa.RegId = @RegId
               AND aa.VodDate = @VodDate
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@RegId", filter.RegId, SqlDbType.VarChar);
        dp.AddParam("@VodDate", new DateTime(3000, 1, 1), SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanDto>(sql, dp);
    }
}