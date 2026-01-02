using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public interface ITindakanKomponenDal :
    IInsertBulk<TindakanKomponenDto>,
    IDelete<ITindakanKey>,
    IListData<TindakanKomponenDto, ITindakanKey>
{
}

public class TindakanKomponenDal : ITindakanKomponenDal
{
    private readonly DatabaseOptions _opt;
    public TindakanKomponenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(IEnumerable<TindakanKomponenDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("TindakanId", "TindakanId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("KomponenTarifId", "KomponenTarifId");
        bcp.AddMap("KomponenTarifName", "KomponenTarifName");
        bcp.AddMap("PpaId", "PpaId");
        bcp.AddMap("PpaName", "PpaName");
        bcp.AddMap("Qty", "Qty");
        bcp.AddMap("Nilai", "Nilai");
        bcp.AddMap("SubTotal", "SubTotal");
        
        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_TindakanKomponen";
        bcp.WriteToServer(fetched.AsDataTable());
    }
    
    public void Delete(ITindakanKey key)
    {
        const string sql = """
           DELETE FROM
               BILRG_TindakanKomponen
           WHERE
               TindakanId = @TindakanId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", key.TindakanId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public IEnumerable<TindakanKomponenDto> ListData(ITindakanKey filter)
    {
        const string sql = """
           SELECT
               TindakanId, NoUrut, KomponenTarifId, KomponenTarifName,
               PpaId, PpaName, Qty, Nilai, SubTotal
           FROM
               BILRG_TindakanKomponen
           WHERE
               TindakanId = @TindakanId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@TindakanId", filter.TindakanId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TindakanKomponenDto>(sql, dp);
    }
}