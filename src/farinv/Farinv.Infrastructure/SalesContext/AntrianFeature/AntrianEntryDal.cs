using Dapper;
using Farinv.Domain.SalesContext.AntrianFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.SalesContext.AntrianFeature;

public interface IAntrianEntryDal :
    IInsert<AntrianEntryDto>,
    IInsertBulk<AntrianEntryDto>,
    IUpdate<AntrianEntryDto>,
    IDelete<IAntrianKey>,
    IListData<AntrianEntryDto, IAntrianKey>
{
    void Delete(IAntrianKey key, int noAntrian);
    AntrianEntryDto GetData(IAntrianKey key, int noAntrian);
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
       INSERT INTO FARIN_AntrianEntry(
            AntrianId, NoAntrian, AntrianStatus, 
            TakenAt, AssignedAt, PreparedAt, DeliveredAt, CancelAt,
            RegId, PasienId, PasienName, ReffId, ReffDesc)
       VALUES (
            @AntrianId, @NoAntrian, @AntrianStatus, 
            @TakenAt, @AssignedAt, @PreparedAt, @DeliveredAt, @CancelAt,
            @RegId, @PasienId, @PasienName, @ReffId, @ReffDesc)
       """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);
        dp.AddParam("@TakenAt", dto.TakenAt, SqlDbType.DateTime);
        dp.AddParam("@AssignedAt", dto.AssignedAt, SqlDbType.DateTime);
        dp.AddParam("@PreparedAt", dto.PreparedAt, SqlDbType.DateTime);
        dp.AddParam("@DeliveredAt", dto.DeliveredAt, SqlDbType.DateTime);
        dp.AddParam("@CancelAt", dto.CancelAt, SqlDbType.DateTime);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Insert(IEnumerable<AntrianEntryDto> listDto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        bcp.AddMap("AntrianId", "AntrianId");
        bcp.AddMap("NoAntrian", "NoAntrian");
        bcp.AddMap("AntrianStatus", "AntrianStatus");
        bcp.AddMap("TakenAt", "TakenAt");
        bcp.AddMap("AssignedAt", "AssignedAt");
        bcp.AddMap("PreparedAt", "PreparedAt");
        bcp.AddMap("DeliveredAt", "DeliveredAt");
        bcp.AddMap("CancelAt", "CancelAt");
        bcp.AddMap("RegId", "RegId");
        bcp.AddMap("PasienId", "PasienId");
        bcp.AddMap("PasienName", "PasienName");
        bcp.AddMap("ReffId", "ReffId");
        bcp.AddMap("ReffDesc", "ReffDesc");
        var fetched = listDto.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "FARIN_AntrianEntry";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Update(AntrianEntryDto dto)
    {
        const string sql = """
           UPDATE
                FARIN_AntrianEntry
           SET
                AntrianStatus = @AntrianStatus,
                TakenAt = @TakenAt, 
                AssignedAt = @AssignedAt, 
                PreparedAt = @PreparedAt,
                DeliveredAt = @DeliveredAt,
                CancelAt =@CancelAt,
                RegId = @RegId,
                PasienId = @PasienId,
                PasienName = @PasienName,
                ReffId = @ReffId,
                ReffDesc = @ReffDesc
           WHERE
                AntrianId = @AntrianId 
                AND NoAntrian = @NoAntrian
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", dto.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", dto.NoAntrian, SqlDbType.Int);
        dp.AddParam("@AntrianStatus", dto.AntrianStatus, SqlDbType.Int);
        dp.AddParam("@TakenAt", dto.TakenAt, SqlDbType.DateTime);
        dp.AddParam("@AssignedAt", dto.AssignedAt, SqlDbType.DateTime);
        dp.AddParam("@PreparedAt", dto.PreparedAt, SqlDbType.DateTime);
        dp.AddParam("@DeliveredAt", dto.DeliveredAt, SqlDbType.DateTime);
        dp.AddParam("@CancelAt", dto.CancelAt, SqlDbType.DateTime);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@PasienName", dto.PasienName, SqlDbType.VarChar);
        dp.AddParam("@ReffId", dto.ReffId, SqlDbType.VarChar);
        dp.AddParam("@ReffDesc", dto.ReffDesc, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key)
    {
        const string sql = """
           DELETE FROM
                FARIN_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IAntrianKey key, int noAntrian)
    {
        const string sql = """
           DELETE FROM
                FARIN_AntrianEntry
           WHERE
                AntrianId = @AntrianId 
                AND NoAntrian = @NoAntrian
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", noAntrian, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AntrianEntryDto GetData(IAntrianKey key, int noAntrian)
    {
        const string sql = """
           SELECT
                AntrianId, NoAntrian, AntrianStatus,
                TakenAt, AssignedAt, PreparedAt, DeliveredAt, CancelAt,
                RegId, PasienId, PasienName, ReffId, ReffDesc
           FROM
                FARIN_AntrianEntry
           WHERE
                AntrianId = @AntrianId 
                AND NoAntrian = @NoAntrian
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key.AntrianId, SqlDbType.VarChar);
        dp.AddParam("@NoAntrian", noAntrian, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianEntryDto>(sql, dp);
    }    

    public IEnumerable<AntrianEntryDto> ListData(IAntrianKey filter)
    {
        const string sql = """
            SELECT
                 AntrianId, NoAntrian, AntrianStatus,
                 TakenAt, AssignedAt, PreparedAt, DeliveredAt, CancelAt,
                 RegId, PasienId, PasienName, ReffId, ReffDesc
            FROM
                 FARIN_AntrianEntry
            WHERE
                AntrianId = @AntrianId 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", filter.AntrianId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianEntryDto>(sql, dp);
    }    
}
