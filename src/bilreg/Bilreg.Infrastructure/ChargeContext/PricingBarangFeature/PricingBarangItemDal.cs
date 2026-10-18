using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IPricingBarangItemDal :
    IListData<ItemPricingVariantDto>,
    IListData<ItemPricingVariantDto, IPricingBarangKey>,
    IDelete<IPricingBarangKey>
{
    void SaveBatch(string barangId, IEnumerable<ItemPricingVariantDto> items);
}

/// <summary>
/// Data access matriks varian harga per tipe penjamin pada tabel legacy <c>tb_barang_tipe</c>.
/// Composite key: <c>fs_kd_barang</c> + <c>fs_kd_tipe_harga</c>.
/// </summary>
public class PricingBarangItemDal : IPricingBarangItemDal
{
    private readonly DatabaseOptions _opt;

    public PricingBarangItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void SaveBatch(string barangId, IEnumerable<ItemPricingVariantDto> items)
    {
        var list = items?.ToList() ?? [];

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));

        if (list.Count == 0)
        {
            const string deleteAllSql = """
                DELETE FROM tb_barang_tipe
                WHERE fs_kd_barang = @fs_kd_barang
                """;

            conn.Execute(deleteAllSql, new { fs_kd_barang = barangId });
            return;
        }

        const string deleteOrphansSql = """
            DELETE FROM tb_barang_tipe
            WHERE fs_kd_barang = @fs_kd_barang
              AND fs_kd_tipe_harga NOT IN @keep_keys
            """;

        var keepKeys = list.Select(x => x.fs_kd_tipe_harga).ToList();
        conn.Execute(deleteOrphansSql, new { fs_kd_barang = barangId, keep_keys = keepKeys });

        const string updateSql = """
            UPDATE tb_barang_tipe
            SET
                fn_harga_jual = @fn_harga_jual,
                fn_profit = @fn_profit,
                fn_biaya = @fn_biaya,
                fn_tax = @fn_tax,
                fn_diskon = @fn_diskon,
                fb_jual = @fb_jual
            WHERE
                fs_kd_barang = @fs_kd_barang
                AND fs_kd_tipe_harga = @fs_kd_tipe_harga
            """;

        const string insertSql = """
            INSERT INTO tb_barang_tipe (
                fs_kd_barang,
                fs_kd_tipe_harga,
                fn_harga_jual,
                fn_profit,
                fn_biaya,
                fn_tax,
                fn_diskon,
                fb_jual
            ) VALUES (
                @fs_kd_barang,
                @fs_kd_tipe_harga,
                @fn_harga_jual,
                @fn_profit,
                @fn_biaya,
                @fn_tax,
                @fn_diskon,
                @fb_jual
            )
            """;

        foreach (var item in list)
        {
            var dp = BuildParameters(item);
            var affected = conn.Execute(updateSql, dp);
            if (affected == 0)
                conn.Execute(insertSql, dp);
        }
    }

    public IEnumerable<ItemPricingVariantDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                fs_kd_tipe_harga,
                fn_harga_jual,
                fn_profit,
                fn_biaya,
                fn_tax,
                fn_diskon,
                fb_jual
            FROM tb_barang_tipe
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ItemPricingVariantDto>(sql);
    }

    public IEnumerable<ItemPricingVariantDto> ListData(IPricingBarangKey key)
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                fs_kd_tipe_harga,
                fn_harga_jual,
                fn_profit,
                fn_biaya,
                fn_tax,
                fn_diskon,
                fb_jual
            FROM tb_barang_tipe
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BarangId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<ItemPricingVariantDto>(sql, dp);
    }

    public void Delete(IPricingBarangKey key)
    {
        const string sql = """
            DELETE FROM tb_barang_tipe
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BarangId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    private static DynamicParameters BuildParameters(ItemPricingVariantDto item)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", item.fs_kd_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tipe_harga", item.fs_kd_tipe_harga, SqlDbType.VarChar);
        dp.AddParam("@fn_harga_jual", item.fn_harga_jual, SqlDbType.Decimal);
        dp.AddParam("@fn_profit", item.fn_profit, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya", item.fn_biaya, SqlDbType.Decimal);
        dp.AddParam("@fn_tax", item.fn_tax, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", item.fn_diskon, SqlDbType.Decimal);
        dp.AddParam("@fb_jual", item.fb_jual, SqlDbType.Bit);
        return dp;
    }
}
