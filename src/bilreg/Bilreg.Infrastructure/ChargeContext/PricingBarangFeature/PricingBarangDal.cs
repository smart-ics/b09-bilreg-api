using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using System.Data;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public interface IPricingBarangDal :
    IGetData<PricingBarangDto, IPricingBarangKey>,
    IUpdate<PricingBarangDto>
{
    void UpdateHetAndMetode(string barangId, decimal hetNominal, MetodeGenHnaEnum metodeGenHna);
}

public class PricingBarangDal : IPricingBarangDal
{
    private readonly DatabaseOptions _opt;

    public PricingBarangDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public PricingBarangDto GetData(IPricingBarangKey key)
    {
        const string sql = """
            SELECT
                fs_kd_barang,
                ISNULL(fn_het, 0) AS fn_het,
                ISNULL(fs_metode_gen_hna, '') AS fs_metode_gen_hna
            FROM tb_barang
            WHERE fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", key.BarangId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PricingBarangDto>(sql, dp);
    }

    public void Update(PricingBarangDto dto)
    {
        UpdateHetAndMetode(dto.fs_kd_barang, dto.fn_het, ParseMetodeGenHna(dto.fs_metode_gen_hna));
    }

    public void UpdateHetAndMetode(string barangId, decimal hetNominal, MetodeGenHnaEnum metodeGenHna)
    {
        const string sql = """
            UPDATE tb_barang
            SET
                fn_het = @fn_het,
                fs_metode_gen_hna = @fs_metode_gen_hna
            WHERE
                fs_kd_barang = @fs_kd_barang
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_barang", barangId, SqlDbType.VarChar);
        dp.AddParam("@fn_het", hetNominal, SqlDbType.Decimal);
        dp.AddParam("@fs_metode_gen_hna", ((int)metodeGenHna).ToString(), SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    private static MetodeGenHnaEnum ParseMetodeGenHna(string value) =>
        int.TryParse(value, out var code) && Enum.IsDefined(typeof(MetodeGenHnaEnum), code)
            ? (MetodeGenHnaEnum)code
            : MetodeGenHnaEnum.Terakhir;
}
