using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Infrastructure.ChargeContext.AmbulanceFeature;

public interface ITrsTransportDal :
    IInsert<TrsTransportModel>,
    IUpdate<TrsTransportModel>,
    IDelete<ITrsTransportKey>,
    IGetData<TrsTransportDto, ITrsTransportKey>
{
    IEnumerable<TrsTransportDto> ListDataByReg(string regId);
    IEnumerable<TrsTransportDto> ListActiveDispatched();
    bool HasActiveDispatchForAmbulance(string ambulanceId, string? excludeTrsId = null);
    bool HasActiveDispatchForDriver(string driverId, string? excludeTrsId = null);
}

public class TrsTransportDal : ITrsTransportDal
{
    private readonly DatabaseOptions _opt;

    public TrsTransportDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    private static DynamicParameters CreateParam(TrsTransportModel model)
    {
        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", model.TrsTransportId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_reg", model.RegId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_layanan", model.LayananId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_trs", model.TglTrs.ToString("yyyy-MM-dd"), SqlDbType.VarChar);
        dp.AddParam("@fs_jam_trs", model.TglTrs.ToString("HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_jam_trs", model.TglTrs.ToString("yyyy-MM-dd HH:mm:ss"), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas", model.PetugasId, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_void", model.IsVoid ? model.TglVoid.ToString("yyyy-MM-dd") : "3000-01-01", SqlDbType.VarChar);
        dp.AddParam("@fs_jam_void", model.IsVoid ? model.TglVoid.ToString("HH:mm:ss") : "00:00:00", SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_void", model.PetugasVoidId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_transport", model.AmbulanceId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_driver", model.DriverId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_polisi", model.NoPolisi, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_tujuan_transport", model.TujuanTransportId, SqlDbType.VarChar);
        dp.AddParam("@fn_konstanta", model.Konstanta, SqlDbType.Decimal);
        dp.AddParam("@fn_sub_total", model.SubTotal, SqlDbType.Decimal);
        dp.AddParam("@fn_diskon", model.Diskon, SqlDbType.Decimal);
        dp.AddParam("@fn_total", model.Total, SqlDbType.Decimal);
        dp.AddParam("@fn_kas", model.Kas, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_rek_kas", model.RekKasId, SqlDbType.VarChar);
        dp.AddParam("@fn_bank", model.Bank, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_rek_bank", model.RekBankId, SqlDbType.VarChar);
        dp.AddParam("@fn_kartu_kredit", model.KartuKredit, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_rek_kartu_kredit", model.RekKartuKreditId, SqlDbType.VarChar);
        dp.AddParam("@fs_no_kartu_kredit", model.NoKartuKredit, SqlDbType.VarChar);
        dp.AddParam("@fs_no_bank", model.NoBank, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_kartu_kredit", model.JenisKartuKreditId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_jenis_bank", model.JenisBankId, SqlDbType.VarChar);
        dp.AddParam("@fn_sisa", model.Sisa, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_rek_sisa", model.RekSisaId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_petugas_kasir", model.PetugasKasirId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_kelas", model.KelasId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_charge", model.TrsChargeId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_deposit", model.TrsDepositId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_trs_kwitansi", model.TrsKwitansiId, SqlDbType.VarChar);
        dp.AddParam("@fn_genkeu", model.GenKeu, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_tipe_jaminan", model.TipeJaminanId, SqlDbType.VarChar);
        dp.AddParam("@fs_nm_pasien", model.PasienName, SqlDbType.VarChar);
        dp.AddParam("@fs_alm1_pasien", model.Alm1Pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_alm2_pasien", model.Alm2Pasien, SqlDbType.VarChar);
        dp.AddParam("@fs_kota_pasien", model.KotaPasien, SqlDbType.VarChar);
        dp.AddParam("@fs_sex", model.Sex, SqlDbType.VarChar);
        dp.AddParam("@fs_keterangan", model.Keterangan, SqlDbType.VarChar);

        dp.AddParam("@fs_status", model.Status.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fs_tipe_perjalanan", model.TipePerjalanan.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fs_metode_tarif", model.MetodeTarif.ToString(), SqlDbType.VarChar);
        dp.AddParam("@fs_kd_perawat", model.PerawatId, SqlDbType.VarChar);
        dp.AddParam("@fs_kd_dokter", model.DokterId, SqlDbType.VarChar);
        dp.AddParam("@fs_lokasi_asal", model.LokasiAsal, SqlDbType.VarChar);
        dp.AddParam("@fs_lokasi_tujuan", model.LokasiTujuan, SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_request", model.TglRequest != default ? model.TglRequest.ToString("yyyy-MM-dd HH:mm:ss") : "", SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_dispatch", model.TglDispatch.HasValue ? model.TglDispatch.Value.ToString("yyyy-MM-dd HH:mm:ss") : "", SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_arrival", model.TglArrival.HasValue ? model.TglArrival.Value.ToString("yyyy-MM-dd HH:mm:ss") : "", SqlDbType.VarChar);
        dp.AddParam("@fd_tgl_return", model.TglReturn.HasValue ? model.TglReturn.Value.ToString("yyyy-MM-dd HH:mm:ss") : "", SqlDbType.VarChar);
        dp.AddParam("@fn_odometer_awal", model.StartOdometer, SqlDbType.Decimal);
        dp.AddParam("@fn_odometer_akhir", model.EndOdometer, SqlDbType.Decimal);
        dp.AddParam("@fs_kd_jaminan", model.JaminanId, SqlDbType.VarChar);
        dp.AddParam("@fb_non_reg", model.IsNonReg, SqlDbType.Bit);
        dp.AddParam("@fb_posted_billing", model.IsPostedBilling, SqlDbType.Bit);
        dp.AddParam("@fs_alasan_batal", model.AlasanBatal, SqlDbType.VarChar);
        dp.AddParam("@fn_biaya_batal", model.BiayaBatal, SqlDbType.Decimal);
        return dp;
    }

    public void Insert(TrsTransportModel model)
    {
        const string sql = @"
            INSERT INTO ta_trs_transport (
                fs_kd_trs, fs_kd_reg, fs_kd_layanan, fd_tgl_trs, fs_jam_trs, fd_tgl_jam_trs, fs_kd_petugas,
                fd_tgl_void, fs_jam_void, fs_kd_petugas_void,
                fs_kd_transport, fs_kd_driver, fs_no_polisi, fs_kd_tujuan_transport, fn_konstanta,
                fn_sub_total, fn_diskon, fn_total, fn_kas, fs_kd_rek_kas, fn_bank, fs_kd_rek_bank,
                fn_kartu_kredit, fs_kd_rek_kartu_kredit, fs_no_kartu_kredit, fs_no_bank, fs_kd_jenis_kartu_kredit, fs_kd_jenis_bank,
                fn_sisa, fs_kd_rek_sisa, fs_kd_petugas_kasir, fs_kd_kelas, fs_kd_trs_charge, fs_kd_trs_deposit, fs_kd_trs_kwitansi,
                fn_genkeu, fs_kd_tipe_jaminan, fs_nm_pasien, fs_alm1_pasien, fs_alm2_pasien, fs_kota_pasien, fs_sex, fs_keterangan,
                fs_status, fs_tipe_perjalanan, fs_metode_tarif, fs_kd_perawat, fs_kd_dokter, fs_lokasi_asal, fs_lokasi_tujuan,
                fd_tgl_request, fd_tgl_dispatch, fd_tgl_arrival, fd_tgl_return, fn_odometer_awal, fn_odometer_akhir,
                fs_kd_jaminan, fb_non_reg, fb_posted_billing, fs_alasan_batal, fn_biaya_batal)
            VALUES (
                @fs_kd_trs, @fs_kd_reg, @fs_kd_layanan, @fd_tgl_trs, @fs_jam_trs, @fd_tgl_jam_trs, @fs_kd_petugas,
                @fd_tgl_void, @fs_jam_void, @fs_kd_petugas_void,
                @fs_kd_transport, @fs_kd_driver, @fs_no_polisi, @fs_kd_tujuan_transport, @fn_konstanta,
                @fn_sub_total, @fn_diskon, @fn_total, @fn_kas, @fs_kd_rek_kas, @fn_bank, @fs_kd_rek_bank,
                @fn_kartu_kredit, @fs_kd_rek_kartu_kredit, @fs_no_kartu_kredit, @fs_no_bank, @fs_kd_jenis_kartu_kredit, @fs_kd_jenis_bank,
                @fn_sisa, @fs_kd_rek_sisa, @fs_kd_petugas_kasir, @fs_kd_kelas, @fs_kd_trs_charge, @fs_kd_trs_deposit, @fs_kd_trs_kwitansi,
                @fn_genkeu, @fs_kd_tipe_jaminan, @fs_nm_pasien, @fs_alm1_pasien, @fs_alm2_pasien, @fs_kota_pasien, @fs_sex, @fs_keterangan,
                @fs_status, @fs_tipe_perjalanan, @fs_metode_tarif, @fs_kd_perawat, @fs_kd_dokter, @fs_lokasi_asal, @fs_lokasi_tujuan,
                @fd_tgl_request, @fd_tgl_dispatch, @fd_tgl_arrival, @fd_tgl_return, @fn_odometer_awal, @fn_odometer_akhir,
                @fs_kd_jaminan, @fb_non_reg, @fb_posted_billing, @fs_alasan_batal, @fn_biaya_batal)";

        var dp = CreateParam(model);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(TrsTransportModel model)
    {
        const string sql = @"
            UPDATE ta_trs_transport
            SET fs_kd_reg = @fs_kd_reg,
                fs_kd_layanan = @fs_kd_layanan,
                fd_tgl_trs = @fd_tgl_trs,
                fs_jam_trs = @fs_jam_trs,
                fd_tgl_jam_trs = @fd_tgl_jam_trs,
                fs_kd_petugas = @fs_kd_petugas,
                fd_tgl_void = @fd_tgl_void,
                fs_jam_void = @fs_jam_void,
                fs_kd_petugas_void = @fs_kd_petugas_void,
                fs_kd_transport = @fs_kd_transport,
                fs_kd_driver = @fs_kd_driver,
                fs_no_polisi = @fs_no_polisi,
                fs_kd_tujuan_transport = @fs_kd_tujuan_transport,
                fn_konstanta = @fn_konstanta,
                fn_sub_total = @fn_sub_total,
                fn_diskon = @fn_diskon,
                fn_total = @fn_total,
                fn_kas = @fn_kas,
                fs_kd_rek_kas = @fs_kd_rek_kas,
                fn_bank = @fn_bank,
                fs_kd_rek_bank = @fs_kd_rek_bank,
                fn_kartu_kredit = @fn_kartu_kredit,
                fs_kd_rek_kartu_kredit = @fs_kd_rek_kartu_kredit,
                fs_no_kartu_kredit = @fs_no_kartu_kredit,
                fs_no_bank = @fs_no_bank,
                fs_kd_jenis_kartu_kredit = @fs_kd_jenis_kartu_kredit,
                fs_kd_jenis_bank = @fs_kd_jenis_bank,
                fn_sisa = @fn_sisa,
                fs_kd_rek_sisa = @fs_kd_rek_sisa,
                fs_kd_petugas_kasir = @fs_kd_petugas_kasir,
                fs_kd_kelas = @fs_kd_kelas,
                fs_kd_trs_charge = @fs_kd_trs_charge,
                fs_kd_trs_deposit = @fs_kd_trs_deposit,
                fs_kd_trs_kwitansi = @fs_kd_trs_kwitansi,
                fn_genkeu = @fn_genkeu,
                fs_kd_tipe_jaminan = @fs_kd_tipe_jaminan,
                fs_nm_pasien = @fs_nm_pasien,
                fs_alm1_pasien = @fs_alm1_pasien,
                fs_alm2_pasien = @fs_alm2_pasien,
                fs_kota_pasien = @fs_kota_pasien,
                fs_sex = @fs_sex,
                fs_keterangan = @fs_keterangan,
                fs_status = @fs_status,
                fs_tipe_perjalanan = @fs_tipe_perjalanan,
                fs_metode_tarif = @fs_metode_tarif,
                fs_kd_perawat = @fs_kd_perawat,
                fs_kd_dokter = @fs_kd_dokter,
                fs_lokasi_asal = @fs_lokasi_asal,
                fs_lokasi_tujuan = @fs_lokasi_tujuan,
                fd_tgl_request = @fd_tgl_request,
                fd_tgl_dispatch = @fd_tgl_dispatch,
                fd_tgl_arrival = @fd_tgl_arrival,
                fd_tgl_return = @fd_tgl_return,
                fn_odometer_awal = @fn_odometer_awal,
                fn_odometer_akhir = @fn_odometer_akhir,
                fs_kd_jaminan = @fs_kd_jaminan,
                fb_non_reg = @fb_non_reg,
                fb_posted_billing = @fb_posted_billing,
                fs_alasan_batal = @fs_alasan_batal,
                fn_biaya_batal = @fn_biaya_batal
            WHERE fs_kd_trs = @fs_kd_trs";

        var dp = CreateParam(model);
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(ITrsTransportKey key)
    {
        const string sql = @"
            DELETE FROM ta_trs_transport
            WHERE fs_kd_trs = @fs_kd_trs";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public TrsTransportDto GetData(ITrsTransportKey key)
    {
        const string sql = @"
            SELECT *
            FROM ta_trs_transport
            WHERE fs_kd_trs = @fs_kd_trs";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_trs", key.TrsTransportId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<TrsTransportDto>(sql, dp);
    }

    public IEnumerable<TrsTransportDto> ListDataByReg(string regId)
    {
        const string sql = @"
            SELECT *
            FROM ta_trs_transport
            WHERE fs_kd_reg = @fs_kd_reg
            ORDER BY fd_tgl_jam_trs DESC";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_reg", regId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TrsTransportDto>(sql, dp);
        return list ?? [];
    }

    public IEnumerable<TrsTransportDto> ListActiveDispatched()
    {
        const string sql = @"
            SELECT *
            FROM ta_trs_transport
            WHERE fs_status = 'Dispatched'
            ORDER BY fd_tgl_dispatch DESC";

        var dp = new DynamicParameters();
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var list = conn.Read<TrsTransportDto>(sql, dp);
        return list ?? [];
    }

    public bool HasActiveDispatchForAmbulance(string ambulanceId, string? excludeTrsId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM ta_trs_transport
            WHERE fs_kd_transport = @fs_kd_transport
              AND fs_status = 'Dispatched'
              AND (@exclude_trs_id = '' OR fs_kd_trs <> @exclude_trs_id)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_transport", ambulanceId, SqlDbType.VarChar);
        dp.AddParam("@exclude_trs_id", excludeTrsId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int>(sql, dp);
        return count > 0;
    }

    public bool HasActiveDispatchForDriver(string driverId, string? excludeTrsId = null)
    {
        const string sql = @"
            SELECT COUNT(1)
            FROM ta_trs_transport
            WHERE fs_kd_driver = @fs_kd_driver
              AND fs_status = 'Dispatched'
              AND (@exclude_trs_id = '' OR fs_kd_trs <> @exclude_trs_id)";

        var dp = new DynamicParameters();
        dp.AddParam("@fs_kd_driver", driverId, SqlDbType.VarChar);
        dp.AddParam("@exclude_trs_id", excludeTrsId ?? string.Empty, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var count = conn.ExecuteScalar<int>(sql, dp);
        return count > 0;
    }
}

public class TrsTransportDto
{
    public string fs_kd_trs { get; set; } = string.Empty;
    public string fs_kd_reg { get; set; } = string.Empty;
    public string fs_kd_layanan { get; set; } = string.Empty;
    public string fd_tgl_trs { get; set; } = "3000-01-01";
    public string fs_jam_trs { get; set; } = "00:00:00";
    public string fd_tgl_jam_trs { get; set; } = "3000-01-01 00:00:00";
    public string fs_kd_petugas { get; set; } = string.Empty;
    public string fd_tgl_void { get; set; } = "3000-01-01";
    public string fs_jam_void { get; set; } = "00:00:00";
    public string fs_kd_petugas_void { get; set; } = string.Empty;
    public string fs_kd_transport { get; set; } = string.Empty;
    public string fs_kd_driver { get; set; } = string.Empty;
    public string fs_no_polisi { get; set; } = string.Empty;
    public string fs_kd_tujuan_transport { get; set; } = string.Empty;
    public decimal fn_konstanta { get; set; }
    public decimal fn_sub_total { get; set; }
    public decimal fn_diskon { get; set; }
    public decimal fn_total { get; set; }
    public decimal fn_kas { get; set; }
    public string fs_kd_rek_kas { get; set; } = string.Empty;
    public decimal fn_bank { get; set; }
    public string fs_kd_rek_bank { get; set; } = string.Empty;
    public decimal fn_kartu_kredit { get; set; }
    public string fs_kd_rek_kartu_kredit { get; set; } = string.Empty;
    public string fs_no_kartu_kredit { get; set; } = string.Empty;
    public string fs_no_bank { get; set; } = string.Empty;
    public string fs_kd_jenis_kartu_kredit { get; set; } = string.Empty;
    public string fs_kd_jenis_bank { get; set; } = string.Empty;
    public decimal fn_sisa { get; set; }
    public string fs_kd_rek_sisa { get; set; } = string.Empty;
    public string fs_kd_petugas_kasir { get; set; } = string.Empty;
    public string fs_kd_kelas { get; set; } = string.Empty;
    public string fs_kd_trs_charge { get; set; } = string.Empty;
    public string fs_kd_trs_deposit { get; set; } = string.Empty;
    public string fs_kd_trs_kwitansi { get; set; } = string.Empty;
    public decimal fn_genkeu { get; set; }
    public string fs_kd_tipe_jaminan { get; set; } = string.Empty;
    public string fs_nm_pasien { get; set; } = string.Empty;
    public string fs_alm1_pasien { get; set; } = string.Empty;
    public string fs_alm2_pasien { get; set; } = string.Empty;
    public string fs_kota_pasien { get; set; } = string.Empty;
    public string fs_sex { get; set; } = string.Empty;
    public string fs_keterangan { get; set; } = string.Empty;

    public string fs_status { get; set; } = "Requested";
    public string fs_tipe_perjalanan { get; set; } = "OutboundReferral";
    public string fs_metode_tarif { get; set; } = "Wilayah";
    public string fs_kd_perawat { get; set; } = string.Empty;
    public string fs_kd_dokter { get; set; } = string.Empty;
    public string fs_lokasi_asal { get; set; } = string.Empty;
    public string fs_lokasi_tujuan { get; set; } = string.Empty;
    public string fd_tgl_request { get; set; } = string.Empty;
    public string fd_tgl_dispatch { get; set; } = string.Empty;
    public string fd_tgl_arrival { get; set; } = string.Empty;
    public string fd_tgl_return { get; set; } = string.Empty;
    public decimal fn_odometer_awal { get; set; }
    public decimal fn_odometer_akhir { get; set; }
    public string fs_kd_jaminan { get; set; } = string.Empty;
    public bool fb_non_reg { get; set; }
    public bool fb_posted_billing { get; set; }
    public string fs_alasan_batal { get; set; } = string.Empty;
    public decimal fn_biaya_batal { get; set; }

    public TrsTransportModel ToModel(IEnumerable<TrsTransportItemModel>? items = null)
    {
        var tglTrs = ParseDateTime(fd_tgl_jam_trs, fd_tgl_trs, fs_jam_trs, DateTime.Now);
        var isVoid = !string.IsNullOrWhiteSpace(fd_tgl_void) && !fd_tgl_void.StartsWith("3000");
        var tglVoid = isVoid ? ParseDateTime(fd_tgl_void, fd_tgl_void, fs_jam_void, DateTime.MinValue) : DateTime.MinValue;

        var status = ParseStatus(fs_status);
        var tipePerjalanan = ParseTipePerjalanan(fs_tipe_perjalanan);
        var metodeTarif = ParseMetodeTarif(fs_metode_tarif);

        var tglRequest = ParseNullableDateTime(fd_tgl_request) ?? tglTrs;
        var tglDispatch = ParseNullableDateTime(fd_tgl_dispatch);
        var tglArrival = ParseNullableDateTime(fd_tgl_arrival);
        var tglReturn = ParseNullableDateTime(fd_tgl_return);

        return TrsTransportModel.Rehydrate(
            fs_kd_trs,
            fs_kd_reg,
            fs_kd_layanan,
            tglTrs,
            fs_kd_petugas,
            isVoid,
            tglVoid,
            fs_kd_petugas_void,
            fs_kd_transport,
            fs_kd_driver,
            fs_no_polisi,
            fs_kd_tujuan_transport,
            fn_konstanta,
            fn_sub_total,
            fn_diskon,
            fn_total,
            fn_kas,
            fs_kd_rek_kas,
            fn_bank,
            fs_kd_rek_bank,
            fn_kartu_kredit,
            fs_kd_rek_kartu_kredit,
            fs_no_kartu_kredit,
            fs_no_bank,
            fs_kd_jenis_kartu_kredit,
            fs_kd_jenis_bank,
            fn_sisa,
            fs_kd_rek_sisa,
            fs_kd_petugas_kasir,
            fs_kd_kelas,
            fs_kd_trs_charge,
            fs_kd_trs_deposit,
            fs_kd_trs_kwitansi,
            fn_genkeu,
            fs_kd_tipe_jaminan,
            fs_nm_pasien,
            fs_alm1_pasien,
            fs_alm2_pasien,
            fs_kota_pasien,
            fs_sex,
            fs_keterangan,
            status,
            tipePerjalanan,
            metodeTarif,
            fs_kd_perawat,
            fs_kd_dokter,
            fs_lokasi_asal,
            fs_lokasi_tujuan,
            tglRequest,
            tglDispatch,
            tglArrival,
            tglReturn,
            fn_odometer_awal,
            fn_odometer_akhir,
            fs_kd_jaminan,
            fb_non_reg,
            fb_posted_billing,
            fs_alasan_batal,
            fn_biaya_batal,
            items);
    }

    private static DateTime ParseDateTime(string fullDate, string dateOnly, string timeOnly, DateTime defaultVal)
    {
        if (!string.IsNullOrWhiteSpace(fullDate) && !fullDate.StartsWith("3000") && DateTime.TryParse(fullDate, out var dtFull))
            return dtFull;
        if (!string.IsNullOrWhiteSpace(dateOnly) && !dateOnly.StartsWith("3000"))
        {
            if (DateTime.TryParse($"{dateOnly} {timeOnly}", out var dtComb))
                return dtComb;
            if (DateTime.TryParse(dateOnly, out var dtDate))
                return dtDate;
        }
        return defaultVal;
    }

    private static DateTime? ParseNullableDateTime(string val)
    {
        if (string.IsNullOrWhiteSpace(val) || val.StartsWith("3000")) return null;
        if (DateTime.TryParse(val, out var dt)) return dt;
        return null;
    }

    private static TransportStatusEnum ParseStatus(string val)
    {
        return Enum.TryParse<TransportStatusEnum>(val, true, out var result) ? result : TransportStatusEnum.Requested;
    }

    private static TransportTypeEnum ParseTipePerjalanan(string val)
    {
        if (string.IsNullOrWhiteSpace(val)) return TransportTypeEnum.OutboundReferral;
        if (Enum.TryParse<TransportTypeEnum>(val, true, out var result)) return result;
        if (val.Contains("Inbound", StringComparison.OrdinalIgnoreCase)) return TransportTypeEnum.InboundRetrieval;
        if (val.Contains("Discharge", StringComparison.OrdinalIgnoreCase)) return TransportTypeEnum.Discharge;
        if (val.Contains("Hearse", StringComparison.OrdinalIgnoreCase)) return TransportTypeEnum.Hearse;
        return TransportTypeEnum.OutboundReferral;
    }

    private static ChargingMethodEnum ParseMetodeTarif(string val)
    {
        return Enum.TryParse<ChargingMethodEnum>(val, true, out var result) ? result : ChargingMethodEnum.Wilayah;
    }
}
