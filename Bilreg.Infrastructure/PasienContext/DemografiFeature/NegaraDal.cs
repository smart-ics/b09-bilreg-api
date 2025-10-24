using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PasienContext.DemografiFeature;

public class NegaraDal : INegaraDal
{
    private readonly DatabaseOptions _opt;

    public NegaraDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(NegaraType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_negara_dk(fs_kd_negara, fs_nm_negara_indonesia, fs_nm_negara_inggris, fs_alpha3, fs_numeric)
            VALUES 
                (@negaraId, @negaraName, @negaraEngName, @alpha3, @negaraNumeric)";

        var dp = new DynamicParameters();
        dp.AddParam("@negaraId", model.NegaraId, SqlDbType.VarChar);
        dp.AddParam("@negaraName", model.NegaraName, SqlDbType.VarChar);
        dp.AddParam("@negaraEngName", model.NegaraEngName, SqlDbType.VarChar);
        dp.AddParam("@alpha3", model.Alpha3, SqlDbType.VarChar);
        dp.AddParam("@negaraNumeric", model.NegaraNumeric, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(NegaraType model)
    {
        const string sql = @"
            UPDATE ta_negara_dk
            SET 
                fs_nm_negara_indonesia = @negaraName,
                fs_nm_negara_inggris = @negaraEngName,
                fs_alpha3 = @alpha3,
                fs_numeric = @negaraNumeric,
            WHERE fs_kd_negara = @negaraId";

        var dp = new DynamicParameters();
        dp.AddParam("@negaraId", model.NegaraId, SqlDbType.VarChar);
        dp.AddParam("@negaraName", model.NegaraName, SqlDbType.VarChar);
        dp.AddParam("@negaraEngName", model.NegaraEngName, SqlDbType.VarChar);
        dp.AddParam("@alpha3", model.Alpha3, SqlDbType.VarChar);
        dp.AddParam("@negaraNumeric", model.NegaraNumeric, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<NegaraType> GetData(INegaraKey key)
    {
        const string sql = @"
            SELECT 
	            fs_kd_negara AS NegaraId, fs_nm_negara_indonesia AS NegaraName,
	            fs_nm_negara_inggris AS NegaraEngName, fs_alpha3 AS Alpha3,
	            fs_numeric AS NegaraNumeric
            FROM 
	            ta_negara_dk 
            WHERE
	            fs_kd_negara = @NegaraId";

        var dp = new DynamicParameters();
        dp.AddParam("@NegaraId", key.NegaraId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<NegaraType>(sql, dp));
    
    }

    public MayBe<IEnumerable<NegaraType>> ListData()
    {
        const string sql = @"
            SELECT 
	            fs_kd_negara AS NegaraId, fs_nm_negara_indonesia AS NegaraName,
	            fs_nm_negara_inggris AS NegaraEngName, fs_alpha3 AS Alpha3,
	            fs_numeric AS NegaraNumeric
            FROM 
	            ta_negara_dk";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.Read<NegaraType>(sql));
    }
}
