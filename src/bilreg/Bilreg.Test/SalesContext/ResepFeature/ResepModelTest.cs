using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.SalesContext.ResepFeature;

public class ResepModelTest
{
    private static BrgObatType Brg(string id = "BRG01", string name = "Amoxicillin 500mg") =>
        BrgObatType.Default with { BrgId = id, BrgName = name };

    private static SatuanType Satuan(string id = "TAB", string name = "Tablet") =>
        SatuanType.Create(id, name);

    private static ResepModel CreateSut(int iter = 0)
    {
        var reg = RegModel.Default;
        var bodyMetric = new BodyMetricType(65m, 170m, 1.75m);
        var dokter = DokterType.Default;
        var layanan = LayananType.Default;
        var urgenitas = UrgenitasType.Default;
        var tipeBrg = TipeBrgType.Default;

        return ResepModel.Create(
            reg,
            bodyMetric,
            dokter,
            layanan,
            urgenitas,
            tipeBrg,
            iter,
            "Resep Rawat Jalan",
            "USER01");
    }

    [Fact]
    public void Key_ShouldInitializeDefaultValues()
    {
        // Act
        var sut = ResepModel.Key("KP12345");

        // Assert
        sut.ResepId.Should().Be("KP12345");
        sut.Register.RegId.Should().Be(RegModel.Default.RegId);
        sut.BodyMetric.Should().Be(BodyMetricType.Default());
        sut.Dokter.DokterId.Should().Be(DokterType.Default.DokterId);
        sut.Layanan.LayananId.Should().Be(LayananType.Default.LayananId);
        sut.LayananStok.LayananId.Should().Be(LayananType.Default.LayananId);
        sut.Urgenitas.Should().Be(UrgenitasType.Default);
        sut.TipeBrg.TipeBrgId.Should().Be(TipeBrgType.Default.TipeBrgId);
        sut.Iter.Should().Be(0);
        sut.Description.Should().Be(AppConst.DASH);
        sut.TglRencanaPeriksa.Should().Be(DateTime.MinValue);
        sut.ResepAsalReffId.Should().Be(AppConst.DASH);
        sut.VoidReason.Should().Be(AppConst.DASH);
        sut.IsVoid.Should().BeFalse();
        sut.ListObat.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithDefaultDepoAndPlannedDate_ShouldSetDefaultsCorrectly()
    {
        // Arrange
        var reg = RegModel.Default;
        var bodyMetric = new BodyMetricType(55m, 160m, 1.55m);
        var dokter = DokterType.Default;
        var layanan = LayananType.Default;

        // Act
        var sut = ResepModel.Create(
            reg,
            bodyMetric,
            dokter,
            layanan,
            UrgenitasType.Default,
            TipeBrgType.Default,
            1,
            "Resep iter 1x",
            "DOKTER01");

        // Assert
        sut.ResepId.Should().StartWith("KP");
        sut.BodyMetric.BodyWeight.Should().Be(55m);
        sut.BodyMetric.BodyHeight.Should().Be(160m);
        sut.Layanan.LayananId.Should().Be(layanan.LayananId);
        sut.LayananStok.LayananId.Should().Be(layanan.LayananId);
        sut.Iter.Should().Be(1);
        sut.Description.Should().Be("Resep iter 1x");
        sut.TglRencanaPeriksa.Date.Should().Be(DateTime.Today);
        sut.ResepAsalReffId.Should().Be(AppConst.DASH);
        sut.VoidReason.Should().Be(AppConst.DASH);
        sut.AuditTrail.Created.UserId.Should().Be("DOKTER01");
        sut.IsVoid.Should().BeFalse();
        sut.ListObat.Should().BeEmpty();
    }

    [Fact]
    public void Create_WithExplicitDepoAndPlannedDate_ShouldSetProperties()
    {
        // Arrange
        var reg = RegModel.Default;
        var bodyMetric = BodyMetricType.Default();
        var dokter = DokterType.Default;
        var layanan = LayananType.Default;
        var depoFarmasi = new LayananReff("DEPO01", "Farmasi Rawat Jalan");
        var plannedDate = DateTime.Today.AddDays(3);

        // Act
        var sut = ResepModel.Create(
            reg,
            bodyMetric,
            dokter,
            layanan,
            depoFarmasi,
            UrgenitasType.Default,
            TipeBrgType.Default,
            2,
            "Terapi pasca op",
            "DOKTER02",
            plannedDate,
            "KP-INDUK-001");

        // Assert
        sut.LayananStok.LayananId.Should().Be("DEPO01");
        sut.LayananStok.LayananName.Should().Be("Farmasi Rawat Jalan");
        sut.TglRencanaPeriksa.Date.Should().Be(plannedDate.Date);
        sut.ResepAsalReffId.Should().Be("KP-INDUK-001");
        sut.Iter.Should().Be(2);
    }

    [Fact]
    public void Create_WithNegativeIter_ShouldThrowArgumentException()
    {
        // Act
        var act = () => ResepModel.Create(
            RegModel.Default,
            BodyMetricType.Default(),
            DokterType.Default,
            LayananType.Default,
            UrgenitasType.Default,
            TipeBrgType.Default,
            -1,
            "Iter minus",
            "USER01");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Iter tidak boleh bernilai negatif*");
    }

    [Fact]
    public void Constructor_WithNegativeIter_ShouldThrowArgumentException()
    {
        // Act
        var act = () => new ResepModel(
            "KP001",
            RegModel.Default.ToReff(),
            BodyMetricType.Default(),
            DokterType.Default.ToReff(),
            LayananType.Default.ToReff(),
            UrgenitasType.Default,
            TipeBrgType.Default.ToReff(),
            -2,
            "Invalid Iter",
            AuditTrailType.Default,
            []);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Iter tidak boleh bernilai negatif*");
    }

    [Fact]
    public void AddObat_WithEtiketType_ShouldAddItemWithStructuredEtiket()
    {
        // Arrange
        var sut = CreateSut();
        var etiket = EtiketType.Create(
            "3dd1",
            "3 kali sehari 1 tablet",
            3,
            1m,
            "ORAL",
            "Sesudah Makan",
            "07-13-19",
            "Minum secara teratur");

        // Act
        sut.AddObat(Brg("BRG01", "Paracetamol 500mg"), Satuan("TAB"), 10m, 0, etiket);

        // Assert
        sut.ListObat.Should().HaveCount(1);
        var obat = sut.ListObat.First();
        obat.NoUrut.Should().Be(1);
        obat.Brg.BrgId.Should().Be("BRG01");
        obat.Qty.Should().Be(10m);
        obat.Iter.Should().Be(0);
        obat.IsRacik.Should().BeFalse();
        obat.Etiket.Frequency.Should().Be(3);
        obat.Etiket.UnitDose.Should().Be(1m);
        obat.Etiket.Route.Should().Be("ORAL");
        obat.Etiket.MealRelation.Should().Be("Sesudah Makan");
        obat.Etiket.UddTime.Should().Be("07-13-19");
        obat.Etiket.Note.Should().Be("Minum secara teratur");
    }

    [Fact]
    public void AddObat_WithStructuredParameters_ShouldCreateStructuredEtiket()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.AddObat(
            Brg("BRG02", "Cefixime 200mg"),
            Satuan("KAP"),
            6m,
            0,
            "2dd1",
            "2 kali sehari 1 kapsul",
            2,
            1m,
            "ORAL",
            "Sesudah Makan",
            "07-19",
            "Habiskan");

        // Assert
        var obat = sut.ListObat.Single();
        obat.Etiket.Frequency.Should().Be(2);
        obat.Etiket.UnitDose.Should().Be(1m);
        obat.Etiket.Route.Should().Be("ORAL");
        obat.Etiket.MealRelation.Should().Be("Sesudah Makan");
        obat.Etiket.UddTime.Should().Be("07-19");
        obat.Etiket.Note.Should().Be("Habiskan");
    }

