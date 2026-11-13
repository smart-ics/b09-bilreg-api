using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanModelTest
{
    private const string TestUserId = "USER-01";
    private const string UserId = "USER-01";
    private readonly DateTime _occurredAt = new(2026, 10, 20, 9, 30, 0);

    [Fact]
    public void Default_ShouldHaveExpectedInitialValues()
    {
        // Act
        var result = TindakanModel.Default;

        // Assert
        result.Should().NotBeNull();
        result.TindakanId.Should().Be("-");
        result.PakaiBedId.Should().BeEmpty();
        result.Qty.Should().Be(1);
        result.Total.Should().Be(0m);
        result.ListKomponen.Should().BeEmpty();
        result.AuditTrail.IsVoided.Should().BeFalse();
    }

    [Fact]
    public void Create_WithDefaultQtyAndBed_ShouldSetQty1AndEmptyBed()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();

        // Act
        var result = TindakanModel.Create(reg, layanan, nilaiTarif, [], TestUserId, _occurredAt);

        // Assert
        result.Should().NotBeNull();
        result.TindakanId.Should().StartWith("TDK");
        result.PakaiBedId.Should().BeEmpty();
        result.Qty.Should().Be(1);
        result.AuditTrail.Created.UserId.Should().Be(TestUserId);
        result.AuditTrail.Created.Timestamp.Should().Be(_occurredAt);
        result.ListKomponen.Should().HaveCount(2);

        var listKomp = result.ListKomponen.ToList();
        listKomp[0].NoUrut.Should().Be(1);
        listKomp[0].Nilai.Should().Be(10000m);
        listKomp[0].Qty.Should().Be(1);
        listKomp[0].SubTotal.Should().Be(10000m);

        listKomp[1].NoUrut.Should().Be(2);
        listKomp[1].Nilai.Should().Be(25000m);
        listKomp[1].Qty.Should().Be(1);
        listKomp[1].SubTotal.Should().Be(25000m);

        result.Total.Should().Be(35000m);
    }

    [Fact]
    public void Create_WithCustomQtyAndBed_ShouldPropagateValues()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();
        const string pakaiBedId = "BED-001";
        const int qty = 3;

        // Act
        var result = TindakanModel.Create(
            reg, layanan, nilaiTarif, [], TestUserId, _occurredAt, pakaiBedId, qty);

        // Assert
        result.PakaiBedId.Should().Be(pakaiBedId);
        result.Qty.Should().Be(qty);

        var listKomp = result.ListKomponen.ToList();
        listKomp[0].Qty.Should().Be(qty);
        listKomp[0].SubTotal.Should().Be(10000m * qty);
        listKomp[1].Qty.Should().Be(qty);
        listKomp[1].SubTotal.Should().Be(25000m * qty);
        result.Total.Should().Be(35000m * qty);
    }

    [Fact]
    public void Create_WithPpaKomponen_ShouldAssignPpaAndCalculateSubTotal()
    {
        // Arrange
        var satTugas = new SatTugasType("ST01", "Dokter Spesialis", ProfesiType.Dokter);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        var ppa = new PpaType(
            "PPA01", "dr. Subroto, Sp.A", "dr. Subroto, Sp.A",
            SmfType.Default, GroupSpesialisType.Default, [], [ppaSatTugas], []);

        var kompMedis = new KomponenType(
            "KM01", "Jasa Dokter", GroupKomponenType.Default,
            CoaType.Default, CoaType.Default, [satTugas]);

        var nilaiTarif = new NilaiTarifType(
            "NT02", "TRF02", "Konsul Spesialis",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 50000m,
            [new NilaiTarifKomponenType(1, kompMedis.ToReff(), 50000m)]);

        var listPpaView = new List<KomponenPpaView>
        {
            new(kompMedis, ppa)
        };

        const int qty = 2;

        // Act
        var result = TindakanModel.Create(
            RegModel.Default, LayananType.Default, nilaiTarif, listPpaView, TestUserId, _occurredAt, "BED-01", qty);

        // Assert
        result.ListKomponen.Should().ContainSingle();
        var item = result.ListKomponen.First();
        item.Should().BeOfType<TindakanKomponenWithPpaType>();

        var itemWithPpa = (TindakanKomponenWithPpaType)item;
        itemWithPpa.Ppa.PpaId.Should().Be("PPA01");
        itemWithPpa.Ppa.PpaName.Should().Be("dr. Subroto, Sp.A");
        itemWithPpa.Qty.Should().Be(qty);
        itemWithPpa.Nilai.Should().Be(50000m);
        itemWithPpa.SubTotal.Should().Be(100000m);
        result.Total.Should().Be(100000m);
    }

    [Fact]
    public void Constructor_WhenDefaultQty_ShouldSetQtyToOne()
    {
        // Arrange & Act
        var tindakan = new TindakanModel(
            "TDK01", _occurredAt, "",
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [],
            AuditTrailType.Default);

        // Assert
        tindakan.Qty.Should().Be(1);
    }

    [Fact]
    public void Constructor_WhenCustomQty_ShouldSetQtyCorrectly()
    {
        // Arrange & Act
        var tindakan = new TindakanModel(
            "TDK01", _occurredAt, "",
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [],
            AuditTrailType.Default,
            qty: 5);

        // Assert
        tindakan.Qty.Should().Be(5);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-5)]
    public void Constructor_WhenQtyIsLessThanOrEqualToZero_ShouldThrowArgumentException(int invalidQty)
    {
        // Arrange & Act
        var act = () => new TindakanModel(
            "TDK01", _occurredAt, "",
            RegModel.Default.ToReff(),
            LayananType.Default.ToReff(),
            KelasType.Default.ToReff(),
            TipeTarifType.Default.ToReff(),
            TarifType.Default.ToReff(),
            [],
            AuditTrailType.Default,
            qty: invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty tindakan minimal 1*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WithQtyLessThanOne_ShouldThrowArgumentException(int invalidQty)
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();

        // Act
        var act = () => TindakanModel.Create(
            reg, layanan, nilaiTarif, [], TestUserId, _occurredAt, "BED-01", invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty*");
    }

    [Fact]
    public void Create_WhenDefaultQty_ShouldHaveQtyOneAndTotalEqualsSumOfNilai()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, UserId, _occurredAt);

        // Assert
        tindakan.Qty.Should().Be(1);
        tindakan.Total.Should().Be(80000m);
        tindakan.ListKomponen.Should().HaveCount(2);

        var kompWithoutPpa = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompWithoutPpa.Qty.Should().Be(1);
        kompWithoutPpa.Nilai.Should().Be(50000m);
        kompWithoutPpa.SubTotal.Should().Be(50000m);

        var kompWithPpa = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompWithPpa.Qty.Should().Be(1);
        kompWithPpa.Nilai.Should().Be(30000m);
        kompWithPpa.SubTotal.Should().Be(30000m);
    }

    [Fact]
    public void Create_WhenCustomQty_ShouldMultiplyComponentSubTotalAndTotalProportionally()
    {
        // Arrange
        const int qty = 3;
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, UserId, _occurredAt, qty: qty);

        // Assert
        tindakan.Qty.Should().Be(qty);
        tindakan.Total.Should().Be(80000m * qty); // 240000m
        tindakan.ListKomponen.Should().HaveCount(2);

        var kompWithoutPpa = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompWithoutPpa.Qty.Should().Be(qty);
        kompWithoutPpa.Nilai.Should().Be(50000m);
        kompWithoutPpa.SubTotal.Should().Be(150000m);

        var kompWithPpa = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompWithPpa.Qty.Should().Be(qty);
        kompWithPpa.Nilai.Should().Be(30000m);
        kompWithPpa.SubTotal.Should().Be(90000m);
    }

    [Fact]
    public void Save_WithSpecifiedQtyAndPakaiBedId_ShouldPreserveValuesAndCalculateTotal()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();
        var auditTrail = AuditTrailType.Create(TestUserId, _occurredAt);
        const string tindakanId = "TDK-EXISTING-01";
        const string pakaiBedId = "BED-VIP-03";
        const int qty = 5;

        // Act
        var result = TindakanModel.Save(
            tindakanId, reg, layanan, nilaiTarif, [], auditTrail, _occurredAt, pakaiBedId, qty);

        // Assert
        result.TindakanId.Should().Be(tindakanId);
        result.PakaiBedId.Should().Be(pakaiBedId);
        result.Qty.Should().Be(qty);
        result.Total.Should().Be(35000m * qty);
    }

    [Fact]
    public void Save_WhenCustomQty_ShouldMultiplyComponentSubTotalAndTotalProportionally()
    {
        // Arrange
        const int qty = 4;
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(25000m, 15000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };
        var audit = AuditTrailType.Create(UserId, _occurredAt);

        // Act
        var tindakan = TindakanModel.Save("TDK02", reg, layanan, nilaiTarif, listPpa, audit, _occurredAt, qty: qty);

        // Assert
        tindakan.Qty.Should().Be(qty);
        tindakan.Total.Should().Be(40000m * qty); // 160000m
        tindakan.ListKomponen.Should().HaveCount(2);

        var komp1 = tindakan.ListKomponen.First(x => x.Komponen.KomponenId == "KOMP-01");
        komp1.Qty.Should().Be(qty);
        komp1.SubTotal.Should().Be(25000m * qty);

        var komp2 = tindakan.ListKomponen.First(x => x.Komponen.KomponenId == "KOMP-02");
        komp2.Qty.Should().Be(qty);
        komp2.SubTotal.Should().Be(15000m * qty);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-3)]
    public void Save_WhenQtyIsLessThanOrEqualToZero_ShouldThrowArgumentException(int invalidQty)
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(25000m, 15000m);
        var audit = AuditTrailType.Create(UserId, _occurredAt);

        // Act
        var act = () => TindakanModel.Save("TDK02", reg, layanan, nilaiTarif, [], audit, _occurredAt, qty: invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty tindakan minimal 1*");
    }

    [Fact]
    public void FromReg_WithSpecifiedQtyAndPakaiBedId_ShouldSetLayananFromRegAndPreserveValues()
    {
        // Arrange
        var reg = RegModel.Default;
        var nilaiTarif = CreateSampleNilaiTarif();
        const string pakaiBedId = "BED-RANAP-05";
        const int qty = 2;

        // Act
        var result = TindakanModel.FromReg(
            reg, nilaiTarif, [], TestUserId, _occurredAt, pakaiBedId, qty);

        // Assert
        result.TindakanId.Should().StartWith("TDK");
        result.PakaiBedId.Should().Be(pakaiBedId);
        result.Qty.Should().Be(qty);
        result.Layanan.Should().Be(reg.Layanan);
        result.Total.Should().Be(70000m);
    }

    [Fact]
    public void FromReg_WhenCustomQty_ShouldMultiplyComponentSubTotalAndTotalProportionally()
    {
        // Arrange
        const int qty = 2;
        var reg = RegModel.Default;
        var nilaiTarif = CreateTestNilaiTarif(100000m, 50000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa, UserId, _occurredAt, qty: qty);

        // Assert
        tindakan.Qty.Should().Be(qty);
        tindakan.Total.Should().Be(150000m * qty); // 300000m
        tindakan.ListKomponen.Should().HaveCount(2);

        foreach (var item in tindakan.ListKomponen)
        {
            item.Qty.Should().Be(qty);
            item.SubTotal.Should().Be(item.Nilai * qty);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-2)]
    public void FromReg_WhenQtyIsLessThanOrEqualToZero_ShouldThrowArgumentException(int invalidQty)
    {
        // Arrange
        var reg = RegModel.Default;
        var nilaiTarif = CreateTestNilaiTarif(100000m, 50000m);

        // Act
        var act = () => TindakanModel.FromReg(reg, nilaiTarif, [], UserId, _occurredAt, qty: invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty tindakan minimal 1*");
    }

    [Fact]
    public void Load_ShouldReconstructModelExactly()
    {
        // Arrange
        var regReff = new RegReff("REG01", "PAS01", "Pasien A");
        var layananReff = new LayananReff("LY01", "Bangsal Melati");
        var kelasReff = new KelasReff("K1", "Kelas 1");
        var tipeTarifReff = new TipeTarifReff("TP", "Umum");
        var tarifReff = new TarifReff("TR01", "Tindakan Bedah");
        var auditTrail = AuditTrailType.Create(TestUserId, _occurredAt);
        var komp = new KomponenReff("KM01", "Jasa Sarana");
        var listKomponen = new List<TindakanKomponenBase>
        {
            TindakanKomponenWithoutPpaType.Create(komp, 1, 50000m, 4)
        };

        // Act
        var result = TindakanModel.Load(
            "TDK-LOAD-01", _occurredAt, "ORD-01", regReff, layananReff,
            kelasReff, tipeTarifReff, tarifReff, listKomponen, auditTrail,
            "BED-MELATI-01", 4);

        // Assert
        result.TindakanId.Should().Be("TDK-LOAD-01");
        result.TindakanDate.Should().Be(_occurredAt);
        result.OrderTindakanId.Should().Be("ORD-01");
        result.Reg.Should().Be(regReff);
        result.Layanan.Should().Be(layananReff);
        result.Kelas.Should().Be(kelasReff);
        result.TipeTarif.Should().Be(tipeTarifReff);
        result.Tarif.Should().Be(tarifReff);
        result.PakaiBedId.Should().Be("BED-MELATI-01");
        result.Qty.Should().Be(4);
        result.Total.Should().Be(200000m);
        result.ListKomponen.Should().HaveCount(1);
    }

    [Fact]
    public void Void_WhenValidUserAndDate_ShouldSetVoidedAuditTrail()
    {
        // Arrange
        var model = TindakanModel.Create(
            RegModel.Default, LayananType.Default, CreateSampleNilaiTarif(), [], TestUserId, _occurredAt);
        var voidDate = new DateTime(2026, 10, 20, 11, 0, 0);
        const string voidUser = "SUPERVISOR";

        // Act
        model.Void(voidUser, voidDate);

        // Assert
        model.AuditTrail.IsVoided.Should().BeTrue();
        model.AuditTrail.Voided.UserId.Should().Be(voidUser);
        model.AuditTrail.Voided.Timestamp.Should().Be(voidDate);
    }

    [Fact]
    public void Void_WhenEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var model = TindakanModel.Create(
            RegModel.Default, LayananType.Default, CreateSampleNilaiTarif(), [], TestUserId, _occurredAt);

        // Act
        var act = () => model.Void("", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Key_Helper_ShouldReturnExpectedKeyWithId()
    {
        // Act
        var key = TindakanModel.Key("TDK-999");

        // Assert
        key.TindakanId.Should().Be("TDK-999");
    }

    [Fact]
    public void TindakanKomponenWithoutPpaType_Create_ShouldComputeSubTotal()
    {
        // Arrange
        var komp = new KomponenReff("KM02", "BHP Infus");

        // Act
        var item = TindakanKomponenWithoutPpaType.Create(komp, 1, 15000m, 3);

        // Assert
        item.Komponen.Should().Be(komp);
        item.NoUrut.Should().Be(1);
        item.Nilai.Should().Be(15000m);
        item.Qty.Should().Be(3);
        item.SubTotal.Should().Be(45000m);
    }

    [Fact]
    public void Create_WhenDiskonDokterFalse_ShouldNotDiscountDoctorComponent()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, UserId, _occurredAt, qty: 1, isDiskonDokter: false);

        // Assert
        tindakan.IsDiskonDokter.Should().BeFalse();
        tindakan.DiskonDokter.Should().Be(0m);
        tindakan.Total.Should().Be(80000m);

        var kompDoc = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompDoc.Diskon.Should().Be(0m);
        kompDoc.SubTotal.Should().Be(30000m);

        var kompSarana = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompSarana.Diskon.Should().Be(0m);
        kompSarana.SubTotal.Should().Be(50000m);
    }

    [Fact]
    public void Create_WhenDiskonDokterTrue_WithQtyOne_ShouldDiscountDoctorComponentFully()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, UserId, _occurredAt, qty: 1, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.DiskonDokter.Should().Be(30000m);
        tindakan.Total.Should().Be(50000m); // Sarana only

        var kompDoc = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompDoc.Diskon.Should().Be(30000m);
        kompDoc.SubTotal.Should().Be(0m);

        var kompSarana = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompSarana.Diskon.Should().Be(0m);
        kompSarana.SubTotal.Should().Be(50000m);
    }

    [Fact]
    public void Create_WhenDiskonDokterTrue_WithMultiQty_ShouldDiscountDoctorComponentProportionally()
    {
        // Arrange
        const int qty = 3;
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, UserId, _occurredAt, qty: qty, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.DiskonDokter.Should().Be(30000m * qty); // 90000m
        tindakan.Total.Should().Be(50000m * qty); // 150000m (sarana only)

        var kompDoc = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompDoc.Diskon.Should().Be(90000m);
        kompDoc.SubTotal.Should().Be(0m);

        var kompSarana = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompSarana.Diskon.Should().Be(0m);
        kompSarana.SubTotal.Should().Be(150000m);
    }

    [Fact]
    public void Create_WhenDiskonDokterTrue_WithMultipleDoctorComponents_ShouldTargetDpjpDoctorOnly()
    {
        // Arrange
        var satTugasDokter = new SatTugasType("ST-DOC", "Dokter", ProfesiType.Dokter);
        var satTugasPerawat = new SatTugasType("ST-NURSE", "Perawat", ProfesiType.Perawat);

        var ppaDokterDpjp = new PpaType("DOC-DPJP", "dr. DPJP", "dr. DPJP",
            SmfType.Default, GroupSpesialisType.Default, [], [new PpaSatTugasType(satTugasDokter, true)], []);
        var ppaDokterPendamping = new PpaType("DOC-ASST", "dr. Pendamping", "dr. Pendamping",
            SmfType.Default, GroupSpesialisType.Default, [], [new PpaSatTugasType(satTugasDokter, true)], []);

        var kompSarana = new KomponenType("KOMP-SARANA", "Sarana", GroupKomponenType.Default, CoaType.Default, CoaType.Default, []);
        var kompDokterDpjp = new KomponenType("KOMP-DOC-DPJP", "Jasa DPJP", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugasDokter]);
        var kompDokterAsst = new KomponenType("KOMP-DOC-ASST", "Jasa Pendamping", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugasDokter]);

        var listKomponen = new List<NilaiTarifKomponenType>
        {
            new(1, kompSarana.ToReff(), 40000m),
            new(2, kompDokterDpjp.ToReff(), 35000m),
            new(3, kompDokterAsst.ToReff(), 25000m)
        };

        var nilaiTarif = new NilaiTarifType("NT-MULTI", "TRF-MULTI", "Operasi Kecil",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 100000m, listKomponen);

        var listPpa = new List<KomponenPpaView>
        {
            new(kompDokterDpjp, ppaDokterDpjp),
            new(kompDokterAsst, ppaDokterPendamping)
        };

        var reg = new RegModel(
            "REG-MULTI", new DateOnly(2026, 10, 16),
            AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default, AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            ppaDokterDpjp.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []
        );

        // Act
        var tindakan = TindakanModel.Create(reg, LayananType.Default, nilaiTarif, listPpa, UserId, _occurredAt, qty: 1, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.DiskonDokter.Should().Be(35000m); // Only DPJP doctor discounted
        tindakan.Total.Should().Be(65000m);        // 40k sarana + 25k assistant doctor

        var kompDpjpResult = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>()
            .Single(x => x.Komponen.KomponenId == "KOMP-DOC-DPJP");
        kompDpjpResult.Diskon.Should().Be(35000m);
        kompDpjpResult.SubTotal.Should().Be(0m);

        var kompAsstResult = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>()
            .Single(x => x.Komponen.KomponenId == "KOMP-DOC-ASST");
        kompAsstResult.Diskon.Should().Be(0m);
        kompAsstResult.SubTotal.Should().Be(25000m);
    }

    [Fact]
    public void Save_WhenDiskonDokterTrue_ShouldDiscountDoctorComponentProportionallyAndLeaveRsUntouched()
    {
        // Arrange
        const int qty = 3;
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };
        var audit = AuditTrailType.Create(UserId, _occurredAt);

        // Act
        var tindakan = TindakanModel.Save("TDK02", reg, layanan, nilaiTarif, listPpa, audit, _occurredAt, qty: qty, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.Qty.Should().Be(3);
        tindakan.DiskonDokter.Should().Be(90000m); // 30k * 3
        tindakan.Total.Should().Be(150000m);       // 50k * 3 (sarana RS untouched)

        var kompDoc = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompDoc.Diskon.Should().Be(90000m);
        kompDoc.SubTotal.Should().Be(0m);

        var kompSarana = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompSarana.Diskon.Should().Be(0m);
        kompSarana.SubTotal.Should().Be(150000m);
    }

    [Fact]
    public void FromReg_WhenDiskonDokterTrue_ShouldDiscountDoctorComponentProportionallyAndLeaveRsUntouched()
    {
        // Arrange
        const int qty = 2;
        var reg = RegModel.Default;
        var nilaiTarif = CreateTestNilaiTarif(100000m, 50000m);
        var (komp, ppa) = CreateValidKomponenAndPpa();
        var listPpa = new List<KomponenPpaView> { new(komp, ppa) };

        // Act
        var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa, UserId, _occurredAt, qty: qty, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.Qty.Should().Be(2);
        tindakan.DiskonDokter.Should().Be(100000m); // 50k * 2
        tindakan.Total.Should().Be(200000m);        // 100k * 2 (sarana RS untouched)

        var kompDoc = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().Single();
        kompDoc.Diskon.Should().Be(100000m);
        kompDoc.SubTotal.Should().Be(0m);

        var kompSarana = tindakan.ListKomponen.OfType<TindakanKomponenWithoutPpaType>().Single();
        kompSarana.Diskon.Should().Be(0m);
        kompSarana.SubTotal.Should().Be(200000m);
    }

    private static NilaiTarifType CreateSampleNilaiTarif()
    {
        var komp1 = new KomponenReff("K01", "Jasa Perawat");
        var komp2 = new KomponenReff("K02", "BHP Medis");
        var listKomp = new List<NilaiTarifKomponenType>
        {
            new(1, komp1, 10000m),
            new(2, komp2, 25000m)
        };

        return new NilaiTarifType(
            "NT01", "TRF01", "Injeksi Obat",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 35000m, listKomp);
    }

    private static NilaiTarifType CreateTestNilaiTarif(decimal nilaiKomp1 = 50000m, decimal nilaiKomp2 = 30000m)
    {
        var komp1 = new KomponenReff("KOMP-01", "Jasa Sarana");
        var komp2 = new KomponenReff("KOMP-02", "Jasa Pelayanan");

        var listKomponen = new List<NilaiTarifKomponenType>
        {
            new(1, komp1, nilaiKomp1),
            new(2, komp2, nilaiKomp2)
        };

        return new NilaiTarifType(
            "NT01",
            "TRF01",
            "Pemeriksaan Dokter",
            TipeTarifType.Default.ToReff(),
            KelasType.Default.ToReff(),
            nilaiKomp1 + nilaiKomp2,
            listKomponen
        );
    }

    private static (KomponenType komp, PpaType ppa) CreateValidKomponenAndPpa()
    {
        var satTugas = new SatTugasType("ST01", "Dokter Umum", ProfesiType.Dokter);
        var ppaSatTugas = new PpaSatTugasType(satTugas, true);
        var ppa = new PpaType("PPA01", "dr. Budi", "dr. Budi", SmfType.Default, GroupSpesialisType.Default,
            [], [ppaSatTugas], []);

        var komp = new KomponenType("KOMP-02", "Jasa Pelayanan", GroupKomponenType.Default, CoaType.Default, CoaType.Default,
            [satTugas]);

        return (komp, ppa);
    }
}
