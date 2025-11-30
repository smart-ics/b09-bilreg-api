using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.BangsalFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IRoomCatDal :
    IInsert<RoomCatDto>,
    IUpdate<RoomCatDto>,
    IDelete<IRoomCatKey>,
    IGetData<RoomCatDto, IRoomCatKey>,
    IListData<RoomCatDto>
{
};

public class RoomCatDal : IRoomCatDal
{
    private readonly DatabaseOptions _opt;

    public RoomCatDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(RoomCatDto dto)
    {
        const string sql = @"
           INSERT INTO
                BILRG_RoomCat (RoomCatId, RoomCatName)
           VALUES (@RoomCatId, @RoomCatName)";

        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", dto.RoomCatId, SqlDbType.VarChar); 
        dp.AddParam("@RoomCatName", dto.RoomCatName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(RoomCatDto dto)
    {
        const string sql = @"
           UPDATE
                BILRG_RoomCat 
           SET 
               RoomCatName = @RoomCatName
           WHERE RoomCatId = @RoomCatId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", dto.RoomCatId, SqlDbType.VarChar); 
        dp.AddParam("@RoomCatName", dto.RoomCatName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRoomCatKey key)
    {
        const string sql = @"
            DELETE FROM 
                BILRG_RoomCat
            WHERE RoomCatId = @RoomCatId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", key.RoomCatId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public RoomCatDto GetData(IRoomCatKey key)
    {
        const string sql = @"
            SELECT 
                RoomCatId, RoomCatName
            FROM BILRG_RoomCat
            WHERE RoomCatId = @RoomCatId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@RoomCatId", key.RoomCatId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<RoomCatDto>(sql, dp);
    }

    public IEnumerable<RoomCatDto> ListData()
    {
        const string sql = @"
            SELECT 
                RoomCatId, RoomCatName
            FROM BILRG_RoomCat";
        
        var dp = new DynamicParameters();
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RoomCatDto>(sql, dp);
    }
}