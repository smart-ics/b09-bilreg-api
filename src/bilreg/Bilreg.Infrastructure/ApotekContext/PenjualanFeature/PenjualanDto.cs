using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// DTO Dapper untuk mapping komprehensif 68 kolom tabel tb_trs_dobill_umum.
/// Mendukung kompatibilitas penuh dengan skema legacy SQL Server tanpa nilai NULL.
/// </summary>
public class PenjualanDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public string fd_tgl_trs { get; set; } = "3000-01-01";
    public string fs_jam_trs { get; set; } = "00:00:00";
    public string fs_kd_petugas { get; set; } = string.Empty;
    public string fd_tgl_void { get; set; } = "3000-01-01";
    public string fs_jam_void { get; set; } = "00:00:00";
    public string fs_kd_petugas_void { get; set; } = string.Empty;

    public string fs_kd_resep { get; set; } = string.Empty;
    public string fs_kd_layanan_resep { get; set; } = string.Empty;
    public string fs_kd_petugas_medis { get; set; } = string.Empty;
    public string fs_kd_layanan { get; set; } = string.Empty;
    public string fs_kd_reg { get; set; } = string.Empty;
    public string fs_nm_pasien { get; set; } = string.Empty;
    public string fs_alm_pasien { get; set; } = string.Empty;
    public string fs_alm2_pasien { get; set; } = string.Empty;
    public string fs_kota_pasien { get; set; } = string.Empty;
    public string fs_sex { get; set; } = string.Empty;

    public decimal fn_sub_total { get; set; }
    public decimal fn_diskon_lain { get; set; }
    public decimal fn_biaya_lain { get; set; }
    public decimal fn_grand_total { get; set; }
    public decimal fn_kas { get; set; }
    public decimal fn_sisa { get; set; }
    public string fs_kd_rek_kas { get; set; } = string.Empty;
    public string fs_kd_rek_sisa { get; set; } = string.Empty;
    public string fs_kd_tipe_barang { get; set; } = "01";
    public string fs_kd_trs_kwitansi { get; set; } = string.Empty;

    public decimal fn_sum_sub_total { get; set; }
    public decimal fn_sum_biaya { get; set; }
    public decimal fn_sum_tax_rupiah { get; set; }
    public string fs_kd_petugas_kasir { get; set; } = string.Empty;
    public decimal fn_bank { get; set; }
    public decimal fn_kartu_kredit { get; set; }
    public string fs_kd_jenis_bank { get; set; } = string.Empty;
    public string fs_kd_jenis_kartu_kredit { get; set; } = string.Empty;
    public string fs_kd_kelas { get; set; } = string.Empty;
    public string fs_kd_rek_bank { get; set; } = string.Empty;
    public string fs_kd_rek_diskon_lain { get; set; } = string.Empty;
    public string fs_no_kartu_kredit { get; set; } = string.Empty;
    public string fs_kd_rek_kartu_kredit { get; set; } = string.Empty;
    public string fs_kd_rek_pdpt { get; set; } = string.Empty;
    public string fs_kd_trs_charge { get; set; } = string.Empty;
    public string fs_kd_trs_deposit { get; set; } = string.Empty;
    public string fs_kd_tipe_jaminan { get; set; } = "00000";
    public string fs_no_bank { get; set; } = string.Empty;
    public decimal fn_pembulatan { get; set; }
    public string fs_kd_trs_gen { get; set; } = string.Empty;
    public decimal fn_bulat { get; set; }
    public string fs_kd_trs_mutasi { get; set; } = string.Empty;
    public string fd_tgl_serah { get; set; } = "3000-01-01";
    public string fs_jam_serah { get; set; } = "00:00:00";
    public string fs_kd_petugas_serah { get; set; } = string.Empty;
    public string fs_catatan_lain { get; set; } = string.Empty;
    public bool fb_reserved_order { get; set; }
    public string fs_no_dok { get; set; } = string.Empty;
    public decimal fn_genkeu { get; set; }
    public bool fb_odd { get; set; }

    public string CRTTGL { get; set; } = "3000-01-01";
    public string CRTJAM { get; set; } = "00:00:00";
    public string CRTIPA { get; set; } = string.Empty;
    public string CRTVER { get; set; } = string.Empty;
    public string CRTUSR { get; set; } = string.Empty;
    public string UPDTGL { get; set; } = "3000-01-01";
    public string UPDJAM { get; set; } = "00:00:00";
    public string UPDIPA { get; set; } = string.Empty;
    public string UPDVER { get; set; } = string.Empty;
    public string UPDUSR { get; set; } = string.Empty;
    public string fd_tgl_jam_trs { get; set; } = "3000-01-01 00:00:00";

    // Extra joined fields
    public string fs_mr { get; set; } = string.Empty;
    public string fs_nm_layanan { get; set; } = string.Empty;
    public string fs_nm_layanan_resep { get; set; } = string.Empty;
    public string fs_nm_petugas_medis { get; set; } = string.Empty;
    public string fs_nm_tipe_jaminan { get; set; } = string.Empty;

    public static PenjualanDto FromModel(
        PenjualanModel model,
        string actor = "BILREG-API",
        string ip = "127.0.0.1",
        string ver = "1.0",
        DateTime? now = null)
    {
        var timestamp = now ?? DateTime.Now;
        var tglToday = timestamp.ToString("yyyy-MM-dd");
        var jamNow = timestamp.ToString("HH:mm:ss");

        var isVoid = model.IsVoid;
        var tglVoid = isVoid ? model.TglVoid.ToString("yyyy-MM-dd") : "3000-01-01";
        var jamVoid = isVoid ? (string.IsNullOrWhiteSpace(model.JamVoid) ? "00:00:00" : model.JamVoid) : "00:00:00";
        var userVoid = isVoid ? model.PetugasVoid : string.Empty;

        var tglSerah = model.TglSerah != ApotekDate.Empty ? model.TglSerah.ToString("yyyy-MM-dd") : "3000-01-01";
        var jamSerah = !string.IsNullOrWhiteSpace(model.JamSerah) ? model.JamSerah : "00:00:00";

        var tglTrs = model.PenjualanDate.ToString("yyyy-MM-dd");
        var jamTrs = !string.IsNullOrWhiteSpace(model.PenjualanTime) ? model.PenjualanTime : jamNow;
        var tglJamTrs = $"{tglTrs} {jamTrs}";

        return new PenjualanDto
        {
            fs_kd_trs = Truncate(model.PenjualanId, 10),
            fd_tgl_trs = tglTrs,
            fs_jam_trs = jamTrs,
            fs_kd_petugas = Truncate(model.PetugasId, 10),
            fd_tgl_void = tglVoid,
            fs_jam_void = jamVoid,
            fs_kd_petugas_void = Truncate(userVoid, 10),

            fs_kd_resep = Truncate(model.ResepId, 10),
            fs_kd_layanan_resep = Truncate(model.LayananResepId, 5),
            fs_kd_petugas_medis = Truncate(model.DokterId, 10),
            fs_kd_layanan = Truncate(model.LayananId, 5),
            fs_kd_reg = Truncate(model.RegId, 10),
            fs_nm_pasien = Truncate(model.PasienName, 40),
            fs_alm_pasien = Truncate(model.PasienAlamat, 40),
            fs_alm2_pasien = Truncate(model.PasienAlamat2, 40),
            fs_kota_pasien = Truncate(model.PasienKota, 20),
            fs_sex = Truncate(model.Sex, 1),

            fn_sub_total = model.SubTotal,
            fn_diskon_lain = model.DiskonLain,
            fn_biaya_lain = model.BiayaLain,
            fn_grand_total = model.GrandTotal,
            fn_kas = model.Kas,
            fn_sisa = model.Sisa,
            fs_kd_rek_kas = string.Empty,
            fs_kd_rek_sisa = string.Empty,
            fs_kd_tipe_barang = "01",
            fs_kd_trs_kwitansi = Truncate(model.TrsKwitansiId, 10),

            fn_sum_sub_total = model.SumSubTotal,
            fn_sum_biaya = model.SumBiaya,
            fn_sum_tax_rupiah = model.SumTaxRupiah,
            fs_kd_petugas_kasir = string.Empty,
            fn_bank = model.Bank,
            fn_kartu_kredit = model.KartuKredit,
            fs_kd_jenis_bank = string.Empty,
            fs_kd_jenis_kartu_kredit = string.Empty,
            fs_kd_kelas = Truncate(model.KelasId, 3),
            fs_kd_rek_bank = string.Empty,
            fs_kd_rek_diskon_lain = string.Empty,
            fs_no_kartu_kredit = string.Empty,
            fs_kd_rek_kartu_kredit = string.Empty,
            fs_kd_rek_pdpt = string.Empty,
            fs_kd_trs_charge = Truncate(model.TrsChargeId, 10),
            fs_kd_trs_deposit = string.Empty,
            fs_kd_tipe_jaminan = Truncate(!string.IsNullOrWhiteSpace(model.TipeJaminanId) ? model.TipeJaminanId : "00000", 5),
            fs_no_bank = string.Empty,
            fn_pembulatan = model.Pembulatan,
            fs_kd_trs_gen = string.Empty,
            fn_bulat = model.Bulat,
            fs_kd_trs_mutasi = Truncate(model.TrsMutasiId, 10),
            fd_tgl_serah = tglSerah,
            fs_jam_serah = jamSerah,
            fs_kd_petugas_serah = Truncate(model.PetugasSerah, 10),
            fs_catatan_lain = Truncate(model.VoidReason, 256),
            fb_reserved_order = false,
            fs_no_dok = string.Empty,
            fn_genkeu = 0,
            fb_odd = false,

            CRTTGL = tglToday,
            CRTJAM = jamNow,
            CRTIPA = Truncate(ip, 40),
            CRTVER = Truncate(ver, 12),
            CRTUSR = Truncate(!string.IsNullOrWhiteSpace(model.PetugasId) ? model.PetugasId : actor, 10),

            UPDTGL = tglToday,
            UPDJAM = jamNow,
            UPDIPA = Truncate(ip, 40),
            UPDVER = Truncate(ver, 12),
            UPDUSR = Truncate(actor, 10),

            fd_tgl_jam_trs = tglJamTrs
        };
    }

    public PenjualanModel ToModel(IEnumerable<PenjualanItemModel> items)
    {
        var tglTrs = DateTime.TryParse(fd_tgl_trs, out var dt) ? dt : DateTime.Today;
        var tglSerahDt = (!string.IsNullOrWhiteSpace(fd_tgl_serah) && fd_tgl_serah != "3000-01-01" && DateTime.TryParse(fd_tgl_serah, out var ds))
            ? ds
            : ApotekDate.Empty;

        var isVoid = !string.IsNullOrWhiteSpace(fd_tgl_void) && fd_tgl_void != "3000-01-01" && fd_tgl_void != "";
        var tglVoidDt = isVoid && DateTime.TryParse(fd_tgl_void, out var dv) ? dv : ApotekDate.Empty;

        var jenis = !string.IsNullOrWhiteSpace(fs_kd_resep)
            ? JenisPenjualanEnum.PrescriptionBased
            : (!string.IsNullOrWhiteSpace(fs_kd_reg)
                ? JenisPenjualanEnum.RegisteredDirect
                : JenisPenjualanEnum.WalkInOTC);

        var status = isVoid
            ? PenjualanStatusEnum.Cancelled
            : (!string.IsNullOrWhiteSpace(fs_kd_trs_kwitansi)
                ? PenjualanStatusEnum.FinanciallyCleared
                : PenjualanStatusEnum.Issued);

        return PenjualanModel.Rehydrate(
            fs_kd_trs,
            tglTrs,
            fs_jam_trs,
            fs_kd_petugas,
            jenis,
            status,
            fs_kd_resep,
            fs_kd_layanan_resep,
            fs_kd_petugas_medis,
            fs_kd_layanan,
            fs_kd_reg,
            fs_nm_pasien,
            fs_alm_pasien,
            fs_alm2_pasien,
            fs_kota_pasien,
            fs_sex,
            fs_kd_tipe_jaminan,
            fs_kd_kelas,
            fn_sum_sub_total,
            fn_sum_biaya,
            fn_sum_tax_rupiah,
            fn_sub_total,
            fn_diskon_lain,
            fn_biaya_lain,
            fn_pembulatan,
            fn_bulat,
            fn_grand_total,
            fn_kas,
            fn_bank,
            fn_kartu_kredit,
            fn_sisa,
            fs_kd_trs_kwitansi,
            fs_kd_trs_charge,
            fs_kd_trs_mutasi,
            tglSerahDt,
            fs_jam_serah,
            fs_kd_petugas_serah,
            isVoid,
            tglVoidDt,
            fs_jam_void,
            fs_kd_petugas_void,
            fs_catatan_lain,
            items);
    }

    private static string Truncate(string? val, int maxLen)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }
}
