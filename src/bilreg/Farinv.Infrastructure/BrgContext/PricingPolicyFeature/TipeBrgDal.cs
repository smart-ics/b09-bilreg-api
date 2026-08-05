using Dapper;
using Farinv.Domain.BrgContext.PricingPolicyFeature;
using Farinv.Infrastructure.Helpers;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Farinv.Infrastructure.BrgContext.PricingPolicyFeature;

public interface ITipeBrgDal :
    IInsert<TipeBrgDto>,
    IUpdate<TipeBrgDto>,
    IDelete<ITipeBrgKey>,
    IGetData<TipeBrgDto, ITipeBrgKey>,
    IListData<TipeBrgDto>
{
}

public class TipeBrgDal : ITipeBrgDal
{
    private readonly DatabaseOptions _opt;

    public TipeBrgDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TipeBrgDto dto)
    {
        const string sql = """
            INSERT INTO tb_tipe_barang (
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fb_aktif,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_profit,
                fn_tax,
                fn_diskon
            ) VALUES (
                @fs_kd_tipe_barang,
                @fs_nm_tipe_barang,
                @fb_aktif,
                @fn_biaya_per_barang,
                @fn_biaya_per_racik,
                @fn_profit,
                @fn_tax,
                @fn_diskon
            )
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", dto.fs_kd_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_barang", dto.fs_nm_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fn_biaya_per_barang", dto.fn_biaya_per_barang, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_per_racik", dto.fn_biaya_per_racik, SqlDbType.Decimal);
        dp.AddParam("@fn_profit", dto.fn_profit, SqlDbType.Decimal);
        dp.AddParam("@fn_tax", dto.fn_tax, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TipeBrgDto dto)
    {
        const string sql = """
            UPDATE tb_tipe_barang
            SET
                fs_nm_tipe_barang = @fs_nm_tipe_barang,
                fb_aktif = @fb_aktif,
                fn_biaya_per_barang = @fn_biaya_per_barang,
                fn_biaya_per_racik = @fn_biaya_per_racik,
                fn_profit = @fn_profit,
                fn_tax = @fn_tax,
                fn_diskon = @fn_diskon
            WHERE
                fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", dto.fs_kd_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_tipe_barang", dto.fs_nm_tipe_barang, SqlDbType.VarChar);
        dp.AddParam("@fb_aktif", dto.fb_aktif, SqlDbType.Bit);
        dp.AddParam("@fn_biaya_per_barang", dto.fn_biaya_per_barang, SqlDbType.Decimal);
        dp.AddParam("@fn_biaya_per_racik", dto.fn_biaya_per_racik, SqlDbType.Decimal);
        dp.AddParam("@fn_profit", dto.fn_profit, SqlDbType.Decimal);
        dp.AddParam("@fn_tax", dto.fn_tax, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", dto.fn_diskon, SqlDbType.Decimal);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITipeBrgKey key)
    {
        const string sql = """
            DELETE FROM tb_tipe_barang
            WHERE fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", key.TipeBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TipeBrgDto GetData(ITipeBrgKey key)
    {
        const string sql = """
            SELECT
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fb_aktif,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_profit,
                fn_tax,
                fn_diskon
            FROM tb_tipe_barang
            WHERE fs_kd_tipe_barang = @fs_kd_tipe_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_tipe_barang", key.TipeBrgId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TipeBrgDto>(sql, dp);
    }

    public IEnumerable<TipeBrgDto> ListData()
    {
        const string sql = """
            SELECT
                fs_kd_tipe_barang,
                fs_nm_tipe_barang,
                fb_aktif,
                fn_biaya_per_barang,
                fn_biaya_per_racik,
                fn_profit,
                fn_tax,
                fn_diskon
            FROM tb_tipe_barang
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<TipeBrgDto>(sql);
    }
}
