using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PaymentContext.KasirFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.PaymentContext.KasirFeature;

public class TrsKasirLegacyDal : ITrsKasirLegacyDal
{
    private readonly DatabaseOptions _opt;

    public TrsKasirLegacyDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(TrsKasirLegacyHdrDto hdr, IEnumerable<TrsKasirLegacyDtlDto> details)
    {
        if (hdr == null)
            throw new ArgumentNullException(nameof(hdr));

        var trsKasirId = Truncate(hdr.TrsKasirId, 10);
        var tglTrs = string.IsNullOrWhiteSpace(hdr.TglTrs) ? DateTime.Today.ToString("yyyy-MM-dd") : hdr.TglTrs;
        var jamTrs = string.IsNullOrWhiteSpace(hdr.JamTrs) ? DateTime.Now.ToString("HH:mm:ss") : hdr.JamTrs;
        var tglJamTrs = string.IsNullOrWhiteSpace(hdr.TglJamTrs) ? $"{tglTrs} {jamTrs}" : Truncate(hdr.TglJamTrs, 19);
        var crtTgl = string.IsNullOrWhiteSpace(hdr.CrtTgl) ? tglTrs : hdr.CrtTgl;
        var crtJam = string.IsNullOrWhiteSpace(hdr.CrtJam) ? jamTrs : hdr.CrtJam;
        var crtUsr = Truncate(string.IsNullOrWhiteSpace(hdr.CrtUsr) ? hdr.PetugasId ?? string.Empty : hdr.CrtUsr, 10);
        var updTgl = string.IsNullOrWhiteSpace(hdr.UpdTgl) ? crtTgl : hdr.UpdTgl;
        var updJam = string.IsNullOrWhiteSpace(hdr.UpdJam) ? crtJam : hdr.UpdJam;
        var updUsr = Truncate(string.IsNullOrWhiteSpace(hdr.UpdUsr) ? crtUsr : hdr.UpdUsr, 10);

        const string sqlHdr = """
            INSERT INTO ta_trs_deposit_khusus (
                fs_kd_trs, fd_tgl_trs, fs_jam_trs, fs_kd_petugas,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_reg, fn_sum_kas, fn_sum_bank, fn_sum_kartu_kredit, fn_sum_all,
                fs_kd_rek_kas, fs_kd_rek_bank, fs_kd_rek_kartu_kredit, fs_no_kartu_kredit,
                fs_kd_trs_kwitansi, fs_kd_trs_charge, fs_kd_trs_disckredit, fs_kd_trs_chargebank, fs_kd_trs_discbank,
                fs_kd_jenis_bank, fs_kd_jenis_kartu_kredit, fs_no_bank, fs_kd_rek_deposit,
                fn_genkeu, fn_status_bridge, fn_sum_voucher_dep,
                CRTTGL, CRTJAM, CRTIPA, CRTVER, CRTUSR,
                UPDTGL, UPDJAM, UPDIPA, UPDVER, UPDUSR,
                fd_tgl_jam_trs
            ) VALUES (
                @fs_kd_trs, @fd_tgl_trs, @fs_jam_trs, @fs_kd_petugas,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fs_kd_reg, @fn_sum_kas, @fn_sum_bank, @fn_sum_kartu_kredit, @fn_sum_all,
                @fs_kd_rek_kas, @fs_kd_rek_bank, @fs_kd_rek_kartu_kredit, @fs_no_kartu_kredit,
                @fs_kd_trs_kwitansi, @fs_kd_trs_charge, @fs_kd_trs_disckredit, @fs_kd_trs_chargebank, @fs_kd_trs_discbank,
                @fs_kd_jenis_bank, @fs_kd_jenis_kartu_kredit, @fs_no_bank, @fs_kd_rek_deposit,
                @fn_genkeu, @fn_status_bridge, @fn_sum_voucher_dep,
                @CRTTGL, @CRTJAM, @CRTIPA, @CRTVER, @CRTUSR,
                @UPDTGL, @UPDJAM, @UPDIPA, @UPDVER, @UPDUSR,
                @fd_tgl_jam_trs
            )
            """;

        var dpHdr = new DynamicParameters();
        dpHdr.Add("@fs_kd_trs", trsKasirId, DbType.String);
        dpHdr.Add("@fd_tgl_trs", tglTrs, DbType.String);
        dpHdr.Add("@fs_jam_trs", jamTrs, DbType.String);
        dpHdr.Add("@fs_kd_petugas", Truncate(hdr.PetugasId ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fd_tgl_void", string.IsNullOrWhiteSpace(hdr.TglVoid) ? "3000-01-01" : hdr.TglVoid, DbType.String);
        dpHdr.Add("@fs_jam_void", string.IsNullOrWhiteSpace(hdr.JamVoid) ? "00:00:00" : hdr.JamVoid, DbType.String);
        dpHdr.Add("@fs_kd_petugas_void", Truncate(hdr.PetugasVoid ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_reg", Truncate(hdr.RegId ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fn_sum_kas", hdr.SumKas, DbType.Decimal);
        dpHdr.Add("@fn_sum_bank", hdr.SumBank, DbType.Decimal);
        dpHdr.Add("@fn_sum_kartu_kredit", hdr.SumKartuKredit, DbType.Decimal);
        dpHdr.Add("@fn_sum_all", hdr.SumAll, DbType.Decimal);
        dpHdr.Add("@fs_kd_rek_kas", Truncate(hdr.KdRekKas ?? string.Empty, 20), DbType.String);
        dpHdr.Add("@fs_kd_rek_bank", Truncate(hdr.KdRekBank ?? string.Empty, 20), DbType.String);
        dpHdr.Add("@fs_kd_rek_kartu_kredit", Truncate(hdr.KdRekKartuKredit ?? string.Empty, 20), DbType.String);
        dpHdr.Add("@fs_no_kartu_kredit", Truncate(hdr.NoKartuKredit ?? string.Empty, 20), DbType.String);
        dpHdr.Add("@fs_kd_trs_kwitansi", Truncate(hdr.KdTrsKwitansi ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_trs_charge", Truncate(hdr.KdTrsCharge ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_trs_disckredit", Truncate(hdr.KdTrsDiscKredit ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_trs_chargebank", Truncate(hdr.KdTrsChargeBank ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_trs_discbank", Truncate(hdr.KdTrsDiscBank ?? string.Empty, 10), DbType.String);
        dpHdr.Add("@fs_kd_jenis_bank", Truncate(hdr.KdJenisBank ?? string.Empty, 5), DbType.String);
        dpHdr.Add("@fs_kd_jenis_kartu_kredit", Truncate(hdr.KdJenisKartuKredit ?? string.Empty, 5), DbType.String);
        dpHdr.Add("@fs_no_bank", Truncate(hdr.NoBank ?? string.Empty, 50), DbType.String);
        dpHdr.Add("@fs_kd_rek_deposit", string.IsNullOrWhiteSpace(hdr.KdRekDeposit) ? "4210000" : Truncate(hdr.KdRekDeposit, 20), DbType.String);
        dpHdr.Add("@fn_genkeu", hdr.GenKeu == 0 ? 5 : hdr.GenKeu, DbType.Decimal);
        dpHdr.Add("@fn_status_bridge", hdr.StatusBridge, DbType.Decimal);
        dpHdr.Add("@fn_sum_voucher_dep", hdr.SumVoucherDep, DbType.Decimal);
        dpHdr.Add("@CRTTGL", crtTgl, DbType.String);
        dpHdr.Add("@CRTJAM", crtJam, DbType.String);
        dpHdr.Add("@CRTIPA", string.IsNullOrWhiteSpace(hdr.CrtIpa) ? "127.0.0.1" : Truncate(hdr.CrtIpa, 40), DbType.String);
        dpHdr.Add("@CRTVER", string.IsNullOrWhiteSpace(hdr.CrtVer) ? "1.0" : Truncate(hdr.CrtVer, 12), DbType.String);
        dpHdr.Add("@CRTUSR", crtUsr, DbType.String);
        dpHdr.Add("@UPDTGL", updTgl, DbType.String);
        dpHdr.Add("@UPDJAM", updJam, DbType.String);
        dpHdr.Add("@UPDIPA", string.IsNullOrWhiteSpace(hdr.UpdIpa) ? (string.IsNullOrWhiteSpace(hdr.CrtIpa) ? "127.0.0.1" : Truncate(hdr.CrtIpa, 40)) : Truncate(hdr.UpdIpa, 40), DbType.String);
        dpHdr.Add("@UPDVER", string.IsNullOrWhiteSpace(hdr.UpdVer) ? (string.IsNullOrWhiteSpace(hdr.CrtVer) ? "1.0" : Truncate(hdr.CrtVer, 12)) : Truncate(hdr.UpdVer, 12), DbType.String);
        dpHdr.Add("@UPDUSR", updUsr, DbType.String);
        dpHdr.Add("@fd_tgl_jam_trs", tglJamTrs, DbType.String);

        const string sqlDtl = """
            INSERT INTO ta_trs_deposit_khusus2 (
                fs_kd_trs, fn_no_urut, fs_kd_trs_bill, fs_kd_layanan,
                fs_keterangan, fn_nilai_bill, fn_kas, fn_bank, fn_kartu_kredit,
                fb_void, fb_jasa, fb_obat, fb_titipan,
                fs_kd_trs_voucher, fn_voucher_dep
            ) VALUES (
                @fs_kd_trs, @fn_no_urut, @fs_kd_trs_bill, @fs_kd_layanan,
                @fs_keterangan, @fn_nilai_bill, @fn_kas, @fn_bank, @fn_kartu_kredit,
                @fb_void, @fb_jasa, @fb_obat, @fb_titipan,
                @fs_kd_trs_voucher, @fn_voucher_dep
            )
            """;

        var dtlList = details?.Select(dtl => new
        {
            fs_kd_trs = trsKasirId,
            fn_no_urut = dtl.NoUrut,
            fs_kd_trs_bill = Truncate(dtl.TrsBillId ?? string.Empty, 15),
            fs_kd_layanan = Truncate(dtl.LayananId ?? string.Empty, 5),
            fs_keterangan = Truncate(dtl.Keterangan ?? string.Empty, 60),
            fn_nilai_bill = dtl.NilaiBill,
            fn_kas = dtl.NilaiKas,
            fn_bank = dtl.NilaiBank,
            fn_kartu_kredit = dtl.NilaiKartuKredit,
            fb_void = dtl.IsVoid,
            fb_jasa = dtl.IsJasa,
            fb_obat = dtl.IsObat,
            fb_titipan = dtl.IsTitipan,
            fs_kd_trs_voucher = Truncate(dtl.TrsVoucherId ?? string.Empty, 10),
            fn_voucher_dep = dtl.VoucherDep
        }).ToList();

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlHdr, dpHdr);
        if (dtlList != null && dtlList.Count > 0)
        {
            conn.Execute(sqlDtl, dtlList);
        }
    }

    public void Void(string trsKasirId, string tglVoid, string jamVoid, string petugasVoid)
    {
        const string sqlHdr = """
            UPDATE ta_trs_deposit_khusus
            SET fd_tgl_void = @TglVoid,
                fs_jam_void = @JamVoid,
                fs_kd_petugas_void = @PetugasVoid,
                UPDTGL = @TglVoid,
                UPDJAM = @JamVoid,
                UPDUSR = @PetugasVoid
            WHERE fs_kd_trs = @TrsKasirId
            """;

        const string sqlDtl = """
            UPDATE ta_trs_deposit_khusus2
            SET fb_void = 1
            WHERE fs_kd_trs = @TrsKasirId
            """;

        var cleanTglVoid = string.IsNullOrWhiteSpace(tglVoid) ? DateTime.Today.ToString("yyyy-MM-dd") : tglVoid;
        var cleanJamVoid = string.IsNullOrWhiteSpace(jamVoid) ? DateTime.Now.ToString("HH:mm:ss") : jamVoid;
        var cleanPetugasVoid = Truncate(petugasVoid ?? string.Empty, 10);

        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", Truncate(trsKasirId, 10), DbType.String);
        dp.Add("@TglVoid", cleanTglVoid, DbType.String);
        dp.Add("@JamVoid", cleanJamVoid, DbType.String);
        dp.Add("@PetugasVoid", cleanPetugasVoid, DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sqlHdr, dp);
        conn.Execute(sqlDtl, dp);
    }

    public TrsKasirLegacyHdrDto? GetHeader(string trsKasirId)
    {
        const string sql = """
            SELECT
                RTRIM(fs_kd_trs) AS TrsKasirId,
                RTRIM(fd_tgl_trs) AS TglTrs,
                RTRIM(fs_jam_trs) AS JamTrs,
                RTRIM(fs_kd_petugas) AS PetugasId,
                RTRIM(fs_kd_reg) AS RegId,
                fn_sum_kas AS SumKas,
                fn_sum_bank AS SumBank,
                fn_sum_kartu_kredit AS SumKartuKredit,
                fn_sum_all AS SumAll,
                RTRIM(fs_kd_rek_kas) AS KdRekKas,
                RTRIM(fs_kd_rek_bank) AS KdRekBank,
                RTRIM(fd_tgl_void) AS TglVoid,
                RTRIM(fs_jam_void) AS JamVoid,
                RTRIM(fs_kd_petugas_void) AS PetugasVoid,
                RTRIM(fs_kd_rek_kartu_kredit) AS KdRekKartuKredit,
                RTRIM(fs_no_kartu_kredit) AS NoKartuKredit,
                RTRIM(fs_kd_trs_kwitansi) AS KdTrsKwitansi,
                RTRIM(fs_kd_trs_charge) AS KdTrsCharge,
                RTRIM(fs_kd_trs_disckredit) AS KdTrsDiscKredit,
                RTRIM(fs_kd_trs_chargebank) AS KdTrsChargeBank,
                RTRIM(fs_kd_trs_discbank) AS KdTrsDiscBank,
                RTRIM(fs_kd_jenis_bank) AS KdJenisBank,
                RTRIM(fs_kd_jenis_kartu_kredit) AS KdJenisKartuKredit,
                RTRIM(fs_no_bank) AS NoBank,
                RTRIM(fs_kd_rek_deposit) AS KdRekDeposit,
                fn_genkeu AS GenKeu,
                fn_status_bridge AS StatusBridge,
                fn_sum_voucher_dep AS SumVoucherDep,
                RTRIM(CRTTGL) AS CrtTgl,
                RTRIM(CRTJAM) AS CrtJam,
                RTRIM(CRTIPA) AS CrtIpa,
                RTRIM(CRTVER) AS CrtVer,
                RTRIM(CRTUSR) AS CrtUsr,
                RTRIM(UPDTGL) AS UpdTgl,
                RTRIM(UPDJAM) AS UpdJam,
                RTRIM(UPDIPA) AS UpdIpa,
                RTRIM(UPDVER) AS UpdVer,
                RTRIM(UPDUSR) AS UpdUsr,
                RTRIM(fd_tgl_jam_trs) AS TglJamTrs
            FROM ta_trs_deposit_khusus
            WHERE fs_kd_trs = @TrsKasirId
            """;

        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", Truncate(trsKasirId, 10), DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<TrsKasirLegacyHdrDto>(sql, dp);
    }

    public IEnumerable<TrsKasirLegacyDtlDto> ListDetails(string trsKasirId)
    {
        const string sql = """
            SELECT
                RTRIM(fs_kd_trs) AS TrsKasirId,
                fn_no_urut AS NoUrut,
                RTRIM(fs_kd_trs_bill) AS TrsBillId,
                RTRIM(fs_kd_layanan) AS LayananId,
                RTRIM(fs_keterangan) AS Keterangan,
                fn_nilai_bill AS NilaiBill,
                fn_kas AS NilaiKas,
                fn_bank AS NilaiBank,
                fn_kartu_kredit AS NilaiKartuKredit,
                fb_void AS IsVoid,
                fb_jasa AS IsJasa,
                fb_obat AS IsObat,
                fb_titipan AS IsTitipan,
                RTRIM(fs_kd_trs_voucher) AS TrsVoucherId,
                fn_voucher_dep AS VoucherDep
            FROM ta_trs_deposit_khusus2
            WHERE fs_kd_trs = @TrsKasirId
            ORDER BY fn_no_urut ASC
            """;

        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", Truncate(trsKasirId, 10), DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<TrsKasirLegacyDtlDto>(sql, dp);
    }

    public void Delete(string trsKasirId)
    {
        const string sql = """
            DELETE FROM ta_trs_deposit_khusus2 WHERE fs_kd_trs = @TrsKasirId;
            DELETE FROM ta_trs_deposit_khusus WHERE fs_kd_trs = @TrsKasirId;
            """;

        var dp = new DynamicParameters();
        dp.Add("@TrsKasirId", Truncate(trsKasirId, 10), DbType.String);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    private static string Truncate(string? value, int maxLength)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        return value.Length <= maxLength ? value : value[..maxLength];
    }
}
