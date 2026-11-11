using System.Data;
using Bilreg.Domain.ApotekContext.PenjualanFeature;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// DTO untuk mapping presisi seluruh 54 kolom tabel tb_trs_dobill_umum2.
/// Mendukung SqlBulkCopy batching berkinerja tinggi tanpa nilai NULL pada kolom wajib.
/// </summary>
public class PenjualanItemDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public string fs_kd_trs2 { get; set; } = string.Empty;
    public decimal fn_no_urut { get; set; }
    public bool fb_void { get; set; }
    public string fs_kd_barang { get; set; } = string.Empty;
    public string fs_nm_barang { get; set; } = string.Empty;
    public string fs_kd_tipe_barang { get; set; } = "01";
    public bool fb_racik { get; set; }
    public bool fb_komponen { get; set; }
    public string fs_kd_racik { get; set; } = string.Empty;
    public decimal fn_qty_racik { get; set; }
    public string fs_racik_qty { get; set; } = string.Empty;
    public decimal fn_qty_barang { get; set; }
    public string fs_nm_satuan { get; set; } = string.Empty;
    public decimal fn_harga_satuan { get; set; }
    public decimal fn_diskon { get; set; }
    public decimal fn_biaya { get; set; }
    public decimal fn_sub_total { get; set; }
    public decimal fn_tax_prosen { get; set; }
    public decimal fn_tax_rupiah { get; set; }
    public decimal fn_biaya_fee { get; set; }
    public decimal fn_total { get; set; }
    public string fs_etiket { get; set; } = string.Empty;
    public bool fb_asuransi { get; set; }
    public string fs_kd_tipe_jaminan { get; set; } = "00000";
    public decimal fn_bulat { get; set; }
    public string fs_kd_satuan { get; set; } = string.Empty;
    public decimal fn_etiket_qty { get; set; }
    public string fs_etiket_qty { get; set; } = string.Empty;
    public decimal fn_etiket_hari { get; set; }
    public string fs_etiket_kd_pakai { get; set; } = string.Empty;
    public string fs_etiket_catatan { get; set; } = string.Empty;
    public string fs_etiket_jenis_obat { get; set; } = string.Empty;
    public string fs_etiket_kd_satuan_pakai { get; set; } = string.Empty;
    public string fd_etiket_cetak { get; set; } = "3000-01-01";
    public bool fb_belibebas { get; set; }
    public string fs_etiket_hari { get; set; } = string.Empty;
    public bool fb_paket_tarif { get; set; }
    public decimal fn_nilai_klaim { get; set; }
    public string fs_waktu_odd { get; set; } = string.Empty;
    public decimal fn_etiket_cetak { get; set; }

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

    public decimal? fn_etiket_sms { get; set; }
    public string fs_etiket_description { get; set; } = "0";
    public bool fb_biaya_tax { get; set; }

    public static PenjualanItemDto FromModel(
        string penjualanId,
        PenjualanItemModel item,
        string tipeJaminanId = "00000",
        string actor = "BILREG-API",
        string ip = "127.0.0.1",
        string ver = "1.0",
        DateTime? now = null)
    {
        var timestamp = now ?? DateTime.Now;
        var tglToday = timestamp.ToString("yyyy-MM-dd");
        var jamNow = timestamp.ToString("HH:mm:ss");

        return new PenjualanItemDto
        {
            fs_kd_trs = Truncate(penjualanId, 10),
            fs_kd_trs2 = Truncate(item.PenjualanItemId, 13),
            fn_no_urut = item.NoUrut,
            fb_void = item.IsVoid,
            fs_kd_barang = Truncate(item.BrgId, 13),
            fs_nm_barang = Truncate(item.BrgName, 50),
            fs_kd_tipe_barang = "01",
            fb_racik = item.IsRacik,
            fb_komponen = item.IsKomponen,
            fs_kd_racik = Truncate(item.RacikId, 13),
            fn_qty_racik = item.IsRacik ? item.Qty : 0m,
            fs_racik_qty = string.Empty,
            fn_qty_barang = item.Qty,
            fs_nm_satuan = Truncate(item.SatuanName, 10),
            fn_harga_satuan = item.HargaSatuan,
            fn_diskon = item.Diskon,
            fn_biaya = item.Biaya,
            fn_sub_total = item.SubTotal,
            fn_tax_prosen = item.TaxProsen,
            fn_tax_rupiah = item.TaxRupiah,
            fn_biaya_fee = item.Fee,
            fn_total = item.Total,
            fs_etiket = Truncate(item.Etiket, 64),
            fb_asuransi = !string.IsNullOrWhiteSpace(tipeJaminanId) && tipeJaminanId != "00000",
            fs_kd_tipe_jaminan = Truncate(!string.IsNullOrWhiteSpace(tipeJaminanId) ? tipeJaminanId : "00000", 5),
            fn_bulat = 0m,
            fs_kd_satuan = Truncate(item.SatuanId, 10),
            fn_etiket_qty = item.EtiketQty,
            fs_etiket_qty = string.Empty,
            fn_etiket_hari = item.EtiketHari,
            fs_etiket_kd_pakai = Truncate(item.EtiketKdPakai, 3),
            fs_etiket_catatan = Truncate(item.EtiketCatatan, 70),
            fs_etiket_jenis_obat = string.Empty,
            fs_etiket_kd_satuan_pakai = string.Empty,
            fd_etiket_cetak = "3000-01-01",
            fb_belibebas = item.IsBeliBebas,
            fs_etiket_hari = string.Empty,
            fb_paket_tarif = false,
            fn_nilai_klaim = item.NilaiKlaim,
            fs_waktu_odd = string.Empty,
            fn_etiket_cetak = 0m,

            CRTTGL = tglToday,
            CRTJAM = jamNow,
            CRTIPA = Truncate(ip, 40),
            CRTVER = Truncate(ver, 12),
            CRTUSR = Truncate(actor, 10),

            UPDTGL = tglToday,
            UPDJAM = jamNow,
            UPDIPA = Truncate(ip, 40),
            UPDVER = Truncate(ver, 12),
            UPDUSR = Truncate(actor, 10),

            fn_etiket_sms = null,
            fs_etiket_description = "0",
            fb_biaya_tax = false
        };
    }

    public PenjualanItemModel ToModel()
    {
        return new PenjualanItemModel(
            (int)fn_no_urut,
            fs_kd_trs2,
            fs_kd_barang,
            fs_nm_barang,
            fs_kd_satuan,
            fs_nm_satuan,
            fn_qty_barang,
            fn_harga_satuan,
            fn_diskon,
            fn_biaya,
            fn_tax_prosen,
            fn_tax_rupiah,
            fn_biaya_fee,
            fn_total,
            fs_etiket,
            fn_etiket_qty,
            fn_etiket_hari,
            fs_etiket_kd_pakai,
            fs_etiket_catatan,
            fb_racik,
            fb_komponen,
            fs_kd_racik,
            fb_belibebas,
            fn_nilai_klaim,
            fb_void,
            fn_sub_total);
    }

    public static DataTable BuildDataTable(IEnumerable<PenjualanItemDto> items)
    {
        var dt = new DataTable("tb_trs_dobill_umum2");

        dt.Columns.Add("fs_kd_trs", typeof(string));
        dt.Columns.Add("fs_kd_trs2", typeof(string));
        dt.Columns.Add("fn_no_urut", typeof(decimal));
        dt.Columns.Add("fb_void", typeof(bool));
        dt.Columns.Add("fs_kd_barang", typeof(string));
        dt.Columns.Add("fs_nm_barang", typeof(string));
        dt.Columns.Add("fs_kd_tipe_barang", typeof(string));
        dt.Columns.Add("fb_racik", typeof(bool));
        dt.Columns.Add("fb_komponen", typeof(bool));
        dt.Columns.Add("fs_kd_racik", typeof(string));
        dt.Columns.Add("fn_qty_racik", typeof(decimal));
        dt.Columns.Add("fs_racik_qty", typeof(string));
        dt.Columns.Add("fn_qty_barang", typeof(decimal));
        dt.Columns.Add("fs_nm_satuan", typeof(string));
        dt.Columns.Add("fn_harga_satuan", typeof(decimal));
        dt.Columns.Add("fn_diskon", typeof(decimal));
        dt.Columns.Add("fn_biaya", typeof(decimal));
        dt.Columns.Add("fn_sub_total", typeof(decimal));
        dt.Columns.Add("fn_tax_prosen", typeof(decimal));
        dt.Columns.Add("fn_tax_rupiah", typeof(decimal));
        dt.Columns.Add("fn_biaya_fee", typeof(decimal));
        dt.Columns.Add("fn_total", typeof(decimal));
        dt.Columns.Add("fs_etiket", typeof(string));
        dt.Columns.Add("fb_asuransi", typeof(bool));
        dt.Columns.Add("fs_kd_tipe_jaminan", typeof(string));
        dt.Columns.Add("fn_bulat", typeof(decimal));
        dt.Columns.Add("fs_kd_satuan", typeof(string));
        dt.Columns.Add("fn_etiket_qty", typeof(decimal));
        dt.Columns.Add("fs_etiket_qty", typeof(string));
        dt.Columns.Add("fn_etiket_hari", typeof(decimal));
        dt.Columns.Add("fs_etiket_kd_pakai", typeof(string));
        dt.Columns.Add("fs_etiket_catatan", typeof(string));
        dt.Columns.Add("fs_etiket_jenis_obat", typeof(string));
        dt.Columns.Add("fs_etiket_kd_satuan_pakai", typeof(string));
        dt.Columns.Add("fd_etiket_cetak", typeof(string));
        dt.Columns.Add("fb_belibebas", typeof(bool));
        dt.Columns.Add("fs_etiket_hari", typeof(string));
        dt.Columns.Add("fb_paket_tarif", typeof(bool));
        dt.Columns.Add("fn_nilai_klaim", typeof(decimal));
        dt.Columns.Add("fs_waktu_odd", typeof(string));
        dt.Columns.Add("fn_etiket_cetak", typeof(decimal));
        dt.Columns.Add("CRTTGL", typeof(string));
        dt.Columns.Add("CRTJAM", typeof(string));
        dt.Columns.Add("CRTIPA", typeof(string));
        dt.Columns.Add("CRTVER", typeof(string));
        dt.Columns.Add("CRTUSR", typeof(string));
        dt.Columns.Add("UPDTGL", typeof(string));
        dt.Columns.Add("UPDJAM", typeof(string));
        dt.Columns.Add("UPDIPA", typeof(string));
        dt.Columns.Add("UPDVER", typeof(string));
        dt.Columns.Add("UPDUSR", typeof(string));
        dt.Columns.Add("fn_etiket_sms", typeof(decimal)).AllowDBNull = true;
        dt.Columns.Add("fs_etiket_description", typeof(string));
        dt.Columns.Add("fb_biaya_tax", typeof(bool));

        foreach (var item in items)
        {
            var row = dt.NewRow();
            row["fs_kd_trs"] = item.fs_kd_trs;
            row["fs_kd_trs2"] = item.fs_kd_trs2;
            row["fn_no_urut"] = item.fn_no_urut;
            row["fb_void"] = item.fb_void;
            row["fs_kd_barang"] = item.fs_kd_barang;
            row["fs_nm_barang"] = item.fs_nm_barang;
            row["fs_kd_tipe_barang"] = item.fs_kd_tipe_barang;
            row["fb_racik"] = item.fb_racik;
            row["fb_komponen"] = item.fb_komponen;
            row["fs_kd_racik"] = item.fs_kd_racik;
            row["fn_qty_racik"] = item.fn_qty_racik;
            row["fs_racik_qty"] = item.fs_racik_qty;
            row["fn_qty_barang"] = item.fn_qty_barang;
            row["fs_nm_satuan"] = item.fs_nm_satuan;
            row["fn_harga_satuan"] = item.fn_harga_satuan;
            row["fn_diskon"] = item.fn_diskon;
            row["fn_biaya"] = item.fn_biaya;
            row["fn_sub_total"] = item.fn_sub_total;
            row["fn_tax_prosen"] = item.fn_tax_prosen;
            row["fn_tax_rupiah"] = item.fn_tax_rupiah;
            row["fn_biaya_fee"] = item.fn_biaya_fee;
            row["fn_total"] = item.fn_total;
            row["fs_etiket"] = item.fs_etiket;
            row["fb_asuransi"] = item.fb_asuransi;
            row["fs_kd_tipe_jaminan"] = item.fs_kd_tipe_jaminan;
            row["fn_bulat"] = item.fn_bulat;
            row["fs_kd_satuan"] = item.fs_kd_satuan;
            row["fn_etiket_qty"] = item.fn_etiket_qty;
            row["fs_etiket_qty"] = item.fs_etiket_qty;
            row["fn_etiket_hari"] = item.fn_etiket_hari;
            row["fs_etiket_kd_pakai"] = item.fs_etiket_kd_pakai;
            row["fs_etiket_catatan"] = item.fs_etiket_catatan;
            row["fs_etiket_jenis_obat"] = item.fs_etiket_jenis_obat;
            row["fs_etiket_kd_satuan_pakai"] = item.fs_etiket_kd_satuan_pakai;
            row["fd_etiket_cetak"] = item.fd_etiket_cetak;
            row["fb_belibebas"] = item.fb_belibebas;
            row["fs_etiket_hari"] = item.fs_etiket_hari;
            row["fb_paket_tarif"] = item.fb_paket_tarif;
            row["fn_nilai_klaim"] = item.fn_nilai_klaim;
            row["fs_waktu_odd"] = item.fs_waktu_odd;
            row["fn_etiket_cetak"] = item.fn_etiket_cetak;
            row["CRTTGL"] = item.CRTTGL;
            row["CRTJAM"] = item.CRTJAM;
            row["CRTIPA"] = item.CRTIPA;
            row["CRTVER"] = item.CRTVER;
            row["CRTUSR"] = item.CRTUSR;
            row["UPDTGL"] = item.UPDTGL;
            row["UPDJAM"] = item.UPDJAM;
            row["UPDIPA"] = item.UPDIPA;
            row["UPDVER"] = item.UPDVER;
            row["UPDUSR"] = item.UPDUSR;
            row["fn_etiket_sms"] = item.fn_etiket_sms.HasValue ? item.fn_etiket_sms.Value : DBNull.Value;
            row["fs_etiket_description"] = item.fs_etiket_description;
            row["fb_biaya_tax"] = item.fb_biaya_tax;

            dt.Rows.Add(row);
        }

        return dt;
    }

    private static string Truncate(string? val, int maxLen)
    {
        if (string.IsNullOrEmpty(val)) return string.Empty;
        return val.Length > maxLen ? val[..maxLen] : val;
    }
}
