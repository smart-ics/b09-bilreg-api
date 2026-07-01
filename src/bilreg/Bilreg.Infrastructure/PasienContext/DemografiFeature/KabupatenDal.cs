using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Infrastructure.PasienContext.DemografiSub.KabupatenAgg;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.PasienContext.DemografiFeature;

public class KabupatenDal : IKabupatenDal
{
    private readonly DatabaseOptions _opt;

    public KabupatenDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(KabupatenType model)
    {
        const string sql = @"
            INSERT INTO 
                ta_kabupaten(fs_kd_kabupaten, fs_nm_kabupaten, fs_kd_propinsi)
            VALUES 
                (@fs_kd_kabupaten, @fs_nm_kabupaten, @fs_kd_propinsi)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", model.KabupatenId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kabupaten", model.KabupatenName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_propinsi", model.Propinsi.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(KabupatenType model)
    {
        const string sql = @"
            UPDATE ta_kabupaten
            SET fs_nm_kabupaten = @fs_nm_kabupaten,
                fs_kd_propinsi = @fs_kd_propinsi
            WHERE fs_kd_kabupaten = @fs_kd_kabupaten";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", model.KabupatenId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_kabupaten", model.KabupatenName, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_propinsi", model.Propinsi.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IKabupatenKey key)
    {
        const string sql = @"
            DELETE FROM ta_kabupaten
            WHERE fs_kd_kabupaten = @fs_kd_kabupaten";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", key.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<KabupatenType> GetData(IKabupatenKey key)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kabupaten, aa.fs_nm_kabupaten, aa.fs_kd_propinsi,
                ISNULL(bb.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kabupaten aa
                LEFT JOIN ta_propinsi bb ON aa.fs_kd_propinsi = bb.fs_kd_propinsi
            WHERE fs_kd_kabupaten = @fs_kd_kabupaten";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_kabupaten", key.KabupatenId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.ReadSingle<KabupatenDto>(sql, dp))
            .Map(x => x.ToModel());
    }

    public MayBe<IEnumerable<KabupatenType>> ListData(IPropinsiKey filter)
    {
        const string sql = @"
            SELECT 
                aa.fs_kd_kabupaten, aa.fs_nm_kabupaten, aa.fs_kd_propinsi,
                ISNULL(bb.fs_nm_propinsi, '-') fs_nm_propinsi
            FROM 
                ta_kabupaten aa
                LEFT JOIN ta_propinsi bb ON aa.fs_kd_propinsi = bb.fs_kd_propinsi
            WHERE 
                aa.fs_kd_propinsi = @fs_kd_propinsi";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_propinsi", filter.PropinsiId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe
            .From(conn.Read<KabupatenDto>(sql, dp))
            .Map(x => x.Select(y => y.ToModel()));
    }

}
