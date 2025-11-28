using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianEntryDal :
    IInsert<AntrianEntryDto>,
    IUpdate<AntrianEntryDto>,
    IDelete<IAntrianKey>,
    IListData<AntrianEntryDto, IAntrianKey>
{
    void Delete(IAntrianKey key, int noUrut);
    AntrianEntryDto GetData(IAntrianKey key, int noUrut);
}

public class AntrianEntryDal : IAntrianEntryDal
{
    private readonly DatabaseOptions _opt;

    public AntrianEntryDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianEntryDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AntrianEntry(
                AntrianId, NoUrut, PersonName, AntrianStatus,
                PasienTrackerId, CreatedAt, ServedAt, DoneAt) 
            VALUES(
                @AntrianId, @NoUrut, @PersonName, @AntrianStatus,
                @PasienTrackerId, @CreatedAt, @ServedAt, @DoneAt)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", dto.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", dto.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", dto.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", dto.DoneAt, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianEntryDto model)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName, 
               PasienTrackerId = @PasienTrackerId,
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt, 
               ServedAt = @ServedAt, 
               DoneAt = @DoneAt 
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", model.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", model.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", model.PersonName, SqlDbType.VarChar);
        dp.AddParam("@PasienTrackerId", model.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@AntrianStatus", model.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", model.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", model.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", model.DoneAt, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key, int noUrut)
    {
        const string sql = """
           DELETE FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key)
    {
        const string sql = """
           DELETE FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    
    public AntrianEntryDto GetData(IAntrianKey key, int noUrut)
    {
        const string sql = """
           SELECT
               AntrianId, NoUrut, PersonName, PasienTrackerId, 
               AntrianStatus, CreatedAt, ServedAt, DoneAt
           FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianEntryDto>(sql, dp);
    }

    public IEnumerable<AntrianEntryDto> ListData(IAntrianKey filter)
    {
        const string sql = """
            SELECT
            AntrianId, NoUrut, PersonName, PasienTrackerId, 
            AntrianStatus, CreatedAt, ServedAt, DoneAt
            FROM
                BILRG_AntrianEntry
            WHERE
               AntrianId = @AntrianId 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", filter.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianEntryDto>(sql, dp);
    }
}
