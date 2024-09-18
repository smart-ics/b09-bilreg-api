using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.GrupTarifDkAgg;
using Bilreg.Domain.BillContext.TindakanSub.GrupTarifDkAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.GrupTarifDkAgg;

public class GrupTarifDkDal:IGrupTarifDkDal
{
    private readonly DatabaseOptions _opt;

    public GrupTarifDkDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public GrupTarifDkModel GetData(IGrupTarifDkKey key)
    {
        const string sql = @"
            SELECT
                fs_kd_grup_tarif_dk,
                fs_nm_grup_tarif_dk
            FROM
                ta_grup_tarif_dk
            WHERE 
                fs_kd_grup_tarif_dk = @fs_kd_grup_tarif_dk
        ";
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_grup_tarif_dk",key.GrupTarifDkId,SqlDbType.VarChar);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<GrupTarifDkDto>(sql, dp);
    }

    public IEnumerable<GrupTarifDkModel> ListData()
    {
        const string sql = @"
        SELECT
            fs_kd_grup_tarif_dk,
            fs_nm_grup_tarif_dk
        FROM
            ta_grup_tarif_dk;
        ";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<GrupTarifDkDto>(sql);
    }
    
    public class GrupTarifDkDto:GrupTarifDkModel
    {
        public GrupTarifDkDto() : base(
            string.Empty, string.Empty
        )
        {
        }
        public string fs_kd_grup_tarif_dk { get => GrupTarifDkId; set => GrupTarifDkId = value; }
        public string fs_nm_grup_tarif_dk { get => GrupTarifDkName; set => GrupTarifDkName = value; }
        
    }
}