using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.JaminanFeature.CaraBayarDkAgg;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.JaminanFeature.CaraBayarDkAgg;

public class CaraBayarDkDal : ICaraBayarDkDal
{
    private readonly DatabaseOptions _opt;

    public CaraBayarDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    
    public CaraBayarDkType GetData(ICaraBayarDkKey key)
    {
        // QUERY
        const string sql = @"
             SELECT 
                 fs_kd_cara_bayar_dk AS CaraBayarDkId, 
                 fs_nm_cara_bayar_dk AS CaraBayarDkName
             FROM 
                 ta_cara_bayar_dk
             WHERE 
                 fs_kd_cara_bayar_dk = @fs_kd_cara_bayar_dk";

        // PARAM
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_cara_bayar_dk", key.CaraBayarDkId, SqlDbType.VarChar);

        // EXECUTE
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<CaraBayarDkType>(sql, dp);

    }

    public IEnumerable<CaraBayarDkType> ListData()
    {
        // QUERY
        const string sql = @"
             SELECT 
                 fs_kd_cara_bayar_dk AS CaraBayarDkId, 
                 fs_nm_cara_bayar_dk AS CaraBayarDkName
             FROM 
                 ta_cara_bayar_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<CaraBayarDkType>(sql);
        
    }
}
