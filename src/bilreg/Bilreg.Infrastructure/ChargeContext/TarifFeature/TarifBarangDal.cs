using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public interface ITarifBarangDal :
    IListData<TarifBarangDto, ITarifKey>
{
}

public class TarifBarangDal : ITarifBarangDal
{
    private readonly DatabaseOptions _opt;

    public TarifBarangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<TarifBarangDto> ListData(ITarifKey filter)
    {
        const string sql = @"
            SELECT 
                a.fs_kd_tarif,
                a.fn_no_urut,
                a.fs_kd_barang,
                ISNULL(b.fs_nm_barang, '') AS fs_nm_barang,
                a.fs_kd_satuan,
                ISNULL(c.fs_nm_satuan, '') AS fs_nm_satuan,
                a.fn_qty_barang
            FROM 
                TA_TARIF7 a
                LEFT JOIN tb_barang b ON a.fs_kd_barang = b.fs_kd_barang
                LEFT JOIN tb_satuan c ON a.fs_kd_satuan = c.fs_kd_satuan
            WHERE 
                a.fs_kd_tarif = @fs_kd_tarif
            ORDER BY 
                a.fn_no_urut ASC";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tarif", filter.TarifId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TarifBarangDto>(sql, dp) ?? [];
    }
}
