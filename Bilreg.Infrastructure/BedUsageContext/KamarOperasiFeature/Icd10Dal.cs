using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IIcd10Dal :
    IInsert<Icd10Dto>,
    IUpdate<Icd10Dto>,
    IDelete<IIcd10Key>,
    IGetData<Icd10Dto, IIcd10Key>,
    IListData<Icd10Dto>
{
}

public class Icd10Dal : IIcd10Dal
{
    private readonly DatabaseOptions _opt;

    public Icd10Dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(Icd10Dto model)
    {
        const string sql = @"
            INSERT INTO tc_icd(
                fs_kd_icd, fs_ket_icd
            VALUES (
                @fs_kd_icd, @fs_ket_icd
            )";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_icd, ", model.fs_kd_icd, SqlDbType.VarChar);
        dp.AddParam("@fs_ket_icd", model.fs_ket_icd, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(Icd10Dto model)
    {
        const string sql = @"
            UPDATE
                tc_icd
            SET
                fs_ket_icd = @fs_ket_icd
            WHERE
                fs_kd_icd = @fs_kd_icd";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_icd, ", model.fs_kd_icd, SqlDbType.VarChar);
        dp.AddParam("@fs_ket_icd", model.fs_ket_icd, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IIcd10Key key)
    {
        const string sql = @"
            DELETE FROM tc_icd
            WHERE fs_kd_icd = @fs_kd_icd";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_icd", key.Icd10Id, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public Icd10Dto GetData(IIcd10Key key)
    {
        const string sql = @"
            SELECT fs_kd_icd, fs_ket_icd
            FROM tc_icd
            WHERE fs_kd_icd = @fs_kd_icd";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_icd", key.Icd10Id, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<Icd10Dto>(sql, dp);
    }

    public IEnumerable<Icd10Dto> ListData()
    {
        const string sql = @"
            SELECT fs_kd_icd, fs_ket_icd
            FROM tc_icd
            WHERE fs_kd_icd = @fs_kd_icd";

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<Icd10Dto>(sql);
    }
}
