using System.Data;
using System.Data.SqlClient;
using Dapper;
using Farinv.Domain.InventoryContext.StokFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Infrastructure.InventoryContext.StokFeature;

public interface IJenisLokasiDal :
    IInsert<JenisLokasiType>,
    IUpdate<JenisLokasiType>,
    IDelete<IJenisLokasiKey>,
    IGetData<JenisLokasiType, IJenisLokasiKey>,
    IListData<JenisLokasiType>
{
}

public class JenisLokasiDal : IJenisLokasiDal
{
    private readonly DatabaseOptions _opt;
    public JenisLokasiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(JenisLokasiType dto)
    {
        const string sql = """
            INSERT INTO FARIN_JenisLokasi(
                JenisLokasiId, JenisLokasiName)
            VALUES( 
                @JenisLokasiId, @JenisLokasiName)
            """;
        var dp = new DynamicParameters();
        dp.AddParam("@JenisLokasiId", dto.JenisLokasiId, SqlDbType.VarChar);
        dp.AddParam("@JenisLokasiName", dto.JenisLokasiName, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Update(JenisLokasiType dto)
    {
        const string sql = """
           UPDATE 
               FARIN_JenisLokasi
           SET
               JenisLokasiName = @JenisLokasiName
           WHERE
               JenisLokasiId = @JenisLokasiId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@JenisLokasiId", dto.JenisLokasiId, SqlDbType.VarChar);
        dp.AddParam("@JenisLokasiName", dto.JenisLokasiName, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public void Delete(IJenisLokasiKey key)
    {
        const string sql = """
           DELETE FROM 
                FARIN_JenisLokasi
           WHERE
               JenisLokasiId = @JenisLokasiId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@JenisLokasiId", key.JenisLokasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
    
    public JenisLokasiType GetData(IJenisLokasiKey key)
    {
        const string sql = """
           SELECT
               JenisLokasiId,
               JenisLokasiName
           FROM 
               FARIN_JenisLokasi
           WHERE
               JenisLokasiId = @JenisLokasiId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@JenisLokasiId", key.JenisLokasiId, SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<JenisLokasiType>(sql, dp);
        return result;
    }
    
    public IEnumerable<JenisLokasiType> ListData()
    {
        const string sql = """
            SELECT
                JenisLokasiId,
                JenisLokasiName
            FROM 
                FARIN_JenisLokasi
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<JenisLokasiType>(sql);
    }
}