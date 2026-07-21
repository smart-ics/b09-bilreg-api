using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianDal :
    IInsert<AntrianDto>,
    IUpdate<AntrianDto>,
    IDelete<IAntrianKey>,
    IGetData<AntrianDto, IAntrianKey>,
    IListData<AntrianDto, Periode>,
    IListData<AntrianViewDto, DateTime>
{
    
}

public class AntrianDal : IAntrianDal
{
    private readonly DatabaseOptions _opt;

    public AntrianDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianDto dto)
    {
        const string sql = """
           INSERT INTO BILRG_Antrian(
               AntrianId, AntrianDate, StartTime, EndTime,
               SequenceTag, AntrianDescription, ServicePointCode)
           VALUES (
               @AntrianId, @AntrianDate, @StartTime, @EndTime,
               @SequenceTag, @AntrianDescription, @ServicePointCode)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@AntrianDate", dto.AntrianDate, SqlDbType.DateTime);	 
        dp.AddParam("@StartTime", dto.StartTime, SqlDbType.VarChar);	 
        dp.AddParam("@EndTime", dto.EndTime, SqlDbType.VarChar);	
        dp.AddParam("@SequenceTag", dto.SequenceTag, SqlDbType.VarChar);	 
        dp.AddParam("@AntrianDescription", dto.AntrianDescription, SqlDbType.VarChar);
        dp.AddParam("@ServicePointCode", dto.ServicePointCode, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianDto dto)
    {
        const string sql = """
           UPDATE 
                BILRG_Antrian
           SET
                AntrianDate = @AntrianDate, 
                StartTime = @StartTime, 
                EndTime = @EndTime,
                SequenceTag = @SequenceTag, 
                AntrianDescription = @AntrianDescription,
                ServicePointCode = @ServicePointCode
           WHERE
               AntrianId = @AntrianId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@AntrianDate", dto.AntrianDate, SqlDbType.DateTime);	 
        dp.AddParam("@StartTime", dto.StartTime, SqlDbType.VarChar);	 
        dp.AddParam("@EndTime", dto.EndTime, SqlDbType.VarChar);	
        dp.AddParam("@SequenceTag", dto.SequenceTag, SqlDbType.VarChar);	 
        dp.AddParam("@AntrianDescription", dto.AntrianDescription, SqlDbType.VarChar);
        dp.AddParam("@ServicePointCode", dto.ServicePointCode, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key)
    {
        const string sql = """
           DELETE FROM 
                BILRG_Antrian
           WHERE
               AntrianId = @AntrianId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AntrianDto GetData(IAntrianKey key)
    {
        const string sql = """
           SELECT
               AntrianId, AntrianDate, StartTime, EndTime,
               SequenceTag, AntrianDescription, ServicePointCode
           FROM
                BILRG_Antrian
           WHERE
               AntrianId = @AntrianId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianDto>(sql, dp);
    }

    public IEnumerable<AntrianDto> ListData(Periode filter)
    {
        const string sql = """
           SELECT
               AntrianId, AntrianDate, StartTime, EndTime,
               SequenceTag, AntrianDescription, ServicePointCode
           FROM
                BILRG_Antrian
           WHERE
               AntrianDate BETWEEN @Tgl1 AND @Tgl2
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime); 
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianDto>(sql, dp);
    }

    public IEnumerable<AntrianViewDto> ListData(DateTime date)
    {
        const string sql = """
           SELECT
                aa.AntrianId, aa.AntrianStatus, aa.NoUrut, 
                aa.PersonName, aa.ReffId, aa.ReffDesc,
                ISNULL(bb.AntrianDate,'') AS AntrianDate, 
                ISNULL(bb.SequenceTag,'') AS SequenceTag, 
                ISNULL(bb.AntrianDescription,'') AS AntrianDescription, 
                ISNULL(bb.StartTime,'') AS StartTime, 
                ISNULL(bb.EndTime,'') AS EndTime
           FROM
           	    BILRG_AntrianEntry aa
           	    LEFT JOIN BILRG_Antrian bb ON aa.AntrianId = bb.AntrianId
           WHERE 
           	    bb.AntrianDate = @Tgl
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl", date, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianViewDto>(sql, dp);
    }
}
