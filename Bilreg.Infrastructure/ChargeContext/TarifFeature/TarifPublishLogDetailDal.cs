using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifPublishLogDetailDal :
    IInsertBulk<TarifPublishLogDetailDto>,
    IDelete<ITarifPublishLogKey>,
    IListData<TarifPublishLogDetailDto, ITarifPublishLogKey>
{
}

public class TarifPublishLogDetailDal : ITarifPublishLogDetailDal
{
    private readonly DatabaseOptions _opt;

    public TarifPublishLogDetailDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<TarifPublishLogDetailDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("PublishLogId", "PublishLogId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("TarifId", "TarifId");
        bcp.AddMap("KelasId", "KelasId");
        bcp.AddMap("TipeTarifId", "TipeTarifId");
        bcp.AddMap("NilaiTarifId", "NilaiTarifId");
        bcp.AddMap("Nilai", "Nilai");

        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_TarifPublishLogDetail";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ITarifPublishLogKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TarifPublishLogDetail
            WHERE PublishLogId = @PublishLogId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PublishLogId", key.PublishLogId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TarifPublishLogDetailDto> ListData(ITarifPublishLogKey filter)
    {
        const string sql = """
            SELECT
                PublishLogId, ItemNo, TarifId, KelasId, TipeTarifId, NilaiTarifId, Nilai
            FROM BILRG_TarifPublishLogDetail
            WHERE PublishLogId = @PublishLogId
            ORDER BY ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PublishLogId", filter.PublishLogId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifPublishLogDetailDto>(sql, dp);
    }
}
