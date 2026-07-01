using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public interface IRegHutangDal :
    IListData<RegHutangDto, IPasienKey>
{
}

public class RegHutangDal : IRegHutangDal
{
    private readonly DatabaseOptions _opt;
    public RegHutangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public IEnumerable<RegHutangDto> ListData(IPasienKey key)
    {
        const string sql = """
           SELECT
               LEFT(fs_keterangan, 10) fs_kd_reg, fd_tgl_piutang,
               fn_piutang, fn_sisa, fn_nilai_jasa, fn_nilai_obat
           FROM
               t_bp_piutang_hdr
           WHERE
               fd_tgl_piutang <= @TglNow
               AND fd_tgl_void = '3000-01-01'
               AND fn_sisa > 0
               AND fs_kd_mr = @PasienId
               AND fs_kd_iii = 'JAMINAN000'
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@TglNow", DateTime.Now.ToString(DateFormatEnum.YMD), SqlDbType.VarChar);
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegHutangDto>(sql, dp);
    }
}
