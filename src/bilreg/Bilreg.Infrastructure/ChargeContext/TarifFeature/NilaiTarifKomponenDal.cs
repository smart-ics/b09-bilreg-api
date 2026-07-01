using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface INilaiTarifKompDal:
    IInsertBulk<NilaiTarifKompDto>,
    IDelete<INilaiTarifKey>,
    IListData<NilaiTarifKompDto, INilaiTarifKey>
{
    void Clear();

    IEnumerable<NilaiTarifKompDto> ListAll();
}

public class NilaiTarifKompDal : INilaiTarifKompDal
{
    private readonly DatabaseOptions _opt;

    public NilaiTarifKompDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<NilaiTarifKompDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("NilaiTarifId", "NilaiTarifId");
        bcp.AddMap("NoUrut", "NoUrut");
        bcp.AddMap("KomponenId", "KomponenId");
        bcp.AddMap("Nilai", "Nilai");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILRG_NilaiTarifKomponen";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(INilaiTarifKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_NilaiTarifKomponen
            WHERE
                NilaiTarifId = @NilaiTarifId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", key.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);    
    }

    public void Clear()
    {
        const string sql = "DELETE FROM BILRG_NilaiTarifKomponen";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql);    
    }
    public IEnumerable<NilaiTarifKompDto> ListData(INilaiTarifKey filter)
    {
        const string sql = """
            SELECT 
                aa.NilaiTarifId, aa.NoUrut, aa.KomponenId, aa.Nilai,
                ISNULL(bb.fs_nm_detil_tarif, '') AS KomponenName
            FROM 
                BILRG_NilaiTarifKomponen aa
                LEFT JOIN ta_detil_tarif bb ON aa.KomponenId = bb.fs_kd_detil_tarif
            WHERE
                aa.NilaiTarifId = @NilaiTarifId
            ORDER BY aa.NoUrut
            """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@NilaiTarifId", filter.NilaiTarifId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifKompDto>(sql, dp);
    }

    public IEnumerable<NilaiTarifKompDto> ListAll()
    {
        const string sql = """
            SELECT
                aa.NilaiTarifId, aa.NoUrut, aa.KomponenId, aa.Nilai,
                ISNULL(bb.fs_nm_detil_tarif, '') AS KomponenName
            FROM BILRG_NilaiTarifKomponen aa
                LEFT JOIN ta_detil_tarif bb ON aa.KomponenId = bb.fs_kd_detil_tarif
            ORDER BY aa.NilaiTarifId, aa.NoUrut
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<NilaiTarifKompDto>(sql);
    }
}