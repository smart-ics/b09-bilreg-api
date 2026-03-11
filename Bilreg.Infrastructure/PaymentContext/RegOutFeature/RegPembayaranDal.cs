using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public interface IRegPembayaranDal :
    IListData<RegPembayaranDto, IRegKey>
{ }

public class RegPembayaranDal : IRegPembayaranDal
{
    private readonly DatabaseOptions _opt;
    public RegPembayaranDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    public IEnumerable<RegPembayaranDto> ListData(IRegKey key)
    {
        const string sql = """
           SELECT 
                'BYDPK' AS fs_kd_bayar, 
                'Bayar Di Muka' AS fs_nm_bayar, 
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
           FROM (SELECT 1 x) d
           LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYDPK'

           UNION ALL

           SELECT 
                'BYDPU' AS fs_kd_bayar,
                'Deposit' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
           FROM (SELECT 1 x) d
           LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYDPU'

           UNION ALL

           SELECT 
                'BYPRI' AS fs_kd_bayar,
                'Hutang Pribadi' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
           FROM (SELECT 1 x) d
           LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYPRI'

           UNION ALL

           SELECT   
                aa.fs_kd_tipe_jaminan AS fs_kd_bayar, 
                aa.fs_nm_tipe_jaminan AS fs_nm_bayar, 
                ISNULL(bb.fn_jasa,0) AS fn_jasa, 
                ISNULL(bb.fn_obat,0) AS fn_obat
           FROM ta_tipe_jaminan aa 
           LEFT JOIN ta_registrasi3 bb ON bb.fs_kd_reg = @RegId AND aa.fs_kd_tipe_jaminan = bb.fs_kd_bayar 
           WHERE aa.fb_subsidi = 1

           UNION ALL

           SELECT   
                aa.fs_kd_bayar AS fs_kd_bayar, 
                bb.fs_nm_tipe_jaminan AS fs_nm_bayar, 
                aa.fn_jasa AS fn_jasa, 
                aa.fn_obat AS fn_obat
           FROM ta_registrasi3 aa 
           INNER JOIN ta_tipe_jaminan bb ON aa.fs_kd_bayar = bb.fs_kd_tipe_jaminan 
           WHERE aa.fs_kd_reg = @RegId AND bb.fb_subsidi = 0

           UNION ALL

           SELECT 
                'BYKAS' AS fs_kd_bayar,
                'Bayar Pribadi' AS fs_nm_bayar,
                ISNULL(a.fn_jasa,0) AS fn_jasa, 
                ISNULL(a.fn_obat,0) AS fn_obat
           FROM (SELECT 1 x) d
           LEFT JOIN ta_registrasi3 a ON a.fs_kd_reg = @RegId AND a.fs_kd_bayar = 'BYKAS'
           """;
        var dp = new DynamicParameters();
        dp.AddParam("@RegId", key.RegId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<RegPembayaranDto>(sql, dp);
    }
}
