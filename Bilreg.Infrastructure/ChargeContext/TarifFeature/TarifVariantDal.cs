using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifVariantDal :
    IInsertBulk<TarifVariantDto>,
    IDelete<ITarifPolicyKey>,
    IListData<TarifVariantDto, ITarifPolicyKey>
{
}

public class TarifVariantDal : ITarifVariantDal
{
    private readonly DatabaseOptions _opt;

    public TarifVariantDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<TarifVariantDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("TarifPolicyId", "TarifPolicyId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("TarifId", "TarifId");
        bcp.AddMap("KelasId", "KelasId");
        bcp.AddMap("TipeTarifId", "TipeTarifId");
        bcp.AddMap("Nilai", "Nilai");
        bcp.AddMap("PublishedNilaiTarifId", "PublishedNilaiTarifId");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_TarifVariant";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ITarifPolicyKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TarifVariant
            WHERE TarifPolicyId = @TarifPolicyId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", key.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TarifVariantDto> ListData(ITarifPolicyKey filter)
    {
        const string sql = """
            SELECT
                TarifPolicyId, ItemNo, TarifId, KelasId, TipeTarifId, Nilai, PublishedNilaiTarifId
            FROM BILRG_TarifVariant
            WHERE TarifPolicyId = @TarifPolicyId
            ORDER BY ItemNo
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", filter.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifVariantDto>(sql, dp);
    }
}
