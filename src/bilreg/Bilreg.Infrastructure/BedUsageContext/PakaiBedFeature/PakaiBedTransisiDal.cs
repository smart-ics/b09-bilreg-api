using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedTransisiDal :
    IInsertBulk<PakaiBedTransisiDto>, IListData<PakaiBedTransisiDto, IPakaiBedAlokasiKey>
{
}

public class PakaiBedTransisiDal : IPakaiBedTransisiDal
{
    private readonly DatabaseOptions _opt;
    public PakaiBedTransisiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<PakaiBedTransisiDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0) return;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        foreach (var name in Columns) bcp.AddMap(name, name);
        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaPakaiBedTransisi";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public IEnumerable<PakaiBedTransisiDto> ListData(IPakaiBedAlokasiKey filter)
    {
        const string sql = """
            SELECT TransitionId, PakaiBedId, PakaiBedTransition, OccurredAt, RecordedAt,
                ActorId, Reason, RequestId, WaitingListId, BedAssignabilityEvidenceId,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaPakaiBedTransisi
            WHERE PakaiBedId = @PakaiBedId
            ORDER BY OccurredAt, TransitionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", filter.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedTransisiDto>(sql, dp);
    }

    private static readonly string[] Columns =
    [
        "TransitionId", "PakaiBedId", "PakaiBedTransition", "OccurredAt", "RecordedAt",
        "ActorId", "Reason", "RequestId", "WaitingListId", "BedAssignabilityEvidenceId",
        "CrtUser", "CrtDate", "UpdUser", "UpdDate", "VodUser", "VodDate"
    ];
}
