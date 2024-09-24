using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BillContext.RoomChargeSub.BangsalAgg;

public class BangsalDal : IBangsalDal
{
    private readonly DatabaseOptions _opt;
    public BangsalDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public void Insert(BangsalModel model)
    {
        const string sql = @"
            INSERT INTO ta_bangsal(
                fs_kd_bangsal, fs_nm_bangsal, fs_kd_layanan)
            VALUES (
                @fs_kd_bangsal, @fs_nm_bangsal, @fs_kd_layanan)";

        var dp = new DynamicParameters();
        dp.AddParam("fs_kd_bangsal", model.BangsalId, SqlDbType.VarChar);
        dp.AddParam("fs_nm_bangsal", model.BangsalName, SqlDbType.VarChar);
        dp.AddParam("fs_kd_layanan", model.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(BangsalModel model)
    {
        const string sql = @"
            UPDATE ta_bangsal
            SET             
                fs_nm_bangsal  = @fs_nm_bangsal, 
                fs_kd_layanan  = @fs_kd_layanan
            WHERE
                fs_kd_bangsal  = @fs_kd_bangsal";

        var dp = new DynamicParameters();
        dp.AddParam("fs_kd_bangsal", model.BangsalId, SqlDbType.VarChar);
        dp.AddParam("fs_nm_bangsal", model.BangsalName, SqlDbType.VarChar);
        dp.AddParam("fs_kd_layanan", model.LayananId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IBangsalKey key)
    {
        const string sql = @"
            DELETE FROM 
               ta_bangsal
            WHERE
                fs_kd_bangsal  = @fs_kd_bangsal";

        var dp = new DynamicParameters();
        dp.AddParam("fs_kd_bangsal", key.BangsalId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public BangsalModel GetData(IBangsalKey key)
    {
        const string sql = @"
            SELECT
                aa.fs_kd_bangsal,
                aa.fs_nm_bangsal,
                aa.fs_kd_layanan,
                ISNULL(bb.fs_nm_layanan,'') fs_nm_layanan
            FROM 
                ta_bangsal aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan
            WHERE
                fs_kd_bangsal  = @fs_kd_bangsal";

        var dp = new DynamicParameters();
        dp.AddParam("fs_kd_bangsal", key.BangsalId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<BangsalDto>(sql, dp);
    }

    public IEnumerable<BangsalModel> ListData()
    {
        const string sql = @"
            SELECT
                aa.fs_kd_bangsal,
                aa.fs_nm_bangsal,
                aa.fs_kd_layanan,
                ISNULL(bb.fs_nm_layanan,'') fs_nm_layanan
            FROM 
                ta_bangsal aa
                LEFT JOIN ta_layanan bb ON aa.fs_kd_layanan = bb.fs_kd_layanan";
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<BangsalDto>(sql);
    }
}

public class BangsalDto() : BangsalModel("","")
{
    public string fs_kd_bangsal { get => BangsalId; set => BangsalId = value; }
    public string fs_nm_bangsal { get => BangsalName; set => BangsalName = value; }
    public string fs_kd_layanan { get => LayananId; set => LayananId = value; }
    public string fs_nm_layanan { get => LayananName; set => LayananName = value; }
    
}