    [Fact]
    public void AddObat_WithLegacyParameters_ShouldFallbackParseEtiket()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        sut.AddObat(Brg("BRG03", "Amoxicillin"), Satuan("TAB"), 15m, 0, "3dd1 tab", "3x1 tablet", "Catatan");

        // Assert
        var obat = sut.ListObat.Single();
        obat.Etiket.Frequency.Should().Be(3);
        obat.Etiket.UnitDose.Should().Be(1m);
        obat.Etiket.Note.Should().Be("Catatan");
    }

    [Fact]
    public void AddObat_WithNegativeIter_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.AddObat(Brg(), Satuan(), 10m, -1, "1dd1", "1x1", AppConst.DASH);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Iter tidak boleh bernilai negatif*");
    }

    [Fact]
    public void AddObat_DuplicateBrg_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddObat(Brg("BRG01", "Obat A"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);

        // Act
        var act = () => sut.AddObat(Brg("BRG01", "Obat A"), Satuan(), 5m, 0, "1dd1", "1x1", AppConst.DASH);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Brg sudah ada, tidak bisa duplikasi*");
    }

    [Fact]
    public void RemoveObat_ShouldRemoveAndReorderNoUrut()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddObat(Brg("BRG01", "Obat 1"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);
        sut.AddObat(Brg("BRG02", "Obat 2"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);
        sut.AddObat(Brg("BRG03", "Obat 3"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);

        // Act
        sut.RemoveObat(Brg("BRG02", "Obat 2"));

        // Assert
        sut.ListObat.Should().HaveCount(2);
        var list = sut.ListObat.ToList();
        list[0].Brg.BrgId.Should().Be("BRG01");
        list[0].NoUrut.Should().Be(1);
        list[1].Brg.BrgId.Should().Be("BRG03");
        list[1].NoUrut.Should().Be(2);
    }

    [Fact]
    public void AddRacik_ShouldCreateRacikContainerWithIsRacikTrue()
    {
        // Arrange
        var sut = CreateSut();
        var etiket = EtiketType.Create("3dd1 bungkus", "3 kali sehari 1 bungkus", 3, 1m, "ORAL", "Sesudah Makan");

        // Act
        sut.AddRacik(Brg("RCK01", "Puyer Batuk Dewasa"), Satuan("BKS", "Bungkus"), 10m, 0, etiket);

        // Assert
        sut.ListObat.Should().HaveCount(1);
        var racik = sut.ListObat.First();
        racik.IsRacik.Should().BeTrue();
        racik.ListItemRacik.Should().BeEmpty();
    }

    [Fact]
    public void AddItemRacik_ShouldAddComponentsWithSequentialNoUrut()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddRacik(Brg("RCK01", "Kapsul Racikan"), Satuan("KAP"), 10m, 0, "3dd1", "3x1");

        // Act
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Paracetamol"), Satuan("TAB"), 5m, 500m, "500 mg");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP02", "Dexamethasone"), Satuan("TAB"), 2m, 0.5m, "0.5 mg");

        // Assert
        var racik = sut.ListObat.Single(x => x.Brg.BrgId == "RCK01");
        racik.ListItemRacik.Should().HaveCount(2);

        var components = racik.ListItemRacik.ToList();
        components[0].NoUrut.Should().Be(1);
        components[0].Brg.BrgId.Should().Be("CMP01");
        components[0].Dosis.Should().Be(500m);
        components[0].DosisTxt.Should().Be("500 mg");

        components[1].NoUrut.Should().Be(2);
        components[1].Brg.BrgId.Should().Be("CMP02");
        components[1].Dosis.Should().Be(0.5m);
    }

    [Fact]
    public void AddItemRacik_WhenRacikNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.AddItemRacik(Brg("UNKNOWN"), Brg("CMP01"), Satuan(), 1m, 1m, "1");

        // Assert
        act.Should().Throw<KeyNotFoundException>()
            .WithMessage("*Obat Racik tidak ditemukan*");
    }

    [Fact]
    public void AddItemRacik_DuplicateComponent_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddRacik(Brg("RCK01", "Racikan"), Satuan("BKS"), 10m, 0, "3dd1", "3x1");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Bahan A"), Satuan(), 2m, 100m, "100 mg");

        // Act
        var act = () => sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Bahan A"), Satuan(), 3m, 100m, "100 mg");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Item racik sudah ada, tidak bisa duplikasi*");
    }

    [Fact]
    public void RemoveItemRacik_ShouldRemoveComponentFromRacik()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddRacik(Brg("RCK01", "Racikan"), Satuan("BKS"), 10m, 0, "3dd1", "3x1");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Bahan A"), Satuan(), 2m, 100m, "100 mg");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP02", "Bahan B"), Satuan(), 3m, 100m, "100 mg");

        // Act
        sut.RemoveItemRacik(Brg("RCK01"), Brg("CMP01", "Bahan A"));

        // Assert
        var racik = sut.ListObat.Single(x => x.Brg.BrgId == "RCK01");
        racik.ListItemRacik.Should().HaveCount(1);
        racik.ListItemRacik.Single().Brg.BrgId.Should().Be("CMP02");
    }

    [Fact]
    public void ValidateRacik_WhenRacikHasNoComponents_ShouldThrowInvalidOperationException()
    {
        // Arrange (EX-02: Racikan tanpa bahan komponen)
        var sut = CreateSut();
        sut.AddRacik(Brg("RCK01", "Puyer Batuk"), Satuan("BKS"), 10m, 0, "3dd1", "3x1");

        // Act
        var act = () => sut.ValidateRacik();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Wadah racikan 'Puyer Batuk' harus memiliki minimal 1 bahan komponen*");
    }

    [Fact]
    public void ValidateRacik_WhenAllRacikHaveComponents_ShouldPass()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddRacik(Brg("RCK01", "Puyer Batuk"), Satuan("BKS"), 10m, 0, "3dd1", "3x1");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Ambroxol"), Satuan(), 5m, 30m, "30 mg");

        // Act
        var act = () => sut.ValidateRacik();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_WhenResepHasNoObat_ShouldThrowInvalidOperationException()
    {
        // Arrange (EX-01: Resep tanpa item obat)
        var sut = CreateSut();

        // Act
        var act = () => sut.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Resep harus memiliki minimal 1 item obat*");
    }

    [Fact]
    public void Validate_WhenResepHasEmptyRacik_ShouldThrowInvalidOperationException()
    {
        // Arrange (EX-02)
        var sut = CreateSut();
        sut.AddObat(Brg("BRG01", "Obat Jadi"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);
        sut.AddRacik(Brg("RCK01", "Salep Racik"), Satuan("POT"), 1m, 0, "2dd1", "2x1");

        // Act
        var act = () => sut.Validate();

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Wadah racikan 'Salep Racik' harus memiliki minimal 1 bahan komponen*");
    }

    [Fact]
    public void Validate_WhenResepValid_ShouldPass()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddObat(Brg("BRG01", "Obat Jadi"), Satuan(), 10m, 0, "1dd1", "1x1", AppConst.DASH);
        sut.AddRacik(Brg("RCK01", "Salep Racik"), Satuan("POT"), 1m, 0, "2dd1", "2x1");
        sut.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Hydrocortisone"), Satuan(), 1m, 1m, "1 tube");

        // Act
        var act = () => sut.Validate();

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Void_WhenActive_ShouldSetAuditTrailAndVoidReason()
    {
        // Arrange
        var sut = CreateSut();
        var voidTime = new DateTime(2026, 10, 12, 14, 30, 0);

        // Act
        sut.Void("DR_SPESIALIS", "Pasien mengalami kontraindikasi", voidTime);

        // Assert
        sut.IsVoid.Should().BeTrue();
        sut.VoidReason.Should().Be("Pasien mengalami kontraindikasi");
        sut.AuditTrail.IsVoided.Should().BeTrue();
        sut.AuditTrail.Voided.UserId.Should().Be("DR_SPESIALIS");
        sut.AuditTrail.Voided.Timestamp.Should().Be(voidTime);
    }

    [Fact]
    public void Void_WhenAlreadyVoided_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.Void("DR_SPESIALIS", "Alasan 1", DateTime.Now);

        // Act
        var act = () => sut.Void("DR_LAIN", "Alasan 2", DateTime.Now);

        // Assert
        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Resep sudah pernah dibatalkan*");
    }

    [Fact]
    public void Void_WithEmptyReason_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Void("DR_SPESIALIS", "   ", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Alasan pembatalan harus diisi*");
    }

    [Fact]
    public void Void_WithEmptyUserId_ShouldThrowArgumentException()
    {
        // Arrange
        var sut = CreateSut();

        // Act
        var act = () => sut.Void("", "Salah resep", DateTime.Now);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*User ID pembatal harus diisi*");
    }

    [Fact]
    public void Operations_WhenVoided_ShouldThrowInvalidOperationException()
    {
        // Arrange
        var sut = CreateSut();
        sut.AddObat(Brg("BRG01", "Obat"), Satuan(), 5m, 0, "1dd1", "1x1", AppConst.DASH);
        sut.Void("USER01", "Dibatalkan", DateTime.Now);

        // Act & Assert
        sut.Invoking(x => x.Modify("USER02"))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.AddObat(Brg("BRG02", "Obat Baru"), Satuan(), 5m, 0, "1dd1", "1x1", AppConst.DASH))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.RemoveObat(Brg("BRG01")))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.AddItemRacik(Brg("BRG01"), Brg("CMP01"), Satuan(), 1m, 1m, "1"))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.RemoveItemRacik(Brg("BRG01"), Brg("CMP01")))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.SetLayananStok(new LayananReff("LYN01", "Depo")))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.SetTglRencanaPeriksa(DateTime.Today))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.SetResepAsalReffId("KP-001"))
            .Should().Throw<InvalidOperationException>();

        sut.Invoking(x => x.SetDescription("Deskripsi baru"))
            .Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Setters_WhenActive_ShouldUpdatePropertiesSuccessfully()
    {
        // Arrange
        var sut = CreateSut();
        var newDepo = new LayananReff("DP02", "Depo Farmasi IGD");
        var newTgl = DateTime.Today.AddDays(1);

        // Act
        sut.SetLayananStok(newDepo);
        sut.SetTglRencanaPeriksa(newTgl);
        sut.SetResepAsalReffId("KP-REF-999");
        sut.SetDescription("Deskripsi baru");

        // Assert
        sut.LayananStok.Should().Be(newDepo);
        sut.TglRencanaPeriksa.Should().Be(newTgl);
        sut.ResepAsalReffId.Should().Be("KP-REF-999");
        sut.Description.Should().Be("Deskripsi baru");
    }
}
