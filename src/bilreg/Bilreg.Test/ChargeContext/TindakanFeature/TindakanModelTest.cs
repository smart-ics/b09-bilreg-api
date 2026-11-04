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
    private const string UserId = "USER-01";
    private readonly DateTime _occurredAt = new(2026, 10, 16, 10, 0, 0);

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

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(-10)]
    public void Create_WhenQtyIsLessThanOrEqualToZero_ShouldThrowArgumentException(int invalidQty)
    {
        // Arrange
        var reg = RegModel.Default;
        var layanan = LayananType.Default;
        var nilaiTarif = CreateTestNilaiTarif(50000m, 30000m);

        // Act
        var act = () => TindakanModel.Create(reg, layanan, nilaiTarif, [], UserId, _occurredAt, qty: invalidQty);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Qty tindakan minimal 1*");
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
        var satTugasDoc = new SatTugasType("ST01", "Dokter Umum", ProfesiType.Dokter);
        var ppaDpjp = new PpaType("PPA-DPJP", "dr. DPJP", "dr. DPJP", SmfType.Default, GroupSpesialisType.Default,
            [], [new PpaSatTugasType(satTugasDoc, true)], []);
        var ppaPendamping = new PpaType("PPA-ANESTESI", "dr. Anestesi", "dr. Anestesi", SmfType.Default, GroupSpesialisType.Default,
            [], [new PpaSatTugasType(satTugasDoc, true)], []);

        var kompDpjp = new KomponenType("KOMP-DPJP", "Jasa Dokter DPJP", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugasDoc]);
        var kompPendamping = new KomponenType("KOMP-ANESTESI", "Jasa Dokter Anestesi", GroupKomponenType.Default, CoaType.Default, CoaType.Default, [satTugasDoc]);

        var listKomponenTarif = new List<NilaiTarifKomponenType>
        {
            new(1, new KomponenReff("KOMP-SARANA", "Sarana RS"), 50000m),
            new(2, kompDpjp.ToReff(), 60000m),
            new(3, kompPendamping.ToReff(), 40000m)
        };

        var nilaiTarif = new NilaiTarifType("NT-MULTI", "TRF-MULTI", "Operasi Kecil",
            TipeTarifType.Default.ToReff(), KelasType.Default.ToReff(), 150000m, listKomponenTarif);

        var listPpa = new List<KomponenPpaView>
        {
            new(kompDpjp, ppaDpjp),
            new(kompPendamping, ppaPendamping)
        };

        // RegModel with DPJP = ppaDpjp
        var reg = RegModel.Default;
        reg.AssignInpatientVisitTo(ppaDpjp, LayananType.Default with { InstalasiDk = InstalasiDkType.RawatInap });

        // Act
        var tindakan = TindakanModel.Create(reg, LayananType.Default, nilaiTarif, listPpa, UserId, _occurredAt, qty: 1, isDiskonDokter: true);

        // Assert
        tindakan.IsDiskonDokter.Should().BeTrue();
        tindakan.DiskonDokter.Should().Be(60000m); // Only DPJP is discounted
        tindakan.Total.Should().Be(90000m); // 50000 (sarana) + 40000 (anestesi)

        var kompDpjpActual = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().First(x => x.Komponen.KomponenId == "KOMP-DPJP");
        kompDpjpActual.Diskon.Should().Be(60000m);
        kompDpjpActual.SubTotal.Should().Be(0m);

        var kompAnestesiActual = tindakan.ListKomponen.OfType<TindakanKomponenWithPpaType>().First(x => x.Komponen.KomponenId == "KOMP-ANESTESI");
        kompAnestesiActual.Diskon.Should().Be(0m);
        kompAnestesiActual.SubTotal.Should().Be(40000m);
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
