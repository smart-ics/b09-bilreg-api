using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BangsalFeature;

public interface IRoomCatDal :
    IInsert<RoomCatType>,
    IUpdate<RoomCatType>,
    IDelete<IRoomCatKey>,
    IGetData<RoomCatType, IRoomCatKey>,
    IListData<RoomCatType>
{
}

public class RoomCatDal : IRoomCatDal
{
    private readonly DatabaseOptions _opt;

    public RoomCatDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(RoomCatType model)
    {
        const string sql = """
            INSERT INTO BILRG_RoomCat(
                RoomCatId, RoomCatName)
            VALUES( 
                @RoomCatId, @RoomCatName)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", model.RoomCatId, SqlDbType.VarChar);
        dp.AddParam("@RoomCatName", model.RoomCatName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RoomCatType model)
    {
        const string sql = @"
           UPDATE 
               BILRG_RoomCat
           SET
               RoomCatName = @RoomCatName
           WHERE
               RoomCatId = @RoomCatId";

        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", model.RoomCatId, SqlDbType.VarChar);
        dp.AddParam("@RoomCatName", model.RoomCatName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRoomCatKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_RoomCat
           WHERE
               RoomCatId = @RoomCatId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", key.RoomCatId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RoomCatType GetData(IRoomCatKey key)
    {
        const string sql = @"
           SELECT
               RoomCatId,
               RoomCatName
           FROM 
               BILRG_RoomCat
           WHERE
               RoomCatId = @RoomCatId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", key.RoomCatId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<RoomCatType>(sql, dp);
        return result;
    }

    public IEnumerable<RoomCatType> ListData()
    {
        const string sql = """
            SELECT
                RoomCatId,
                RoomCatName
            FROM 
                BILRG_RoomCat
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomCatType>(sql);
    }
}