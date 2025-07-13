using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.TindakanSub.KomponenTarifAgg;
using Bilreg.Domain.BillContext.TindakanSub.TarifFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.TindakanSub.KomponenTarifAgg;

public class KomponenTarifDal: IKomponenTarifDal
{
    private readonly DatabaseOptions _opt;

    public KomponenTarifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(KomponenType type)
    {
        const string sql = @"
            INSERT INTO ta_detil_tarif (fs_kd_detil_tarif, fs_nm_detil_tarif)
            VALUES(@fs_kd_detil_tarif, @fs_nm_detil_tarif)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", type.KomponenId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", type.KomponenName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KomponenType type)
    {
        const string sql = @"
            UPDATE
                ta_detil_tarif
            SET
                fs_nm_detil_tarif = @fs_nm_detil_tarif
            WHERE
                fs_kd_detil_tarif = @fs_kd_detil_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", type.KomponenId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_detil_tarif", type.KomponenName, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKomponenKey key)
    {
        const string sql = @"
            DELETE FROM 
                ta_detil_tarif
            WHERE
                fs_kd_detil_tarif = @fs_kd_detil_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public KomponenType GetData(IKomponenKey key)
    {
        const string sql = @"
            SELECT fs_kd_detil_tarif, fs_nm_detil_tarif
            FROM 
                ta_detil_tarif
            WHERE
                fs_kd_detil_tarif = @fs_kd_detil_tarif";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_detil_tarif", key.KomponenId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<KomponenDto>(sql, dp);
        return result;
    }

    public IEnumerable<KomponenType> ListData()
    {
        const string sql = @"
            SELECT fs_kd_detil_tarif, fs_nm_detil_tarif
            FROM ta_detil_tarif";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.Read<KomponenDto>(sql);
        return result;
    }
}

public class KomponenDto() : KomponenType(string.Empty, String.Empty)
{
    public string fs_kd_detil_tarif { get => KomponenId; set => KomponenId = value; }
    public string fs_nm_detil_tarif { get => KomponenName; set => KomponenName = value; }
}