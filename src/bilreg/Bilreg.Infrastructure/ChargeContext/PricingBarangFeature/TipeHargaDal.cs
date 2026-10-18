using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface ITipeHargaDal :
    IInsert<TipeHargaDto>,
    IUpdate<TipeHargaDto>,
    IDelete<ITipeHargaKey>,
    IGetData<TipeHargaDto, ITipeHargaKey>,
    IListData<TipeHargaDto>
{
}

/// <summary>
/// Data access master tipe harga penjamin pada tabel legacy <c>tb_tipe_barang</c>.
/// </summary>
public class TipeHargaDal : ITipeHargaDal
{
    private readonly DatabaseOptions _opt;

    public TipeHargaDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeHargaDto dto)
    {
        const string sql = """
            INSERT INTO tb_tipe_barang (
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fn_default_profit,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_tax,
                fn_diskon,
                fb_aktif
            ) VALUES (
                @fs_kd_tipe_barang,
                @fs_nm_tipe_barang,
                @fn_default_profit,
                @fn_biaya_per_barang,
                @fn_biaya_per_racik,
                @fn_tax,
                @fn_diskon,
                @fb_aktif
            )
            """;

        var dp = BuildParameters(dto);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeHargaDto dto)
    {
        const string sql = """
            UPDATE tb_tipe_barang
            SET
                fs_nm_tipe_barang = @fs_nm_tipe_barang,
                fn_default_profit = @fn_default_profit,
                fn_biaya_per_barang = @fn_biaya_per_barang,
                fn_biaya_per_racik = @fn_biaya_per_racik,
                fn_tax = @fn_tax,
                fn_diskon = @fn_diskon,
                fb_aktif = @fb_aktif
            WHERE
                fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = BuildParameters(dto);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeHargaKey key)
    {
        const string sql = """
            DELETE FROM tb_tipe_barang
            WHERE fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", key.TipeHargaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeHargaDto GetData(ITipeHargaKey key)
    {
        const string sql = """
            SELECT
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fn_default_profit,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_tax,
                fn_diskon,
                fb_aktif
            FROM tb_tipe_barang
            WHERE fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", key.TipeHargaId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TipeHargaDto>(sql, dp);
    }

    public IEnumerable<TipeHargaDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fn_default_profit,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_tax,
                fn_diskon,
                fb_aktif
            FROM tb_tipe_barang
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeHargaDto>(sql);
    }

    private static DynamicParameters BuildParameters(TipeHargaDto dto)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", dto.fs_kd_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_barang", dto.fs_nm_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fn_default_profit", dto.fn_default_profit, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_per_barang", dto.fn_biaya_per_barang, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_per_racik", dto.fn_biaya_per_racik, SqlDbType.Decimal);
        dp.AddParam("@fn_tax", dto.fn_tax, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        return dp;
    }
}
