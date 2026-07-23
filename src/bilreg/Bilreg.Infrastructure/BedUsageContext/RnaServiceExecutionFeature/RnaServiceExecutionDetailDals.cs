using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public interface IServiceWorkSourceRevisionDal :
    IInsertBulk<ServiceWorkSourceRevisionDto>,
    IDelete<IRnaServiceExecutionKey>,
    IListData<ServiceWorkSourceRevisionDto, IRnaServiceExecutionKey>
{
}

public interface IServiceExecutionFactDal :
    IInsertBulk<ServiceExecutionFactDto>,
    IDelete<IRnaServiceExecutionKey>,
    IListData<ServiceExecutionFactDto, IRnaServiceExecutionKey>
{
}

public interface IExecutionCorrectionDal :
    IInsertBulk<ExecutionCorrectionDto>,
    IDelete<IRnaServiceExecutionKey>,
    IListData<ExecutionCorrectionDto, IRnaServiceExecutionKey>
{
}

public sealed class ServiceWorkSourceRevisionDal : IServiceWorkSourceRevisionDal
{
    private readonly DatabaseOptions _opt;

    public ServiceWorkSourceRevisionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<ServiceWorkSourceRevisionDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ServiceExecutionId", "ServiceExecutionId");
        bcp.AddMap("SourceContext", "SourceContext");
        bcp.AddMap("SourceFactId", "SourceFactId");
        bcp.AddMap("SourceRevision", "SourceRevision");
        bcp.AddMap("RevisionKind", "RevisionKind");
        bcp.AddMap("EffectiveAt", "EffectiveAt");
        bcp.AddMap("RecordedAt", "RecordedAt");
        bcp.AddMap("ActorId", "ActorId");
        bcp.AddMap("Reason", "Reason");
        bcp.AddMap("CrtUser", "CrtUser");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("UpdUser", "UpdUser");
        bcp.AddMap("UpdDate", "UpdDate");
        bcp.AddMap("VodUser", "VodUser");
        bcp.AddMap("VodDate", "VodDate");

        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaServiceExecutionSourceRevision";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public void Delete(IRnaServiceExecutionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_RnaServiceExecutionSourceRevision
            WHERE ServiceExecutionId = @ServiceExecutionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ServiceWorkSourceRevisionDto> ListData(IRnaServiceExecutionKey key)
    {
        const string sql = """
            SELECT
                aa.ServiceExecutionId, aa.SourceContext, aa.SourceFactId, aa.SourceRevision,
                aa.RevisionKind, aa.EffectiveAt, aa.RecordedAt, aa.ActorId, aa.Reason,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM
                BILRG_RnaServiceExecutionSourceRevision aa
            WHERE
                aa.ServiceExecutionId = @ServiceExecutionId
            ORDER BY
                aa.SourceRevision
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ServiceWorkSourceRevisionDto>(sql, dp);
    }
}

public sealed class ServiceExecutionFactDal : IServiceExecutionFactDal
{
    private readonly DatabaseOptions _opt;

    public ServiceExecutionFactDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<ServiceExecutionFactDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("ServiceExecutionFactId", "ServiceExecutionFactId");
        bcp.AddMap("ServiceExecutionId", "ServiceExecutionId");
        bcp.AddMap("ExecutionRevision", "ExecutionRevision");
        bcp.AddMap("BillableClassification", "BillableClassification");
        bcp.AddMap("TarifServiceId", "TarifServiceId");
        bcp.AddMap("TarifServiceName", "TarifServiceName");
        bcp.AddMap("NonBillableDescription", "NonBillableDescription");
        bcp.AddMap("PerformerId", "PerformerId");
        bcp.AddMap("PerformedAt", "PerformedAt");
        bcp.AddMap("RecordedAt", "RecordedAt");
        bcp.AddMap("RecorderActorId", "RecorderActorId");
        bcp.AddMap("LateEntryReason", "LateEntryReason");
        bcp.AddMap("CrtUser", "CrtUser");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("UpdUser", "UpdUser");
        bcp.AddMap("UpdDate", "UpdDate");
        bcp.AddMap("VodUser", "VodUser");
        bcp.AddMap("VodDate", "VodDate");

        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaServiceExecutionFact";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public void Delete(IRnaServiceExecutionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_RnaServiceExecutionFact
            WHERE ServiceExecutionId = @ServiceExecutionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ServiceExecutionFactDto> ListData(IRnaServiceExecutionKey key)
    {
        const string sql = """
            SELECT
                aa.ServiceExecutionFactId, aa.ServiceExecutionId, aa.ExecutionRevision,
                aa.BillableClassification, aa.TarifServiceId, aa.TarifServiceName,
                aa.NonBillableDescription, aa.PerformerId, aa.PerformedAt,
                aa.RecordedAt, aa.RecorderActorId, aa.LateEntryReason,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM
                BILRG_RnaServiceExecutionFact aa
            WHERE
                aa.ServiceExecutionId = @ServiceExecutionId
            ORDER BY
                aa.ExecutionRevision
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ServiceExecutionFactDto>(sql, dp);
    }
}

