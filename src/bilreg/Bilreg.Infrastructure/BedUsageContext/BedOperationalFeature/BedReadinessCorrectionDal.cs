using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;

public interface IBedReadinessCorrectionDal :
    IInsertBulk<BedReadinessCorrectionDto>,
    IListData<BedReadinessCorrectionDto, IBedOperationalKey>
{
}

public class BedReadinessCorrectionDal : IBedReadinessCorrectionDal
{
    private readonly DatabaseOptions _opt;

    public BedReadinessCorrectionDal(IOptions<DatabaseOptions> opt) =>
        _opt = opt.Value;

    public void Insert(IEnumerable<BedReadinessCorrectionDto> listModel)
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
        bcp.DestinationTableName = "BILRG_RnaBedReadinessCorrection";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public IEnumerable<BedReadinessCorrectionDto> ListData(
        IBedOperationalKey filter)
    {
        const string sql = """
            SELECT
                CorrectionId, BedId, OriginalTransactionId,
                ReplacementTransactionId, ActorId, Reason,
                OccurredAt, RecordedAt, RequestId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaBedReadinessCorrection
            WHERE BedId = @BedId
            ORDER BY OccurredAt, CorrectionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@BedId", filter.BedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BedReadinessCorrectionDto>(sql, dp);
    }

    private static readonly string[] Columns =
    [
        "CorrectionId", "BedId", "OriginalTransactionId",
        "ReplacementTransactionId", "ActorId", "Reason",
        "OccurredAt", "RecordedAt", "RequestId",
        "CrtUser", "CrtDate", "UpdUser", "UpdDate", "VodUser", "VodDate"
    ];
}
