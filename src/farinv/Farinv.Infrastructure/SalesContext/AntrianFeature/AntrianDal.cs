using Dapper;
using Farinv.Domain.SalesContext.AntrianFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

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
           INSERT INTO FARIN_Antrian(
               AntrianId, AntrianDate, ServicePoint, AntrianDescription)
           VALUES (
               @AntrianId, @AntrianDate, @ServicePoint, @AntrianDescription)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@AntrianDate", dto.AntrianDate, SqlDbType.DateTime);
        dp.AddParam("@ServicePoint", dto.ServicePoint, SqlDbType.Int);
        dp.AddParam("@AntrianDescription", dto.AntrianDescription, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianDto dto)
    {
        const string sql = """
           UPDATE 
                FARIN_Antrian
           SET
                AntrianDate = @AntrianDate, 
                ServicePoint = @ServicePoint, 
                AntrianDescription = @AntrianDescription
           WHERE
                AntrianId = @AntrianId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@AntrianDate", dto.AntrianDate, SqlDbType.DateTime);
        dp.AddParam("@ServicePoint", dto.ServicePoint, SqlDbType.Int);
        dp.AddParam("@AntrianDescription", dto.AntrianDescription, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key)
    {
        const string sql = """
           DELETE FROM 
                FARIN_Antrian
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
                AntrianId, AntrianDate, ServicePoint, AntrianDescription
           FROM
                FARIN_Antrian
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
                AntrianId, AntrianDate, ServicePoint, AntrianDescription
           FROM
                FARIN_Antrian
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
                aa.AntrianId, aa.NoAntrian, aa.AntrianStatus, 
                aa.RegId, aa.PasienId, aa.PasienName, 
                aa.ReffId, aa.ReffDesc,
                ISNULL(bb.AntrianDate,'') AS AntrianDate, 
                ISNULL(bb.ServicePoint, 0) AS ServicePoint,
                ISNULL(bb.AntrianDescription,'') AS AntrianDescription
           FROM
           	    FARIN_AntrianEntry aa
           	    LEFT JOIN FARIN_Antrian bb ON aa.AntrianId = bb.AntrianId
           WHERE 
           	    bb.AntrianDate = @Tgl
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl", date, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianViewDto>(sql, dp);
    }
}
