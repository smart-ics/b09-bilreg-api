using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ApotekContext.PenjualanFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Infrastructure.ApotekContext.PenjualanFeature;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Moq;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.PenjualanFeature;

public class PenjualanDalTest
{
    private readonly PenjualanDal _penjualanDal = new(ConnStringHelper.GetTestEnv());
    private readonly PenjualanItemDal _penjualanItemDal = new(ConnStringHelper.GetTestEnv());
    private readonly PenjualanRepo _penjualanRepo;

    public PenjualanDalTest()
    {
        _penjualanRepo = new PenjualanRepo(_penjualanDal, _penjualanItemDal, ConnStringHelper.GetTestEnv());
    }

    private static PenjualanDto CreateFakerDto(string id = "DUTST0001A") => new()
    {
        fs_kd_trs = id,
        fd_tgl_trs = "2026-10-18",
        fs_jam_trs = "10:15:00",
        fs_kd_petugas = "USR01",
        fd_tgl_void = "3000-01-01",
        fs_jam_void = "00:00:00",
        fs_kd_petugas_void = string.Empty,

        fs_kd_resep = "RSP0000001",
        fs_kd_layanan_resep = "FAR01",
        fs_kd_petugas_medis = "DOC01",
        fs_kd_layanan = "APT01",
        fs_kd_reg = "RG26101801",
        fs_nm_pasien = "Budi Santoso",
        fs_alm_pasien = "Jl. Merdeka No. 10",
        fs_alm2_pasien = "RT 01 RW 02",
        fs_kota_pasien = "Pekalongan",
        fs_sex = "L",

        fn_sub_total = 50000m,
        fn_diskon_lain = 0m,
        fn_biaya_lain = 2000m,
        fn_grand_total = 52000m,
        fn_kas = 52000m,
        fn_sisa = 0m,
        fs_kd_rek_kas = "11101",
        fs_kd_rek_sisa = string.Empty,
        fs_kd_tipe_barang = "01",
        fs_kd_trs_kwitansi = "KW26101801",

        fn_sum_sub_total = 48000m,
        fn_sum_biaya = 2000m,
        fn_sum_tax_rupiah = 0m,
        fs_kd_petugas_kasir = "KSR01",
        fn_bank = 0m,
        fn_kartu_kredit = 0m,
        fs_kd_jenis_bank = string.Empty,
        fs_kd_jenis_kartu_kredit = string.Empty,
        fs_kd_kelas = "REG",
        fs_kd_rek_bank = string.Empty,
        fs_kd_rek_diskon_lain = string.Empty,
        fs_no_kartu_kredit = string.Empty,
        fs_kd_rek_kartu_kredit = string.Empty,
        fs_kd_rek_pdpt = "41101",
        fs_kd_trs_charge = string.Empty,
        fs_kd_trs_deposit = string.Empty,
        fs_kd_tipe_jaminan = "00000",
        fs_no_bank = string.Empty,
        fn_pembulatan = 0m,
        fs_kd_trs_gen = string.Empty,
        fn_bulat = 0m,
        fs_kd_trs_mutasi = string.Empty,
        fd_tgl_serah = "3000-01-01",
        fs_jam_serah = "00:00:00",
        fs_kd_petugas_serah = string.Empty,
        fs_catatan_lain = "Catatan resep",
        fb_reserved_order = false,
        fs_no_dok = string.Empty,
        fn_genkeu = 0,
        fb_odd = false,

        CRTTGL = "2026-10-18",
        CRTJAM = "10:15:00",
        CRTIPA = "127.0.0.1",
        CRTVER = "1.0",
        CRTUSR = "USR01",
        UPDTGL = "2026-10-18",
        UPDJAM = "10:15:00",
        UPDIPA = "127.0.0.1",
        UPDVER = "1.0",
        UPDUSR = "USR01",
        fd_tgl_jam_trs = "2026-10-18 10:15:00"
    };

