using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public interface IBilrgTataRekPasienBalanceOutstandingDal :
    IInsertBulk<BilrgTataRekPasienBalanceOutstandingDto>,
    IDelete<IPasienKey>,
    IListData<BilrgTataRekPasienBalanceOutstandingDto, IPasienKey>
{
}

public class BilrgTataRekPasienBalanceOutstandingDal : IBilrgTataRekPasienBalanceOutstandingDal
{
    private readonly DatabaseOptions _opt;

    public BilrgTataRekPasienBalanceOutstandingDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<BilrgTataRekPasienBalanceOutstandingDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("EntryId", "EntryId");
        bcp.AddMap("PasienId", "PasienId");
        bcp.AddMap("RegId", "RegId");
        bcp.AddMap("OutstandingJasa", "OutstandingJasa");
        bcp.AddMap("OutstandingObat", "OutstandingObat");
        bcp.AddMap("LastTransactionDate", "LastTransactionDate");
        bcp.AddMap("SourceReference", "SourceReference");
        bcp.AddMap("CreatedAt", "CreatedAt");
        bcp.AddMap("CreatedBy", "CreatedBy");

        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_TataRekPasienBalanceOutstanding";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public void Delete(IPasienKey key)
    {
        const string sql = """
            DELETE FROM BILRG_TataRekPasienBalanceOutstanding
            WHERE PasienId = @PasienId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<BilrgTataRekPasienBalanceOutstandingDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT
                EntryId, PasienId, RegId,
                OutstandingJasa, OutstandingObat,
                LastTransactionDate, SourceReference,
                CreatedAt, CreatedBy
            FROM BILRG_TataRekPasienBalanceOutstanding
            WHERE PasienId = @PasienId
            ORDER BY LastTransactionDate, EntryId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BilrgTataRekPasienBalanceOutstandingDto>(sql, dp);
    }
}
