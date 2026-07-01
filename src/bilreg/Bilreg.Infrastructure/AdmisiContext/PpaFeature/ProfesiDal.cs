using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.PpaFeature;

public interface IProfesiDal :
    IInsert<ProfesiType>,
    IUpdate<ProfesiType>,
    IDelete<IProfesiKey>,
    IGetData<ProfesiType, IProfesiKey>,
    IListData<ProfesiType>
{
}

public class ProfesiDal : IProfesiDal
{
    private readonly DatabaseOptions _opt;

    public ProfesiDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(ProfesiType dto)
    {
        const string sql = """
            INSERT INTO BILRG_Profesi(
                ProfesiId, ProfesiName)
            VALUES( 
                @ProfesiId, @ProfesiName)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", dto.ProfesiId, SqlDbType.VarChar);
        dp.AddParam("@ProfesiName", dto.ProfesiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ProfesiType dto)
    {
        const string sql = @"
           UPDATE 
               BILRG_Profesi
           SET
               ProfesiName = @ProfesiName
           WHERE
               ProfesiId = @ProfesiId";

        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", dto.ProfesiId, SqlDbType.VarChar);
        dp.AddParam("@ProfesiName", dto.ProfesiName, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IProfesiKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_Profesi
           WHERE
               ProfesiId = @ProfesiId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", key.ProfesiId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ProfesiType GetData(IProfesiKey key)
    {
        const string sql = @"
           SELECT
               ProfesiId,
               ProfesiName
           FROM 
               BILRG_Profesi
           WHERE
               ProfesiId = @ProfesiId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@ProfesiId", key.ProfesiId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<ProfesiType>(sql, dp);
        return result;
    }

    public IEnumerable<ProfesiType> ListData()
    {
        const string sql = """
            SELECT
                ProfesiId,
                ProfesiName
            FROM 
                BILRG_Profesi
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ProfesiType>(sql);
    }
}
