using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IPasienTrackerEventDal :
    IInsert<PasienTrackerEventDto>,
    IUpdate<PasienTrackerEventDto>,
    IDelete<IPasienTrackerKey>,
    IListData<PasienTrackerEventDto, IPasienTrackerKey>
{
    void Delete(string pasienTrackerId, int noUrut);
    PasienTrackerEventDto GetData(string pasienTrackerId, int noUrut);
}

public class PasienTrackerEventDal : IPasienTrackerEventDal
{
    private readonly DatabaseOptions _opt;

    public PasienTrackerEventDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienTrackerEventDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_PasienTrackerEvent(
                PasienTrackerId, NoUrut, EventName, EventDate, ReffId)
            VALUES (
                @PasienTrackerId, @NoUrut, @EventName, @EventDate, @ReffId)
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@EventName", dto.EventName, SqlDbType.VarChar);
        dp.AddParam("@EventDate", dto.EventDate, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienTrackerEventDto dto)
    {
        const string sql = """
            UPDATE
               BILRG_PasienTrackerEvent
            SET
               PasienTrackerId = @PasienTrackerId, 
               NoUrut = @NoUrut,
               EventName = @EventName, 
               EventDate = @EventDate, 
               ReffId = @ReffId
            WHERE
               PasienTrackerId = @PasienTrackerId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);
        dp.AddParam("@EventName", dto.EventName, SqlDbType.VarChar);
        dp.AddParam("@EventDate", dto.EventDate, SqlDbType.DateTime);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    public void Delete(string pasienTrackerId, int noUrut)
    {
        const string sql = """
            DELETE FROM
              BILRG_PasienTrackerEvent
            WHERE
              PasienTrackerId = @PasienTrackerId
              AND NoUrut = @NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", pasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienTrackerEventDto GetData(string pasienTrackerId, int noUrut)
    {
        const string sql = """
            SELECT 
               PasienTrackerId, NoUrut, EventName, EventDate, ReffId
            FROM
               BILRG_PasienTrackerEvent 
            WHERE
              PasienTrackerId = @PasienTrackerId
              AND NoUrut = @NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", pasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienTrackerEventDto>(sql, dp);
    }

    public IEnumerable<PasienTrackerEventDto> ListData(IPasienTrackerKey filter)
    {
        const string sql = """
            SELECT 
                PasienTrackerId, NoUrut, EventName, EventDate, ReffId
            FROM
                BILRG_PasienTrackerEvent 
            WHERE
                PasienTrackerId = @PasienTrackerId
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", filter.PasienTrackerId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienTrackerEventDto>(sql, dp);
    }

    public void Delete(IPasienTrackerKey key)
    {
        const string sql = """
           SELECT 
               PasienTrackerId, NoUrut, EventName, EventDate, ReffId
           FROM
               BILRG_PasienTrackerEvent 
           WHERE
               PasienTrackerId = @PasienTrackerId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", key.PasienTrackerId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
