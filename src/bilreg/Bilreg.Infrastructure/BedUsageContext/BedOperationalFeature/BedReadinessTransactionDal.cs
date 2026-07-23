using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public interface IBedReadinessTransactionDal :
    IInsertBulk<BedReadinessTransactionDto>,
    IListData<BedReadinessTransactionDto, IBedOperationalKey>
{
}

public class BedReadinessTransactionDal : IBedReadinessTransactionDal
{
    private readonly DatabaseOptions _opt;

    public BedReadinessTransactionDal(IOptions<DatabaseOptions> opt) =>
        _opt = opt.Value;

    public void Insert(IEnumerable<BedReadinessTransactionDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        foreach (var name in Columns)
            bcp.AddMap(name, name);
        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaBedReadinessTransaction";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public IEnumerable<BedReadinessTransactionDto> ListData(
        IBedOperationalKey filter)
    {
        const string sql = """
            SELECT
                TransactionId, BedId, NewStatus, RestrictionType,
                Reason, EvidenceReference, ResponsibleActorId, VerifiedByActorId,
                OccurredAt, RecordedAt, SourceFactId, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaBedReadinessTransaction
            WHERE BedId = @BedId
            ORDER BY OccurredAt, TransactionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", filter.BedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedReadinessTransactionDto>(sql, dp);
    }

    private static readonly string[] Columns =
    [
        "TransactionId", "BedId", "NewStatus", "RestrictionType",
        "Reason", "EvidenceReference", "ResponsibleActorId", "VerifiedByActorId",
        "OccurredAt", "RecordedAt", "SourceFactId", "RequestId",
        "CrtUser", "CrtDate", "UpdUser", "UpdDate", "VodUser", "VodDate"
    ];
}
