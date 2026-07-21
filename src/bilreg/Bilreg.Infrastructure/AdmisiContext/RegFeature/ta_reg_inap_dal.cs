// ReSharper disable InconsistentNaming

using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RegFeature;

public interface Ita_reg_inap_dal :
    IInsert<ta_reg_inap_dto>,
    IUpdate<ta_reg_inap_dto>,
    IDelete<IRegKey>,
    IGetData<ta_reg_inap_dto, IRegKey>
{
}

public class ta_reg_inap_dal : Ita_reg_inap_dal
{
    private readonly DatabaseOptions _opt;

    public ta_reg_inap_dal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(ta_reg_inap_dto model)
    {
        const string sql = """
           INSERT INTO ta_reg_inap (
               fs_kd_reg, fs_kd_caramasuk_inap,
               fs_kd_trs_booking_bed, fs_kd_medis_sekunder)
           VALUES (
               @fs_kd_reg, @fs_kd_caramasuk_inap,
               @fs_kd_trs_booking_bed, @fs_kd_medis_sekunder)
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", model.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_caramasuk_inap", model.fs_kd_caramasuk_inap, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_booking_bed", model.fs_kd_trs_booking_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis_sekunder", model.fs_kd_medis_sekunder, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(ta_reg_inap_dto model)
    {
        const string sql = """
           UPDATE ta_reg_inap
           SET
               fs_kd_caramasuk_inap = @fs_kd_caramasuk_inap,
               fs_kd_trs_booking_bed = @fs_kd_trs_booking_bed,
               fs_kd_medis_sekunder = @fs_kd_medis_sekunder
           WHERE
               fs_kd_reg = @fs_kd_reg
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", model.fs_kd_reg, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_caramasuk_inap", model.fs_kd_caramasuk_inap, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_booking_bed", model.fs_kd_trs_booking_bed, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_medis_sekunder", model.fs_kd_medis_sekunder, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IRegKey regKey)
    {
        const string sql = """
           DELETE FROM ta_reg_inap
           WHERE fs_kd_reg = @fs_kd_reg
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", regKey.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public ta_reg_inap_dto GetData(IRegKey key)
    {
        const string sql = """
            SELECT
                fs_kd_reg,
                fs_kd_caramasuk_inap,
                fs_kd_trs_booking_bed,
                fs_kd_medis_sekunder
            FROM ta_reg_inap
            WHERE fs_kd_reg = @fs_kd_reg
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<ta_reg_inap_dto>(sql, dp);
    }
}
