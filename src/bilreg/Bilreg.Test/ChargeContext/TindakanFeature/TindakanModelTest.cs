using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanModelTest
{
    private const string TestUserId = "USER-01";
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
        listKomp[0].Qty.Should().Be(1);
        listKomp[0].Nilai.Should().Be(10000m);
        listKomp[0].SubTotal.Should().Be(10000m);

        listKomp[1].Qty.Should().Be(1);
        listKomp[1].Nilai.Should().Be(25000m);
        listKomp[1].SubTotal.Should().Be(25000m);

        result.Total.Should().Be(35000m);
    }

    [Fact]
    public void Create_WithSpecifiedQtyAndPakaiBedId_ShouldSetValuesAndComputeSubtotalsAndTotal()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();
        const string pakaiBedId = "BED-ICU-01";
        const int qty = 3;

        // Act
        var result = TindakanModel.Create(
            reg, layanan, nilaiTarif, [], TestUserId, _occurredAt, pakaiBedId, qty);

        // Assert
        result.Should().NotBeNull();
        result.PakaiBedId.Should().Be(pakaiBedId);
        result.Qty.Should().Be(qty);

        var listKomp = result.ListKomponen.ToList();
        listKomp[0].Qty.Should().Be(qty);
        listKomp[0].Nilai.Should().Be(10000m);
        listKomp[0].SubTotal.Should().Be(30000m);

        listKomp[1].Qty.Should().Be(qty);
        listKomp[1].Nilai.Should().Be(25000m);
        listKomp[1].SubTotal.Should().Be(75000m);

        result.Total.Should().Be(105000m);
    }

    [Fact]
    public void Create_WithPositionalPakaiBedIdAndQtyOverload_ShouldPreserveValues()
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateSampleNilaiTarif();
        const string pakaiBedId = "BED-RANAP-02";
        const int qty = 4;

        // Act
        var result = TindakanModel.Create(
            reg, layanan, nilaiTarif, [], TestUserId, pakaiBedId, qty, _occurredAt);

        // Assert
        result.PakaiBedId.Should().Be(pakaiBedId);
        result.Qty.Should().Be(qty);
        result.Total.Should().Be(35000m * 4);
    }

    [Fact]
    public void Create_WithPpaAllocation_ShouldCreateTindakanKomponenWithPpaTypeWithMultipliedSubtotal()
    {
        // Arrange
        var satTugas = new SatTugasType("ST01", "Dokter Spesialis", ProfesiType.Dokter);
        var komp = new KomponenType(
            "KOMP01", "Jasa Medis", GroupKomponenType.Default, CoaType.Default, CoaType.Default,
            [satTugas]);

        var ppa = new PpaType(
            "PPA01", "dr. Subroto, Sp.A", "DOKTER", SmfType.Default, GroupSpesialisType.Default,
            [], [new PpaSatTugasType(satTugas, true)], []);

        var listPpaView = new List<KomponenPpaView> { new(komp, ppa) };

        var listNilaiKomp = new List<NilaiTarifKomponenType>
        {
            new(1, komp.ToReff(), 50000m)
        };
        var nilaiTarif = new NilaiTarifType(
            "NT01", "TRF01", "Konsul Spesialis",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 50000m, listNilaiKomp);

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
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
}
