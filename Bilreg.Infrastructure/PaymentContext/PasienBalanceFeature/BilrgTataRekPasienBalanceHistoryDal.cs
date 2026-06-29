using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.PaymentContext.PasienBalanceFeature;

public interface IBilrgTataRekPasienBalanceHistoryDal :
    IInsertBulk<BilrgTataRekPasienBalanceHistoryDto>,
    IListData<BilrgTataRekPasienBalanceHistoryDto, IPasienKey>
{
}

public class BilrgTataRekPasienBalanceHistoryDal : IBilrgTataRekPasienBalanceHistoryDal
{
    private readonly DatabaseOptions _opt;

    public BilrgTataRekPasienBalanceHistoryDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<BilrgTataRekPasienBalanceHistoryDto> listModel)
    {
        var rows = listModel.ToList();
        if (rows.Count == 0)
            return;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);

        conn.Open();
        bcp.AddMap("HistoryId", "HistoryId");
        bcp.AddMap("PasienId", "PasienId");
        bcp.AddMap("RegId", "RegId");
        bcp.AddMap("TrsDate", "TrsDate");
        bcp.AddMap("OpeningJasaBalance", "OpeningJasaBalance");
        bcp.AddMap("OpeningObatBalance", "OpeningObatBalance");
        bcp.AddMap("ChargeJasa", "ChargeJasa");
        bcp.AddMap("ChargeObat", "ChargeObat");
        bcp.AddMap("PaymentJasa", "PaymentJasa");
        bcp.AddMap("PaymentObat", "PaymentObat");
        bcp.AddMap("ClosingJasaBalance", "ClosingJasaBalance");
        bcp.AddMap("ClosingObatBalance", "ClosingObatBalance");
        bcp.AddMap("Remarks", "Remarks");
        bcp.AddMap("CreatedAt", "CreatedAt");
        bcp.AddMap("CreatedBy", "CreatedBy");

        bcp.BatchSize = rows.Count;
        bcp.DestinationTableName = "BILRG_TataRekPasienBalanceHistory";
        bcp.WriteToServer(rows.AsDataTable());
    }

    public IEnumerable<BilrgTataRekPasienBalanceHistoryDto> ListData(IPasienKey filter)
    {
        const string sql = """
            SELECT
                HistoryId, PasienId, RegId, TrsDate,
                OpeningJasaBalance, OpeningObatBalance,
                ChargeJasa, ChargeObat, PaymentJasa, PaymentObat,
                ClosingJasaBalance, ClosingObatBalance,
                Remarks, CreatedAt, CreatedBy
            FROM BILRG_TataRekPasienBalanceHistory
            WHERE PasienId = @PasienId
            ORDER BY TrsDate, HistoryId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BilrgTataRekPasienBalanceHistoryDto>(sql, dp);
    }
}
