using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedKoreksiDal :
    IInsertBulk<PakaiBedKoreksiDto>, IListData<PakaiBedKoreksiDto, IPakaiBedAlokasiKey>
{
}

public class PakaiBedKoreksiDal : IPakaiBedKoreksiDal
{
    private readonly DatabaseOptions _opt;
    public PakaiBedKoreksiDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<PakaiBedKoreksiDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0) return;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        conn.Open();
        foreach (var name in Columns) bcp.AddMap(name, name);
        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_RnaPakaiBedKoreksi";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public IEnumerable<PakaiBedKoreksiDto> ListData(IPakaiBedAlokasiKey filter)
    {
        const string sql = """
            SELECT CorrectionId, PakaiBedId, OriginalTransitionId, PakaiBedCorrection,
                OriginalBangsalId, Reason, CorrectedBy, OccurredAt, RecordedAt,
                ReviewReference, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate
            FROM BILRG_RnaPakaiBedKoreksi
            WHERE PakaiBedId = @PakaiBedId
            ORDER BY OccurredAt, CorrectionId
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@PakaiBedId", filter.PakaiBedId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PakaiBedKoreksiDto>(sql, dp);
    }

    private static readonly string[] Columns =
    [
        "CorrectionId", "PakaiBedId", "OriginalTransitionId", "PakaiBedCorrection",
        "OriginalBangsalId", "Reason", "CorrectedBy", "OccurredAt", "RecordedAt",
        "ReviewReference", "CrtUser", "CrtDate", "UpdUser", "UpdDate", "VodUser", "VodDate"
    ];
}
