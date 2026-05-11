using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.IgdContext.BedIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.IgdContext.BedIgdFeature;

public interface IPakaiBedDal :
    IInsert<PakaiBedDto>,
    IUpdate<PakaiBedDto>,
    IDelete<IPakaiBedKey>,
    IGetData<PakaiBedDto, IPakaiBedKey>,
    IListData<PakaiBedDto, IIgdVisitKey>
{
    PakaiBedDto? GetOpenForBed(IBedIgdKey bed);
    PakaiBedDto? GetOpenForVisit(IIgdVisitKey visit);
    IEnumerable<OrphanPakaiBedRow> ListOrphans();
}

public record OrphanPakaiBedRow(
    string PakaiBedId,
    string IgdVisitId,
    string BedIgdId,
    string BedIgdName,
    DateTime CheckInDateTime,
    string OrphanReason,
    string VisitState,
    string BedState,
    string BedCurrentIgdVisitId);

public class PakaiBedDal : IPakaiBedDal
{
    private static readonly DateTime OPEN_SENTINEL = new(3000, 1, 1);
    private readonly DatabaseOptions _opt;

    public PakaiBedDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PakaiBedDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_PakaiBed (
                PakaiBedId, IgdVisitId, BedIgdId, BedIgdName,
                CheckInDateTime, CheckInUserId, CheckOutDateTime, CheckOutUserId)
            VALUES (
                @PakaiBedId, @IgdVisitId, @BedIgdId, @BedIgdName,
                @CheckInDateTime, @CheckInUserId, @CheckOutDateTime, @CheckOutUserId)
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PakaiBedDto dto)
    {
        const string sql = """
            UPDATE BILRG_PakaiBed
            SET IgdVisitId = @IgdVisitId,
                BedIgdId = @BedIgdId,
                BedIgdName = @BedIgdName,
                CheckInDateTime = @CheckInDateTime,
                CheckInUserId = @CheckInUserId,
                CheckOutDateTime = @CheckOutDateTime,
                CheckOutUserId = @CheckOutUserId
            WHERE PakaiBedId = @PakaiBedId
            """;
        var dp = BuildParams(dto);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPakaiBedKey key)
    {
        const string sql = "DELETE BILRG_PakaiBed WHERE PakaiBedId = @PakaiBedId";
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", key.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PakaiBedDto GetData(IPakaiBedKey key)
    {
        const string sql = """
            SELECT PakaiBedId, IgdVisitId, BedIgdId, BedIgdName,
                CheckInDateTime, CheckInUserId, CheckOutDateTime, CheckOutUserId
            FROM BILRG_PakaiBed
            WHERE PakaiBedId = @PakaiBedId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", key.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedDto>(sql, dp);
    }

    public IEnumerable<PakaiBedDto> ListData(IIgdVisitKey filter)
    {
        const string sql = """
            SELECT PakaiBedId, IgdVisitId, BedIgdId, BedIgdName,
                CheckInDateTime, CheckInUserId, CheckOutDateTime, CheckOutUserId
            FROM BILRG_PakaiBed
            WHERE IgdVisitId = @IgdVisitId
            ORDER BY CheckInDateTime
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", filter.IgdVisitId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedDto>(sql, dp);
    }

    public PakaiBedDto? GetOpenForBed(IBedIgdKey bed)
    {
        const string sql = """
            SELECT TOP 1 PakaiBedId, IgdVisitId, BedIgdId, BedIgdName,
                CheckInDateTime, CheckInUserId, CheckOutDateTime, CheckOutUserId
            FROM BILRG_PakaiBed
            WHERE BedIgdId = @BedIgdId
              AND CheckOutDateTime = @OpenSentinel
            ORDER BY CheckInDateTime DESC
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedIgdId", bed.BedIgdId, SqlDbType.VarChar);
        dp.AddParam("@OpenSentinel", OPEN_SENTINEL, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedDto>(sql, dp);
    }

    public PakaiBedDto? GetOpenForVisit(IIgdVisitKey visit)
    {
        const string sql = """
            SELECT TOP 1 PakaiBedId, IgdVisitId, BedIgdId, BedIgdName,
                CheckInDateTime, CheckInUserId, CheckOutDateTime, CheckOutUserId
            FROM BILRG_PakaiBed
            WHERE IgdVisitId = @IgdVisitId
              AND CheckOutDateTime = @OpenSentinel
            ORDER BY CheckInDateTime DESC
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@IgdVisitId", visit.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@OpenSentinel", OPEN_SENTINEL, SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PakaiBedDto>(sql, dp);
    }

    public IEnumerable<OrphanPakaiBedRow> ListOrphans()
    {
        const string sql = """
            SELECT
                pb.PakaiBedId,
                pb.IgdVisitId,
                pb.BedIgdId,
                pb.BedIgdName,
                pb.CheckInDateTime,
                CASE
                    WHEN v.IgdVisitId IS NULL THEN 'VISIT_NOT_FOUND'
                    WHEN v.VodDate <> @VoidSentinel THEN 'VISIT_VOIDED'
                    WHEN v.AdministrativeState IN ('DISCHARGED','REDIRECTED') THEN 'VISIT_TERMINAL'
                    WHEN b.BedIgdId IS NULL THEN 'BED_NOT_FOUND'
                    WHEN ISNULL(b.CurrentIgdVisitId,'') <> pb.IgdVisitId THEN 'BED_REASSIGNED'
                    WHEN b.BedState <> 'OCCUPIED' THEN 'BED_NOT_OCCUPIED'
                    ELSE 'UNKNOWN'
                END AS OrphanReason,
                ISNULL(v.AdministrativeState,'-')   AS VisitState,
                ISNULL(b.BedState,'-')              AS BedState,
                ISNULL(b.CurrentIgdVisitId,'-')     AS BedCurrentIgdVisitId
            FROM BILRG_PakaiBed pb
            LEFT JOIN BILRG_IgdVisit v ON v.IgdVisitId = pb.IgdVisitId
            LEFT JOIN BILRG_BedIgd   b ON b.BedIgdId   = pb.BedIgdId
            WHERE pb.CheckOutDateTime = @OpenSentinel
              AND (
                    v.IgdVisitId IS NULL
                 OR v.VodDate <> @VoidSentinel
                 OR v.AdministrativeState IN ('DISCHARGED','REDIRECTED')
                 OR b.BedIgdId IS NULL
                 OR ISNULL(b.CurrentIgdVisitId,'') <> pb.IgdVisitId
                 OR b.BedState <> 'OCCUPIED'
              )
            ORDER BY pb.CheckInDateTime
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@OpenSentinel", OPEN_SENTINEL, SqlDbType.DateTime);
        dp.AddParam("@VoidSentinel", new DateTime(3000, 1, 1), SqlDbType.DateTime);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OrphanPakaiBedRow>(sql, dp);
    }

    private static DynamicParameters BuildParams(PakaiBedDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", dto.PakaiBedId, SqlDbType.VarChar);
        dp.AddParam("@IgdVisitId", dto.IgdVisitId, SqlDbType.VarChar);
        dp.AddParam("@BedIgdId", dto.BedIgdId, SqlDbType.VarChar);
        dp.AddParam("@BedIgdName", dto.BedIgdName, SqlDbType.VarChar);
        dp.AddParam("@CheckInDateTime", dto.CheckInDateTime, SqlDbType.DateTime);
        dp.AddParam("@CheckInUserId", dto.CheckInUserId, SqlDbType.VarChar);
        dp.AddParam("@CheckOutDateTime", dto.CheckOutDateTime, SqlDbType.DateTime);
        dp.AddParam("@CheckOutUserId", dto.CheckOutUserId, SqlDbType.VarChar);
        return dp;
    }
}
