using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifVariantKomponenDal :
    IInsertBulk<TarifVariantKomponenDto>,
    IDelete<ITarifPolicyKey>,
    IListData<TarifVariantKomponenDto, ITarifVariantKey>
{
}

public class TarifVariantKomponenDal : ITarifVariantKomponenDal
{
    private readonly DatabaseOptions _opt;

    public TarifVariantKomponenDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<TarifVariantKomponenDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("TarifPolicyId", "TarifPolicyId");
        bcp.AddMap("ItemNo", "ItemNo");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("KomponenId", "KomponenId");
        bcp.AddMap("Nilai", "Nilai");

        var fetched = listModel.ToList();
        if (fetched.Count == 0)
            return;

        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_TarifVariantKomponen";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(ITarifPolicyKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TarifVariantKomponen
            WHERE TarifPolicyId = @TarifPolicyId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", key.TarifPolicyId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<TarifVariantKomponenDto> ListData(ITarifVariantKey filter)
    {
        const string sql = """
            SELECT
                TarifPolicyId, ItemNo, NoUrut, KomponenId, Nilai
            FROM BILRG_TarifVariantKomponen
            WHERE TarifPolicyId = @TarifPolicyId
              AND ItemNo = @ItemNo
            ORDER BY NoUrut
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@TarifPolicyId", filter.TarifPolicyId, SqlDbType.VarChar);
        dp.AddParam("@ItemNo", filter.ItemNo, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifVariantKomponenDto>(sql, dp);
    }
}
