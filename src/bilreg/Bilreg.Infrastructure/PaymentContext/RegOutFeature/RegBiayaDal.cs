using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public interface IRegBiayaDal :
    IListData<RegBiayaDto, IRegKey>
{
}

public class RegBiayaDal : IRegBiayaDal
{
    private readonly DatabaseOptions _opt;
    public RegBiayaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public IEnumerable<RegBiayaDto> ListData(IRegKey key)
    {
        const string sql = """
           SELECT
                fs_kd_reg, fs_kd_trs_bl_admin, fn_bl_admin,
                fs_kd_trs_bl_materai, fn_bl_materai,
                fs_kd_trs_bl_bulat_jasa, fn_bl_bulat_jasa,
                fs_kd_trs_bl_bulat_obat, fn_bl_bulat_obat
           FROM
               ta_reg_trs_ref
           WHERE
               fs_kd_reg = @RegId
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegBiayaDto>(sql, dp);
    }
}