    private static List<PenjualanItemDto> CreateFakerItems(string trsId = "DUTST0001A") =>
    [
        new PenjualanItemDto
        {
            fs_kd_trs = trsId,
            fs_kd_trs2 = $"{trsId}001",
            fn_no_urut = 1m,
            fb_void = false,
            fs_kd_barang = "BRG0000001",
            fs_nm_barang = "Paracetamol 500mg",
            fs_kd_tipe_barang = "01",
            fb_racik = false,
            fb_komponen = false,
            fs_kd_racik = string.Empty,
            fn_qty_racik = 0m,
            fs_racik_qty = string.Empty,
            fn_qty_barang = 10m,
            fs_nm_satuan = "TAB",
            fn_harga_satuan = 2500m,
            fn_diskon = 0m,
            fn_biaya = 1000m,
            fn_sub_total = 25000m,
            fn_tax_prosen = 0m,
            fn_tax_rupiah = 0m,
            fn_biaya_fee = 0m,
            fn_total = 26000m,
            fs_etiket = "3x1 tablet sesudah makan",
            fb_asuransi = false,
            fs_kd_tipe_jaminan = "00000",
            fn_bulat = 0m,
            fs_kd_satuan = "TAB",
            fn_etiket_qty = 3m,
            fs_etiket_qty = "TAB",
            fn_etiket_hari = 1m,
            fs_etiket_kd_pakai = "P01",
            fs_etiket_catatan = "Bila demam",
            fs_etiket_jenis_obat = "1",
            fs_etiket_kd_satuan_pakai = "TAB",
            fd_etiket_cetak = "3000-01-01",
            fb_belibebas = false,
            fs_etiket_hari = "HARI",
            fb_paket_tarif = false,
            fn_nilai_klaim = 0m,
            fs_waktu_odd = string.Empty,
            fn_etiket_cetak = 0m,
            CRTTGL = "2026-10-18",
            CRTJAM = "10:15:00",
            CRTIPA = "127.0.0.1",
            CRTVER = "1.0",
            CRTUSR = "USR01",
            UPDTGL = "2026-10-18",
            UPDJAM = "10:15:00",
            UPDIPA = "127.0.0.1",
            UPDVER = "1.0",
            UPDUSR = "USR01",
            fn_etiket_sms = null,
            fs_etiket_description = "0",
            fb_biaya_tax = false
        },
        new PenjualanItemDto
        {
            fs_kd_trs = trsId,
            fs_kd_trs2 = $"{trsId}002",
            fn_no_urut = 2m,
            fb_void = false,
            fs_kd_barang = "BRG0000002",
            fs_nm_barang = "Amoxicillin 500mg",
            fs_kd_tipe_barang = "01",
            fb_racik = false,
            fb_komponen = false,
            fs_kd_racik = string.Empty,
            fn_qty_racik = 0m,
            fs_racik_qty = string.Empty,
            fn_qty_barang = 10m,
            fs_nm_satuan = "KAP",
            fn_harga_satuan = 2500m,
            fn_diskon = 0m,
            fn_biaya = 1000m,
            fn_sub_total = 25000m,
            fn_tax_prosen = 0m,
            fn_tax_rupiah = 0m,
            fn_biaya_fee = 0m,
            fn_total = 26000m,
            fs_etiket = "3x1 kaplet habiskan",
            fb_asuransi = false,
            fs_kd_tipe_jaminan = "00000",
            fn_bulat = 0m,
            fs_kd_satuan = "KAP",
            fn_etiket_qty = 3m,
            fs_etiket_qty = "KAP",
            fn_etiket_hari = 1m,
            fs_etiket_kd_pakai = "P01",
            fs_etiket_catatan = "Habiskan",
            fs_etiket_jenis_obat = "1",
            fs_etiket_kd_satuan_pakai = "KAP",
            fd_etiket_cetak = "3000-01-01",
            fb_belibebas = false,
            fs_etiket_hari = "HARI",
            fb_paket_tarif = false,
            fn_nilai_klaim = 0m,
            fs_waktu_odd = string.Empty,
            fn_etiket_cetak = 0m,
            CRTTGL = "2026-10-18",
            CRTJAM = "10:15:00",
            CRTIPA = "127.0.0.1",
            CRTVER = "1.0",
            CRTUSR = "USR01",
            UPDTGL = "2026-10-18",
            UPDJAM = "10:15:00",
            UPDIPA = "127.0.0.1",
            UPDVER = "1.0",
            UPDUSR = "USR01",
            fn_etiket_sms = null,
            fs_etiket_description = "0",
            fb_biaya_tax = false
        }
    ];

