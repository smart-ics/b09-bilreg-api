using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsKasirItemDal : ITrsKasirItemDal
{
    private readonly DatabaseOptions _opt;

    public TrsKasirItemDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TrsKasirItemDto item)
    {
        Insert([item]);
    }

    public void Insert(IEnumerable<TrsKasirItemDto> items)
    {
        var list = items?.Select(item => new
        {
            KasirItemId = item.KasirItemId,
            TrsKasirId = item.TrsKasirId,
            NoUrut = item.NoUrut,
            BillId = item.BillId,
            RegId = item.RegId,
            LayananId = item.LayananId,
            Keterangan = item.Keterangan ?? string.Empty,
            NilaiBill = item.NilaiBill,
            NilaiBayar = item.NilaiBayar,
            crttgl = string.IsNullOrWhiteSpace(item.crttgl) ? DateTime.Today.ToString("yyyy-MM-dd") : item.crttgl,
            crtjam = string.IsNullOrWhiteSpace(item.crtjam) ? DateTime.Now.ToString("HH:mm:ss") : item.crtjam,
            crtusr = item.crtusr ?? string.Empty
        }).ToList();

        if (list == null || list.Count == 0)
            return;

        const string sql = """
            INSERT INTO BILRG_TrsKasirItem (
                KasirItemId, TrsKasirId, NoUrut, BillId, RegId, LayananId,
                Keterangan, NilaiBill, NilaiBayar, crttgl, crtjam, crtusr
            ) VALUES (
                @KasirItemId, @TrsKasirId, @NoUrut, @BillId, @RegId, @LayananId,
                @Keterangan, @NilaiBill, @NilaiBayar, @crttgl, @crtjam, @crtusr
            )
            """;

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, list);
    }

    public IEnumerable<TrsKasirItemDto> ListDataByTrsKasir(string trsKasirId)
    {
        const string sql = """
            SELECT
                RTRIM(KasirItemId) AS KasirItemId,
                RTRIM(TrsKasirId) AS TrsKasirId,
                NoUrut,
                RTRIM(BillId) AS BillId,
                RTRIM(RegId) AS RegId,
                RTRIM(LayananId) AS LayananId,
                RTRIM(Keterangan) AS Keterangan,
                NilaiBill,
                NilaiBayar,
                RTRIM(crttgl) AS crttgl,
                RTRIM(crtjam) AS crtjam,
                RTRIM(crtusr) AS crtusr
            FROM BILRG_TrsKasirItem WITH (NOLOCK)
            WHERE TrsKasirId = @TrsKasirId
            ORDER BY NoUrut ASC
            """;

        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", trsKasirId, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<TrsKasirItemDto>(sql, dp);
    }

    public IEnumerable<string> ListPaidBillIdsByReg(string regId)
    {
        const string sql = """
            SELECT DISTINCT RTRIM(ki.BillId) AS BillId
            FROM BILRG_TrsKasirItem ki WITH (NOLOCK)
            INNER JOIN BILRG_TrsKasir k WITH (NOLOCK) ON ki.TrsKasirId = k.TrsKasirId
            WHERE ki.RegId = @RegId
              AND k.StatusTrs = 'BERHASIL'
            """;

        var dp = new DynamicParameters();
        dp.Add("@RegId", regId, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<string>(sql, dp);
    }

    public IEnumerable<BillingUnpaidItemDto> ListUnpaidBillingByReg(string regId)
    {
        if (string.IsNullOrWhiteSpace(regId))
            return Enumerable.Empty<BillingUnpaidItemDto>();

        const string sql = """
            SELECT
                RTRIM(b.fs_kd_trs) AS BillId,
                RTRIM(b.fs_kd_reg) AS RegId,
                RTRIM(b.fs_kd_layanan) AS LayananId,
                ISNULL(RTRIM(lay.fs_nm_layanan), '') AS LayananName,
                RTRIM(b.fs_keterangan) AS Keterangan,
                b.fn_total AS Nilai,
                RTRIM(b.fd_tgl_trs) AS TglTrs,
                RTRIM(b.fs_jam_trs) AS JamTrs
            FROM ta_trs_billing b WITH (NOLOCK)
            LEFT JOIN ta_layanan lay WITH (NOLOCK) ON b.fs_kd_layanan = lay.fs_kd_layanan
            WHERE b.fs_kd_reg = @RegId
              AND b.fn_total > 0
              AND NOT EXISTS (
                  SELECT 1 FROM BILRG_TrsKasirItem ki WITH (NOLOCK)
                  INNER JOIN BILRG_TrsKasir k WITH (NOLOCK) ON ki.TrsKasirId = k.TrsKasirId
                  WHERE ki.BillId = b.fs_kd_trs
                    AND k.StatusTrs = 'BERHASIL'
              )
              AND (
                  SELECT ISNULL(SUM(k.TotalNilai), 0)
                  FROM BILRG_TrsKasir k WITH (NOLOCK)
                  WHERE k.RegId = b.fs_kd_reg
                    AND k.JenisTrs = 'TERIMA'
                    AND k.StatusTrs = 'BERHASIL'
              ) < (
                  SELECT ISNULL(SUM(b_all.fn_total), 0)
                  FROM ta_trs_billing b_all WITH (NOLOCK)
                  WHERE b_all.fs_kd_reg = b.fs_kd_reg
                    AND b_all.fn_total > 0
              )
            ORDER BY b.fd_tgl_trs ASC, b.fs_jam_trs ASC, b.fs_kd_trs ASC
            """;

        var dp = new DynamicParameters();
        dp.Add("@RegId", regId, DbType.AnsiString);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<BillingUnpaidItemDto>(sql, dp) ?? Enumerable.Empty<BillingUnpaidItemDto>();
    }

    public void DeleteByTrsKasir(string trsKasirId)
    {
        const string sql = "DELETE FROM BILRG_TrsKasirItem WHERE TrsKasirId = @TrsKasirId";
        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", trsKasirId, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }
}