public sealed class ExecutionCorrectionDal : IExecutionCorrectionDal
{
    private readonly DatabaseOptions _opt;

    public ExecutionCorrectionDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<ExecutionCorrectionDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("CorrectionFactId", "CorrectionFactId");
        bcp.AddMap("ServiceExecutionId", "ServiceExecutionId");
        bcp.AddMap("OriginalServiceExecutionFactId", "OriginalServiceExecutionFactId");
        bcp.AddMap("PreviousRevision", "PreviousRevision");
        bcp.AddMap("CorrectionRevision", "CorrectionRevision");
        bcp.AddMap("CorrectionKind", "CorrectionKind");
        bcp.AddMap("ReplacementServiceExecutionFactId", "ReplacementServiceExecutionFactId");
        bcp.AddMap("CorrectionReason", "CorrectionReason");
        bcp.AddMap("CorrectingActorId", "CorrectingActorId");
        bcp.AddMap("SecondReviewerId", "SecondReviewerId");
        bcp.AddMap("EvidenceReference", "EvidenceReference");
        bcp.AddMap("CorrectedAt", "CorrectedAt");
        bcp.AddMap("RecordedAt", "RecordedAt");
        bcp.AddMap("CrtUser", "CrtUser");
        bcp.AddMap("CrtDate", "CrtDate");
        bcp.AddMap("UpdUser", "UpdUser");
        bcp.AddMap("UpdDate", "UpdDate");
        bcp.AddMap("VodUser", "VodUser");
        bcp.AddMap("VodDate", "VodDate");

        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaServiceExecutionCorrection";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public void Delete(IRnaServiceExecutionKey key)
    {
        const string sql = """
            DELETE FROM BILRG_RnaServiceExecutionCorrection
            WHERE ServiceExecutionId = @ServiceExecutionId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<ExecutionCorrectionDto> ListData(IRnaServiceExecutionKey key)
    {
        const string sql = """
            SELECT
                aa.CorrectionFactId, aa.ServiceExecutionId, aa.OriginalServiceExecutionFactId,
                aa.PreviousRevision, aa.CorrectionRevision, aa.CorrectionKind,
                aa.ReplacementServiceExecutionFactId, aa.CorrectionReason, aa.CorrectingActorId,
                aa.SecondReviewerId, aa.EvidenceReference, aa.CorrectedAt, aa.RecordedAt,
                aa.CrtUser, aa.CrtDate, aa.UpdUser, aa.UpdDate, aa.VodUser, aa.VodDate
            FROM
                BILRG_RnaServiceExecutionCorrection aa
            WHERE
                aa.ServiceExecutionId = @ServiceExecutionId
            ORDER BY
                aa.CorrectionRevision,
                aa.CorrectionFactId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ServiceExecutionId", key.ServiceExecutionId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ExecutionCorrectionDto>(sql, dp);
    }
}
