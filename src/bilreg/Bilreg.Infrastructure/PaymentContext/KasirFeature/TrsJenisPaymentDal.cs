using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Domain.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsJenisPaymentDal : ITrsJenisPaymentDal
{
    private readonly DatabaseOptions _opt;

    private static readonly string[] StandardCategories =
    [
        KasirConst.MetodeBayarTunai,
        KasirConst.MetodeBayarDebit,
        KasirConst.MetodeBayarKredit,
        KasirConst.MetodeBayarTransfer,
        KasirConst.MetodeBayarQris
    ];

    public TrsJenisPaymentDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<JenisPaymentDto> ListAktif()
    {
        const string sql = """
            SELECT
                RTRIM(fs_kd_jenis_payment) AS fs_kd_jenis_payment,
                RTRIM(fs_nm_jenis_payment) AS fs_nm_jenis_payment,
                RTRIM(fs_kd_rek) AS fs_kd_rek,
                fb_kredit,
                fb_aktif
            FROM ta_jenis_payment
            WHERE fb_aktif = 1
            ORDER BY fs_kd_jenis_payment
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Query<RawJenisPaymentRow>(sql);

        return rows.Select(MapToDto);
    }

    public IEnumerable<JenisPaymentGroupDto> ListAktifGrouped()
    {
        var allItems = ListAktif().ToList();

        return StandardCategories.Select(cat => new JenisPaymentGroupDto(
            Kategori: cat,
            Items: allItems.Where(x => string.Equals(x.Kategori, cat, StringComparison.OrdinalIgnoreCase)).ToList()
        )).ToList();
    }

    public JenisPaymentDto? GetData(string kodeJenisPayment)
    {
        const string sql = """
            SELECT
                RTRIM(fs_kd_jenis_payment) AS fs_kd_jenis_payment,
                RTRIM(fs_nm_jenis_payment) AS fs_nm_jenis_payment,
                RTRIM(fs_kd_rek) AS fs_kd_rek,
                fb_kredit,
                fb_aktif
            FROM ta_jenis_payment
            WHERE fs_kd_jenis_payment = @KodeJenisPayment
            """;

        var dp = new DynamicParameters();
        dp.Add("@KodeJenisPayment", kodeJenisPayment, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var row = conn.QuerySingleOrDefault<RawJenisPaymentRow>(sql, dp);

        return row is not null ? MapToDto(row) : null;
    }

    public static string DetermineKategori(string kode, string nama, bool isKredit)
    {
        var upperNama = (nama ?? string.Empty).ToUpperInvariant();
        var upperKode = (kode ?? string.Empty).ToUpperInvariant();

        if (upperNama.Contains("TUNAI") || upperNama.Contains("KAS ") || upperNama.StartsWith("KAS"))
            return KasirConst.MetodeBayarTunai;

        if (upperNama.Contains("QRIS") || upperKode.Contains("QRIS"))
            return KasirConst.MetodeBayarQris;

        if (upperNama.Contains("TRANSFER") || upperNama.Contains("TRF") || upperNama.Contains("GIRO") || upperKode.StartsWith("GR"))
            return KasirConst.MetodeBayarTransfer;

        if (upperNama.Contains("KREDIT") || upperNama.Contains("CREDIT"))
            return KasirConst.MetodeBayarKredit;

        if (upperNama.Contains("DEBIT"))
            return KasirConst.MetodeBayarDebit;

        if (!isKredit)
            return KasirConst.MetodeBayarKredit;

        return KasirConst.MetodeBayarDebit;
    }

    private static JenisPaymentDto MapToDto(RawJenisPaymentRow row)
    {
        var kode = row.fs_kd_jenis_payment?.Trim() ?? string.Empty;
        var nama = row.fs_nm_jenis_payment?.Trim() ?? string.Empty;
        var coa = row.fs_kd_rek?.Trim() ?? string.Empty;
        var kategori = DetermineKategori(kode, nama, row.fb_kredit);

        return new JenisPaymentDto(
            KodeJenisPayment: kode,
            NamaJenisPayment: nama,
            CoaId: coa,
            IsKredit: row.fb_kredit,
            Kategori: kategori);
    }

    private sealed class RawJenisPaymentRow
    {
        public string fs_kd_jenis_payment { get; set; } = string.Empty;
        public string fs_nm_jenis_payment { get; set; } = string.Empty;
        public string fs_kd_rek { get; set; } = string.Empty;
        public bool fb_kredit { get; set; }
        public bool fb_aktif { get; set; }
    }
}
