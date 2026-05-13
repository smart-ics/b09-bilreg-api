using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.PaymentContext.TrsBillingFeature;
using FluentAssertions;
using System.Globalization;

namespace Bilreg.Test.PaymentContext.TrsBillingFeature;

public class TrsBillingTypeTests
{
    #region EXISTING TESTS (UNCHANGED)

    [Fact]
    public void UT1_GivenValidParameters_WhenCreateFromTindakan_ThenTrsBillingTypeIsCreated()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.Should().NotBeNull();
        result.TrsBillingId.Should().Be(tindakan.TindakanId);
        result.Modul.Should().Be(0);
        result.TglTrs.Should().Be(tindakan.TindakanDate);
        result.Reg.Should().BeEquivalentTo(tindakan.Reg);
        result.Layanan.Should().BeEquivalentTo(tindakan.Layanan);
        result.Kelas.Should().BeEquivalentTo(tindakan.Kelas);
        result.SubTotal.Should().Be(tindakan.Total);
        result.Diskon.Should().Be(0);
        result.Tax.Should().Be(0);
        result.Biaya.Should().Be(0);
        result.Total.Should().Be(result.SubTotal - result.Diskon + result.Tax + result.Biaya);
        result.Keterangan.Keterangan.Should().Be(tarif.TarifName);
        result.Keterangan.RefBiaya.Should().Be(tarif.TarifId);
        result.ListTrsBilling2.Should().NotBeEmpty();
    }

    [Fact]
    public void UT2_GivenMismatchedTarif_WhenCreateFromTindakan_ThenArgumentExceptionIsThrown()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var mismatchedTarif = CreateTestTarifTypeWithDifferentId();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TrsBillingType.CreateFromTindakan(tindakan, reg, mismatchedTarif, jaminan, listReffKomp));
    }

    [Fact]
    public void UT3_GivenMismatchedJaminan_WhenCreateFromTindakan_ThenArgumentExceptionIsThrown()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var mismatchedJaminan = CreateTestJaminanTypeWithDifferentId();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act & Assert
        Assert.Throws<ArgumentException>(() => TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, mismatchedJaminan, listReffKomp));
    }

    [Fact]
    public void UT4_GivenRegInap_WhenCreateFromTindakan_ThenRanapRekeningIsUsed()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModelInap();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.ListTrsBilling2.Should().NotBeEmpty();
        var trsBilling2 = result.ListTrsBilling2.First();
        // Verify that Ranap rekening is used based on JenisRegEnum
    }

    [Fact]
    public void UT5_GivenRegJalan_WhenCreateFromTindakan_ThenRajalRekeningIsUsed()
    {
        // Arrange
        var tindakan = CreateTestTindakanModel();
        var reg = CreateTestRegModel();
        var tarif = CreateTestTarifType();
        var jaminan = CreateTestJaminanType();
        var listReffKomp = new List<KomponenType> { CreateTestKomponenType() };

        // Act
        var result = TrsBillingType.CreateFromTindakan(tindakan, reg, tarif, jaminan, listReffKomp);

        // Assert
        result.ListTrsBilling2.Should().NotBeEmpty();
        var trsBilling2 = result.ListTrsBilling2.First();
        // Verify that Rajal rekening is used based on JenisRegEnum
    }

    [Fact]
    public void UT6_GivenDefaultModel_WhenTotalCalculated_ThenCorrectTotalIsReturned()
    {
        // Arrange
        var billing = TrsBillingType.Default with
        {
            SubTotal = 1000,
            Diskon = 100,
            Tax = 50,
            Biaya = 25
        };

        // Act
        var total = billing.Total;

        // Assert
        total.Should().Be(975); // 1000 - 100 + 50 + 25
    }

    [Fact]
    public void UT7_GivenTrsBillingWithItems_WhenAddTrsBilling2_ThenItemIsAddedToList()
    {
        // Arrange
        var billing = TrsBillingType.Default;
        var trsBilling2 = CreateTestTrsBilling2();

        // Act
        billing.AddTrsBilling2(trsBilling2);

        // Assert
        billing.ListTrsBilling2.Should().Contain(trsBilling2);
        billing.ListTrsBilling2.Count().Should().Be(1);
    }

    #endregion

    #region NEW TESTS: TrsBilling2GenRegOut.CreateFromRegKeluar

    [Fact]
    public void GivenValidRegOutParameters_WhenCreateFromRegKeluar_ThenJasaTypeCreatedWithCorrectKasir()
    {
        // === ARRANGE ===
        var tglJamKeluar = "2024-01-15 10:30:00";
        var userId = "KASIR01";

        var pembayaran = new[] {
            CreateTestRegPembayaranType("REG001", "BYKAS", nilaiJasa: 100000, nilaiObat: 0)
        };

        var existingBilling = new[] {
            CreateTestTrsBilling2Jasa("BILL001", 1, "PAY001", "MED01", "JASA01", sisa: 50000)
        };

        var jenisBayarMap = new Dictionary<string, string> { ["BYKAS"] = "KAS" };

        // === ACT ===
        var result = TrsBilling2GenRegOut.CreateFromRegKeluar(
            pembayaran, existingBilling, jenisBayarMap, tglJamKeluar, userId).ToList();

        // === ASSERT ===
        result.Should().NotBeEmpty();
        var billing = result.First();

        billing.Should().BeOfType<TrsBilling2JasaType>();
        var jasa = (TrsBilling2JasaType)billing;

        jasa.Kasir.PegId.Should().Be(userId);  // ✅ Kasir dari UserId
        jasa.PaymentDate.Should().Be(DateTime.ParseExact(tglJamKeluar, "yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture));
        jasa.NilaiBilling.JenisBayar.Should().Be("KAS");
        jasa.NilaiBilling.NilaiN.Should().Be(100000);  // Full amount distributed
        jasa.Komponen.KomponenId.Should().Be("JASA01");
    }

    [Fact]
    public void GivenObatPayment_WhenCreateFromRegKeluar_ThenObatTypeCreated()
    {
        // === ARRANGE ===
        var tglJamKeluar = "2024-01-15 11:00:00";
        var userId = "KASIR02";

        var pembayaran = new[] {
            CreateTestRegPembayaranType("REG002", "BYKAS", nilaiJasa: 0, nilaiObat: 75000)
        };

        var existingBilling = new[] {
            CreateTestTrsBilling2Obat("BILL002", 1, "PAY002", "GR01", sisa: 75000)
        };

        var jenisBayarMap = new Dictionary<string, string> { ["BYKAS"] = "KAS" };

        // === ACT ===
        var result = TrsBilling2GenRegOut.CreateFromRegKeluar(
            pembayaran, existingBilling, jenisBayarMap, tglJamKeluar, userId).ToList();

        // === ASSERT ===
        result.Should().NotBeEmpty();
        var billing = result.First();

        billing.Should().BeOfType<TrsBilling2ObatType>();
        var obat = (TrsBilling2ObatType)billing;

        obat.Kasir.PegId.Should().Be(userId);
        obat.GroupRek.GroupRekId.Should().Be("GR01");
        obat.NilaiBilling.NilaiN.Should().Be(75000);
    }

    [Fact]
    public void GivenZeroPaymentValues_WhenCreateFromRegKeluar_ThenEmptyResultIsReturned()
    {
        // === ARRANGE ===
        var tglJamKeluar = "2024-01-15 12:00:00";
        var userId = "KASIR03";

        var pembayaran = new[] {
            CreateTestRegPembayaranType("REG003", "BYKAS", nilaiJasa: 0, nilaiObat: 0)
        };

        var existingBilling = Enumerable.Empty<TrsBilling2Base>();
        var jenisBayarMap = new Dictionary<string, string>();

        // === ACT ===
        var result = TrsBilling2GenRegOut.CreateFromRegKeluar(
            pembayaran, existingBilling, jenisBayarMap, tglJamKeluar, userId).ToList();

        // === ASSERT ===
        result.Should().BeEmpty();
    }

    [Fact]
    public void GivenFinancialPrecisionDistribution_WhenCreateFromRegKeluar_ThenTotalAmountIsAccurate()
    {
        // === ARRANGE ===
        var tglJamKeluar = "2024-01-15 13:00:00";
        var userId = "KASIR04";

        // 100 to distribute among 3 items with basis 33, 33, 34 → should sum to exactly 100
        var pembayaran = new[] {
            CreateTestRegPembayaranType("REG004", "BYKAS", nilaiJasa: 100, nilaiObat: 0)
        };

        var existingBilling = new[] {
            CreateTestTrsBilling2Jasa("BILL004", 1, "PAY004", "MED01", "JASA01", sisa: 33),
            CreateTestTrsBilling2Jasa("BILL004", 2, "PAY004", "MED01", "JASA02", sisa: 33),
            CreateTestTrsBilling2Jasa("BILL004", 3, "PAY004", "MED01", "JASA03", sisa: 34)
        };

        var jenisBayarMap = new Dictionary<string, string> { ["BYKAS"] = "KAS" };

        // === ACT ===
        var result = TrsBilling2GenRegOut.CreateFromRegKeluar(
            pembayaran, existingBilling, jenisBayarMap, tglJamKeluar, userId).ToList();

        // === ASSERT ===
        result.Should().HaveCount(3);
        var totalDistributed = result.Sum(x => x.NilaiBilling.NilaiN);
        totalDistributed.Should().Be(100);  // ✅ Rounding adjustment ensures no penny loss

        // Verify last item received the rounding adjustment
        var lastItem = result.OrderByDescending(x => x.NoUrut).First();
        lastItem.NilaiBilling.NilaiN.Should().BeGreaterOrEqualTo(34);  // Last item gets remainder
    }

    [Fact]
    public void GivenJenisBayarMapping_WhenCreateFromRegKeluar_ThenResolvedJenisBayarIsUsed()
    {
        // === ARRANGE ===
        var tglJamKeluar = "2024-01-15 14:00:00";
        var userId = "KASIR05";

        var pembayaran = new[] {
            CreateTestRegPembayaranType("REG005", "BYPRI", nilaiJasa: 50000, nilaiObat: 0)  // BYPRI → HUT
        };

        var existingBilling = new[] {
            CreateTestTrsBilling2Jasa("BILL005", 1, "PAY005", "MED01", "JASA01", sisa: 50000)
        };

        // Map BYPRI → HUT
        var jenisBayarMap = new Dictionary<string, string> { ["BYPRI"] = "HUT" };

        // === ACT ===
        var result = TrsBilling2GenRegOut.CreateFromRegKeluar(
            pembayaran, existingBilling, jenisBayarMap, tglJamKeluar, userId).ToList();

        // === ASSERT ===
        result.Should().NotBeEmpty();
        result.First().NilaiBilling.JenisBayar.Should().Be("HUT");  // ✅ Mapped value used
    }

    #endregion

    #region NEW TESTS: TaTrsBilling2Dto Conversion

    [Fact]
    public void GivenTrsBilling2JasaType_WhenFromModel_ThenDtoHasKomponenIdAndEmptyGroupRek()
    {
        // === ARRANGE ===
        var jasa = CreateTestTrsBilling2Jasa("BILL006", 1, "RO123456", "MED01", "JASA01", sisa: 10000);

        // === ACT ===
        var dto = TaTrsBilling2Dto.FromModel(jasa, "BILL006");

        // === ASSERT ===
        dto.fs_kd_detil_tarif.Should().Be("JASA01");  // ✅ KomponenId terisi
        dto.fs_kd_grup_rek.Should().BeEmpty();         // ✅ GroupRek kosong untuk Jasa
        dto.fs_kd_trs_bayar.Should().Be("RO123456");
        dto.fd_tgl_bayar.Should().Be("2024-01-15");    // Assumes PaymentDate in helper
        dto.fs_jam_bayar.Should().Be("10:30:00");
    }

    [Fact]
    public void GivenTrsBilling2ObatType_WhenFromModel_ThenDtoHasGroupRekIdAndEmptyKomponen()
    {
        // === ARRANGE ===
        var obat = CreateTestTrsBilling2Obat("BILL007", 1, "RO654321", "GR01", sisa: 25000);

        // === ACT ===
        var dto = TaTrsBilling2Dto.FromModel(obat, "BILL007");

        // === ASSERT ===
        dto.fs_kd_grup_rek.Should().Be("GR01");        // ✅ GroupRekId terisi
        dto.fs_kd_detil_tarif.Should().BeEmpty();       // ✅ Komponen kosong untuk Obat
        dto.fs_kd_trs_bayar.Should().Be("RO654321");
    }

    [Fact]
    public void GivenJasaDto_WhenToModel_ThenReturnsTrsBilling2JasaType()
    {
        // === ARRANGE ===
        var dto = new TaTrsBilling2Dto(
            fs_kd_trs: "BILL008", fn_no_urut: 1, fs_kd_jenis_bayar: "KAS", fn_trs_p: 10000, fn_trs_n: 0,
            fs_kd_trs_bayar: "RO111111", fd_tgl_bayar: "2024-01-15", fs_jam_bayar: "10:30:00",
            fs_kd_petugas_kasir: "KAS01", fs_kd_petugas_medis: "MED01",
            fs_kd_detil_tarif: "JASA01", fs_kd_grup_rek: "",  // ✅ Terisi → Jasa
            fs_kd_rek_ppdp: "", fs_kd_rek_pdpt: "", fs_kd_rek_disc: "",
            fs_kd_rek_pdpt_lain: "", fs_kd_rek_persediaan: "", fs_kd_rek_tax: "", fs_kd_rek_retur: "",
            fs_nm_detil_tarif: "Jasa Medis", fs_nm_grup_rek: "",
            fs_nm_peg_kasir: "Kasir 1", fs_nm_peg_medis: "Dr. A");

        // === ACT ===
        var model = dto.ToModel();

        // === ASSERT ===
        model.Should().BeOfType<TrsBilling2JasaType>();
        var jasa = (TrsBilling2JasaType)model;
        jasa.Komponen.KomponenId.Should().Be("JASA01");
        jasa.Ppa.PpaId.Should().Be("MED01");
        jasa.Kasir.PegId.Should().Be("KAS01");
    }

    [Fact]
    public void GivenObatDto_WhenToModel_ThenReturnsTrsBilling2ObatType()
    {
        // === ARRANGE ===
        var dto = new TaTrsBilling2Dto(
            fs_kd_trs: "BILL009", fn_no_urut: 1, fs_kd_jenis_bayar: "KAS", fn_trs_p: 25000, fn_trs_n: 0,
            fs_kd_trs_bayar: "RO222222", fd_tgl_bayar: "2024-01-15", fs_jam_bayar: "11:00:00",
            fs_kd_petugas_kasir: "KAS02", fs_kd_petugas_medis: "",
            fs_kd_detil_tarif: "", fs_kd_grup_rek: "GR01",  // ✅ Terisi → Obat
            fs_kd_rek_ppdp: "", fs_kd_rek_pdpt: "", fs_kd_rek_disc: "",
            fs_kd_rek_pdpt_lain: "", fs_kd_rek_persediaan: "", fs_kd_rek_tax: "", fs_kd_rek_retur: "",
            fs_nm_detil_tarif: "", fs_nm_grup_rek: "Grup Obat",
            fs_nm_peg_kasir: "Kasir 2", fs_nm_peg_medis: "");

        // === ACT ===
        var model = dto.ToModel();

        // === ASSERT ===
        model.Should().BeOfType<TrsBilling2ObatType>();
        var obat = (TrsBilling2ObatType)model;
        obat.GroupRek.GroupRekId.Should().Be("GR01");
        obat.Kasir.PegId.Should().Be("KAS02");
    }

    #endregion

    #region HELPER METHODS

    private static TindakanModel CreateTestTindakanModel()
    {
        var tdkKomponen = new List<TindakanKomponenWithoutPpaType>
        {
            new TindakanKomponenWithoutPpaType(KomponenType.Default.ToReff(), 1, 200, 1, 200)
        };
        return new TindakanModel(
            "-", DateTime.Today, "", RegModel.Default.ToReff(),
            LayananType.Default.ToReff(), KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(), TarifType.Default.ToReff(),
            tdkKomponen, AuditTrailType.Default);
    }

    private static RegModel CreateTestRegModel() => CreateRegWithJenis(JenisRegEnum.RegJalan);
    private static RegModel CreateTestRegModelInap() => CreateRegWithJenis(JenisRegEnum.RegInap);

    private static RegModel CreateRegWithJenis(JenisRegEnum jenisReg)
    {
        var tipeJaminan = new TipeJaminanReff("J0011", "Jaminan Test");
        return new RegModel("-", new DateOnly(3000, 1, 1),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            jenisReg, PasienModel.Default.ToReff(), tipeJaminan,
            PolisModel.Default.ToReff(), KelasType.Default.ToReff(), CaraMasukDkType.Default,
            RujukanType.Default.ToReff(), PpaType.Default.ToReff(), LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(), "-", "-", []);
    }

    private static TarifType CreateTestTarifType() => TarifType.Default;
    private static TarifType CreateTestTarifTypeWithDifferentId() => TarifType.Default with { TarifId = "DIFFERENT" };
    private static JaminanType CreateTestJaminanType() => JaminanType.Default with { JaminanId = "J00" };
    private static JaminanType CreateTestJaminanTypeWithDifferentId() => JaminanType.Default with { JaminanId = "DIFFERENT" };
    private static KomponenType CreateTestKomponenType() => KomponenType.Default;

    private static TrsBilling2Base CreateTestTrsBilling2()
        => new TrsBilling2JasaType("TU001", 0, "A", DateTime.Now, new NilaiBillingType("B", 100, 0),
            PpaType.Default.ToReff(), PegType.Default, KomponenType.Default.ToReff(), new RekJasaType("C", "D", "E"));

    // ✅ NEW HELPERS FOR REGOUT TESTS
    private static RegPembayaranType CreateTestRegPembayaranType(string regId, string caraBayarId, decimal nilaiJasa, decimal nilaiObat)
        => new RegPembayaranType(regId, caraBayarId, $"{caraBayarId} Name", nilaiJasa, nilaiObat, nilaiJasa + nilaiObat);

    private static TrsBilling2JasaType CreateTestTrsBilling2Jasa(string billingId, int noUrut, string paymentId, string medisId, string komponenId, decimal sisa)
        => new TrsBilling2JasaType(
            TrsBillingId: billingId, NoUrut: noUrut, PaymentId: paymentId,
            PaymentDate: DateTime.Parse("2024-01-15 10:30:00"),
            NilaiBilling: new NilaiBillingType("KAS", sisa, 0),
            Ppa: new PpaReff(medisId, "Dr. Test"),
            Kasir: PegType.Create("KSR_OLD", "Kasir Lama"),
            Komponen: new KomponenReff(komponenId, "Komponen Test"),
            Rekening: new RekJasaType("", "", ""));

    private static TrsBilling2ObatType CreateTestTrsBilling2Obat(string billingId, int noUrut, string paymentId, string groupRekId, decimal sisa)
        => new TrsBilling2ObatType(
            TrsBillingId: billingId, NoUrut: noUrut, PaymentId: paymentId,
            PaymentDate: DateTime.Parse("2024-01-15 10:30:00"),
            NilaiBilling: new NilaiBillingType("KAS", sisa, 0),
            Kasir: PegType.Create("KSR_OLD", "Kasir Lama"),
            GroupRek: new GroupRekReff(groupRekId, "Grup Test"),
            Rekening: new RekObatType("", "", "", "", "", "", ""));

    #endregion
}