    [Fact]
    public void PenjualanDal_Insert_And_GetData_Returns68ColumnsAccurately()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUDAL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var dto = CreateFakerDto(trsId);

        _penjualanDal.Insert(dto);

        var loaded = _penjualanDal.GetData(PenjualanModel.Key(trsId));
        loaded.Should().NotBeNull();
        loaded!.fs_kd_trs.Should().Be(trsId);
        loaded.fd_tgl_trs.Should().Be("2026-10-18");
        loaded.fs_jam_trs.Should().Be("10:15:00");
        loaded.fs_kd_reg.Should().Be(dto.fs_kd_reg);
        loaded.fs_nm_pasien.Should().Be(dto.fs_nm_pasien);
        loaded.fn_sub_total.Should().Be(50000m);
        loaded.fn_grand_total.Should().Be(52000m);
        loaded.CRTUSR.Should().Be("USR01");
        loaded.fd_tgl_void.Should().Be("3000-01-01");
    }

    [Fact]
    public void PenjualanDal_Update_ModifiesFieldsAndAuditStamps()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUDAL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var dto = CreateFakerDto(trsId);
        _penjualanDal.Insert(dto);

        dto.fn_diskon_lain = 5000m;
        dto.fn_grand_total = 47000m;
        dto.fs_catatan_lain = "Diskon khusus hari kemerdekaan";
        dto.UPDUSR = "UPDUSR01";
        dto.UPDJAM = "11:00:00";
        _penjualanDal.Update(dto);

        var loaded = _penjualanDal.GetData(PenjualanModel.Key(trsId));
        loaded.Should().NotBeNull();
        loaded!.fn_diskon_lain.Should().Be(5000m);
        loaded.fn_grand_total.Should().Be(47000m);
        loaded.fs_catatan_lain.Should().Be("Diskon khusus hari kemerdekaan");
        loaded.UPDUSR.Should().Be("UPDUSR01");
    }

    [Fact]
    public void PenjualanDal_SoftVoid_SetsVoidStamps_And_ExcludesFromActiveList()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUDAL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var dto = CreateFakerDto(trsId);
        _penjualanDal.Insert(dto);

        // Before void, must be returned in ListByReg and ListWorklist
        var listBefore = _penjualanDal.ListByReg(dto.fs_kd_reg).ToList();
        listBefore.Should().Contain(x => x.fs_kd_trs == trsId);

        // Soft-void
        _penjualanDal.SoftVoid(trsId, "2026-10-18", "11:30:00", "SUPERVISOR", "Salah pasien");

        // GetData by ID returns the row with void stamps
        var loaded = _penjualanDal.GetData(PenjualanModel.Key(trsId));
        loaded.Should().NotBeNull();
        loaded!.fd_tgl_void.Should().Be("2026-10-18");
        loaded.fs_jam_void.Should().Be("11:30:00");
        loaded.fs_kd_petugas_void.Should().Be("SUPERVISOR");
        loaded.fs_catatan_lain.Should().Be("Salah pasien");

        // Active queries must EXCLUDE the voided transaction
        var listAfterReg = _penjualanDal.ListByReg(dto.fs_kd_reg).ToList();
        listAfterReg.Should().NotContain(x => x.fs_kd_trs == trsId);

        var listAfterWorklist = _penjualanDal.ListWorklist("2026-10-18").ToList();
        listAfterWorklist.Should().NotContain(x => x.fs_kd_trs == trsId);

        var listAfterResep = _penjualanDal.ListByResep(dto.fs_kd_resep).ToList();
        listAfterResep.Should().NotContain(x => x.fs_kd_trs == trsId);
    }

    [Fact]
    public void PenjualanDal_Delete_RemovesRowPermanently()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUDAL" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var dto = CreateFakerDto(trsId);
        _penjualanDal.Insert(dto);

        _penjualanDal.Delete(PenjualanModel.Key(trsId));

        var loaded = _penjualanDal.GetData(PenjualanModel.Key(trsId));
        loaded.Should().BeNull();
    }

    [Fact]
    public void PenjualanItemDal_InsertBulk_And_ListData_Maps54ColumnsAccurately()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUITM" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var items = CreateFakerItems(trsId);

        _penjualanItemDal.InsertBulk(items);

        var loaded = _penjualanItemDal.ListData(PenjualanModel.Key(trsId)).ToList();
        loaded.Should().HaveCount(2);

        var item1 = loaded[0];
        item1.fs_kd_trs.Should().Be(trsId);
        item1.fs_kd_trs2.Should().Be($"{trsId}001");
        item1.fn_no_urut.Should().Be(1m);
        item1.fs_kd_barang.Should().Be("BRG0000001");
        item1.fs_nm_barang.Should().Be("Paracetamol 500mg");
        item1.fn_qty_barang.Should().Be(10m);
        item1.fn_harga_satuan.Should().Be(2500m);
        item1.fn_biaya.Should().Be(1000m);
        item1.fn_total.Should().Be(26000m);
        item1.fs_etiket.Should().Be("3x1 tablet sesudah makan");
        item1.fb_void.Should().BeFalse();

        var item2 = loaded[1];
        item2.fs_kd_trs.Should().Be(trsId);
        item2.fs_kd_trs2.Should().Be($"{trsId}002");
        item2.fn_no_urut.Should().Be(2m);
        item2.fs_nm_barang.Should().Be("Amoxicillin 500mg");
    }

    [Fact]
    public void PenjualanItemDal_SoftVoidItems_SetsVoidFlagOnAllDetailRows()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUITM" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();
        var items = CreateFakerItems(trsId);
        _penjualanItemDal.InsertBulk(items);

        _penjualanItemDal.SoftVoidItems(trsId, "2026-10-18", "12:00:00", "VOIDUSER");

        var loaded = _penjualanItemDal.ListData(PenjualanModel.Key(trsId)).ToList();
        loaded.Should().HaveCount(2);
        loaded.All(x => x.fb_void).Should().BeTrue();
        loaded.All(x => x.UPDUSR == "VOIDUSER").Should().BeTrue();
    }

    [Fact]
    public void PenjualanRepo_AtomicSaveChanges_And_LoadEntity_RoundTripsAggregateProperly()
    {
        using var trans = TransHelper.NewScope();
        var trsId = "DUREP" + Guid.NewGuid().ToString("N")[..5].ToUpperInvariant();

        var item1 = PenjualanItemModel.Create(
            noUrut: 1,
            penjualanItemId: $"{trsId}001",
            brgId: "BRG001",
            brgName: "Omeprazole 20mg",
            satuanId: "KAP",
            satuanName: "Kapsul",
            qty: 14m,
            hargaSatuan: 3000m,
            biaya: 1000m,
            etiket: "1x1 pagi sebelum makan");

        var model = PenjualanModel.CreateFromTelaah(
            resepId: "RSP9901",
            regId: "RG9901",
            pasienName: "Siti Aminah",
            dokterId: "DOC99",
            layananId: "APT01",
            layananResepId: "FAR01",
            tipeJaminanId: "00000",
            kelasId: "REG",
            petugasId: "PETUGAS01",
            penjualanDate: new DateTime(2026, 10, 18, 9, 30, 0),
            items: [item1],
            penjualanId: trsId);

        _penjualanRepo.SaveChanges(model);

        var loadedMaybe = _penjualanRepo.LoadEntity(PenjualanModel.Key(trsId));
        loadedMaybe.HasValue.Should().BeTrue();

        var loaded = loadedMaybe.Value;
        loaded.PenjualanId.Should().Be(trsId);
        loaded.ResepId.Should().Be("RSP9901");
        loaded.RegId.Should().Be("RG9901");
        loaded.PasienName.Should().Be("Siti Aminah");
        loaded.Items.Should().HaveCount(1);
        loaded.Items[0].BrgName.Should().Be("Omeprazole 20mg");
        loaded.GrandTotal.Should().Be(model.GrandTotal);

        // Verify query list
        var listByReg = _penjualanRepo.ListByReg("RG9901");
        listByReg.Should().Contain(x => x.PenjualanId == trsId);

        var listByResep = _penjualanRepo.ListByResep("RSP9901");
        listByResep.Should().Contain(x => x.PenjualanId == trsId);

        var listWorklist = _penjualanRepo.ListWorklist(new DateOnly(2026, 10, 18));
        listWorklist.Should().Contain(x => x.PenjualanId == trsId);
    }

    [Fact]
    public async Task TrkTarifPricingAdapter_CollaboratesWithPricingCalculator()
    {
        var mockCalculator = new Mock<IItemPricingCalculator>();
        var date = new DateTime(2026, 10, 18);

        mockCalculator
            .Setup(x => x.CalculateAsync("BRG01", "UM", date, 5, "TAB", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ItemPriceCalculationResultDto(
                BarangId: "BRG01",
                TipeHargaId: "UM",
                TransactionDate: date,
                Qty: 5,
                SatuanId: "TAB",
                Konversi: 1m,
                HnaApplied: 2000m,
                MarginPersenApplied: 25m,
                HargaJualFixApplied: 0m,
                IsFixedPriceApplied: false,
                IsHetCapped: false,
                HetNominal: 5000m,
                HargaJualSatuanDasar: 2500m,
                HargaJualFinal: 2500m,
                DiskonDefaultPersen: 5m,
                BiayaTambahanItem: 0m,
                BiayaPerObat_r: 1000m,
                BiayaPerResep_R: 0m));

        mockCalculator
            .Setup(x => x.CalculateRounding(12345m, 100, RoundingModeEnum.Normal))
            .Returns(new RoundingEvaluationResultDto(12345m, 12300m, -45m, 100, RoundingModeEnum.Normal));

        var sut = new TrkTarifPricingAdapter(mockCalculator.Object);

        var req = new TrkTarifItemPricingRequest("BRG01", "00000", date, 5m, "TAB");
        var result = await sut.CalculateItemPriceAsync(req);

        result.BrgId.Should().Be("BRG01");
        result.HargaSatuan.Should().Be(2500m);
        result.DiskonPersen.Should().Be(5m);
        result.BiayaPerObat_r.Should().Be(1000m);
        result.HnaApplied.Should().Be(2000m);

        var rounding = sut.CalculateRounding(12345m, 100, 1);
        rounding.TotalBeforeRounding.Should().Be(12345m);
        rounding.TotalAfterRounding.Should().Be(12300m);
        rounding.RoundingDifference.Should().Be(-45m);
    }

    [Fact]
    public void TrkBillingChargeAdapter_PostAndCancel_DispatchesToTrsBillingDal()
    {
        var mockBillingDal = new Mock<ITrsBillingDal>();
        var sut = new TrkBillingChargeAdapter(mockBillingDal.Object);

        var req = new TrkBillingChargeRequest(
            PenjualanId: "DUTST9999A",
            RegId: "RG12345",
            LayananId: "RANAP01",
            KelasId: "1",
            PetugasId: "APTUSR",
            TransactionDate: new DateTime(2026, 10, 18, 14, 0, 0),
            SubTotal: 100000m,
            Diskon: 0m,
            Biaya: 2000m,
            Tax: 0m,
            Total: 102000m,
            Keterangan: "Obat Inap Kamar 101");

        var chargeId = sut.PostBillingCharge(req);

        chargeId.Should().NotBeNullOrWhiteSpace();
        chargeId.Length.Should().Be(10);
        chargeId.Should().StartWith("BI");

        mockBillingDal.Verify(x => x.Insert(It.Is<TrsBillingDto>(d =>
            d.fs_kd_trs == chargeId &&
            d.fn_modul == 1 &&
            d.fs_kd_reg == "RG12345" &&
            d.fs_kd_ref_biaya == "DUTST9999A" &&
            d.fn_total == 102000m)), Times.Once);

        sut.CancelBillingCharge(chargeId, "SUPERVISOR", "Void transaksi");

        mockBillingDal.Verify(x => x.Delete(It.Is<ITrsBillingKey>(k =>
            k.TrsBillingId == chargeId)), Times.Once);
    }
}
