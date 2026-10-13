using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Application.ApotekContext.ResepKerjaFeature.UseCases;
using Bilreg.Application.SalesContext.ResepFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.Shared;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.ApotekContext.ResepKerjaFeature;
using Bilreg.Test.ApotekContext.Support;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.ResepKerjaFeature;

public class PrescriptionContractAdapterTest
{
    private readonly Mock<IResepRepo> _resepRepoMock = new();

    private static BrgObatType Brg(string id = "BRG01", string name = "Amoxicillin 500mg") =>
        BrgObatType.Default with { BrgId = id, BrgName = name };

    private static SatuanType Satuan(string id = "TAB", string name = "Tablet") =>
        SatuanType.Create(id, name);

    private static ResepModel CreateSampleResep(
        string resepId = "KP123456",
        string regId = "RG001",
        string pasienId = "PAS001",
        string pasienName = "John Doe",
        string dokterId = "DOC001",
        string dokterName = "dr. Smith",
        string layananId = "LY001",
        string layananStokId = "FM001",
        string urgenitasId = "0",
        string urgenitasName = "Normal",
        int iter = 1)
    {
        var reg = new RegReff(regId, pasienId, pasienName);
        var bodyMetric = new BodyMetricType(65m, 170m, 1.75m);
        var dokter = new DokterReff(dokterId, dokterName);
        var layanan = new LayananReff(layananId, "Poli Umum");
        var layananStok = new LayananReff(layananStokId, "Farmasi Rawat Jalan");
        var urgenitas = UrgenitasType.Load(urgenitasId, urgenitasName);
        var tipeBrg = new TipeBrgReff("01", "Obat");

        return new ResepModel(
            resepId,
            reg,
            bodyMetric,
            dokter,
            layanan,
            layananStok,
            urgenitas,
            tipeBrg,
            iter,
            "Resep Uji",
            DateTime.Today,
            AppConst.DASH,
            AppConst.DASH,
            AuditTrailType.Create("USER01", DateTime.Now),
            new List<ResepObatType>());
    }

    [Fact]
    public void Load_GivenValidLegacyResep_MapsHeaderAndNonRacikItemsSuccessfully()
    {
        // Arrange
        var resep = CreateSampleResep("KP123456", iter: 2);
        var etiket = EtiketType.Create("3dd1", "3 kali sehari 1 tablet", 3, 1m, "ORAL", "Sesudah Makan", "07-13-19", "Habiskan");
        resep.AddObat(Brg("BRG01", "Paracetamol 500mg"), Satuan("TAB", "Tablet"), 10m, 0, etiket);

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP123456")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP123456");

        // Assert
        contract.Should().NotBeNull();
        contract.SourceKind.Should().Be(ResepKerjaSourceKindEnum.LegacyResep);
        contract.SourceResepId.Should().Be("KP123456");
        contract.RegId.Should().Be("RG001");
        contract.PasienId.Should().Be("PAS001");
        contract.PasienName.Should().Be("John Doe");
        contract.DokterId.Should().Be("DOC001");
        contract.DokterName.Should().Be("dr. Smith");
        contract.LayananId.Should().Be("LY001");
        contract.Urgenitas.Should().Be(0);
        contract.IterEntitled.Should().Be(2);

        contract.Items.Should().HaveCount(1);
        var item = contract.Items[0];
        item.SourceItemNo.Should().Be(1);
        item.BrgId.Should().Be("BRG01");
        item.BrgName.Should().Be("Paracetamol 500mg");
        item.SatuanId.Should().Be("TAB");
        item.SatuanName.Should().Be("Tablet");
        item.Qty.Should().Be(10m);
        item.Iter.Should().Be(0);
        item.Signa.Should().Be("3dd1");
        item.Instruction.Should().Be("3 kali sehari 1 tablet");
        item.Note.Should().Be("Habiskan");
        item.IsRacik.Should().BeFalse();

        contract.Components.Should().BeEmpty();
    }

    [Fact]
    public void Load_GivenCpoeSourceKind_PreservesCpoeSourceKind()
    {
        // Arrange
        var resep = CreateSampleResep("KP222222");
        resep.AddObat(Brg("BRG01", "Amoxicillin"), Satuan("KAP", "Kapsul"), 15m, 0, "3dd1", "3x1", "-");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP222222")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.Cpoe, "KP222222");

        // Assert
        contract.SourceKind.Should().Be(ResepKerjaSourceKindEnum.Cpoe);
        contract.SourceResepId.Should().Be("KP222222");
    }

    [Fact]
    public void Load_GivenResepWithRacikan_MapsContainersAndComponentsHierarchically()
    {
        // Arrange
        var resep = CreateSampleResep("KP333333");
        var etiketRacik = EtiketType.Create("3dd1 bungkus", "3x sehari 1 bungkus", 3, 1m, "ORAL", "Sesudah Makan");
        resep.AddRacik(Brg("RCK01", "Puyer Batuk Dewasa"), Satuan("BKS", "Bungkus"), 10m, 0, etiketRacik);
        resep.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Paracetamol 500mg"), Satuan("TAB", "Tablet"), 5m, 500m, "500 mg");
        resep.AddItemRacik(Brg("RCK01"), Brg("CMP02", "Dexamethasone 0.5mg"), Satuan("TAB", "Tablet"), 2m, 0.5m, "0.5 mg");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP333333")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP333333");

        // Assert
        contract.Items.Should().HaveCount(1);
        var container = contract.Items[0];
        container.SourceItemNo.Should().Be(1);
        container.BrgId.Should().Be("RCK01");
        container.BrgName.Should().Be("Puyer Batuk Dewasa");
        container.SatuanId.Should().Be("BKS");
        container.SatuanName.Should().Be("Bungkus");
        container.Qty.Should().Be(10m);
        container.IsRacik.Should().BeTrue();

        contract.Components.Should().HaveCount(2);
        var comp1 = contract.Components[0];
        comp1.SourceItemNo.Should().Be(1);
        comp1.ComponentNo.Should().Be(1);
        comp1.BrgId.Should().Be("CMP01");
        comp1.BrgName.Should().Be("Paracetamol 500mg");
        comp1.SatuanId.Should().Be("TAB");
        comp1.Qty.Should().Be(5m);

        var comp2 = contract.Components[1];
        comp2.SourceItemNo.Should().Be(1);
        comp2.ComponentNo.Should().Be(2);
        comp2.BrgId.Should().Be("CMP02");
        comp2.BrgName.Should().Be("Dexamethasone 0.5mg");
        comp2.SatuanId.Should().Be("TAB");
        comp2.Qty.Should().Be(2m);
    }

    [Fact]
    public void Load_GivenResepWithMixedSingleAndMultipleRacikan_MapsAllContainersAndComponentsDeterministically()
    {
        // Arrange
        var resep = CreateSampleResep("KP444444");
        // Item 1: Non-racik
        resep.AddObat(Brg("OBAT01", "Cetirizine 10mg"), Satuan("TAB", "Tablet"), 10m, 0, "1dd1", "1x1", "-");
        // Item 2: Racik A
        resep.AddRacik(Brg("RCK01", "Puyer A"), Satuan("BKS", "Bungkus"), 10m, 0, "3dd1", "3x1");
        resep.AddItemRacik(Brg("RCK01"), Brg("CMP01", "Bahan A1"), Satuan("TAB", "Tablet"), 3m, 100m, "100 mg");
        resep.AddItemRacik(Brg("RCK01"), Brg("CMP02", "Bahan A2"), Satuan("TAB", "Tablet"), 4m, 200m, "200 mg");
        // Item 3: Racik B
        resep.AddRacik(Brg("RCK02", "Kapsul B"), Satuan("KAP", "Kapsul"), 6m, 0, "2dd1", "2x1");
        resep.AddItemRacik(Brg("RCK02"), Brg("CMP03", "Bahan B1"), Satuan("TAB", "Tablet"), 6m, 50m, "50 mg");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP444444")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP444444");

        // Assert
        contract.Items.Should().HaveCount(3);
        contract.Items[0].SourceItemNo.Should().Be(1);
        contract.Items[0].IsRacik.Should().BeFalse();

        contract.Items[1].SourceItemNo.Should().Be(2);
        contract.Items[1].IsRacik.Should().BeTrue();

        contract.Items[2].SourceItemNo.Should().Be(3);
        contract.Items[2].IsRacik.Should().BeTrue();

        contract.Components.Should().HaveCount(3);
        contract.Components.Where(c => c.SourceItemNo == 2).Should().HaveCount(2);
        contract.Components.Where(c => c.SourceItemNo == 3).Should().HaveCount(1);
    }

    [Theory]
    [InlineData("1", 1)]
    [InlineData("2", 2)]
    [InlineData("0", 0)]
    public void Load_GivenNumericUrgenitasId_ParsesIntegerCorrectly(string urgenitasId, int expected)
    {
        // Arrange
        var resep = CreateSampleResep("KP555555", urgenitasId: urgenitasId);
        resep.AddObat(Brg("BRG01"), Satuan(), 5m, 0, "1dd1", "1x1", "-");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP555555")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP555555");

        // Assert
        contract.Urgenitas.Should().Be(expected);
    }

    [Theory]
    [InlineData("U", "CITO")]
    [InlineData("CITO", "Cito segera")]
    [InlineData("STAT", "Urgent Stat")]
    public void Load_GivenLegacyCitoOrUrgentMarker_ParsesUrgenitasAsOne(string urgenitasId, string urgenitasName)
    {
        // Arrange
        var resep = CreateSampleResep("KP666666", urgenitasId: urgenitasId, urgenitasName: urgenitasName);
        resep.AddObat(Brg("BRG01"), Satuan(), 5m, 0, "1dd1", "1x1", "-");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP666666")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP666666");

        // Assert
        contract.Urgenitas.Should().Be(1);
    }

    [Fact]
    public void Load_GivenNonNumericAndNonCitoUrgenitas_DefaultsUrgenitasToZero()
    {
        // Arrange
        var resep = CreateSampleResep("KP777777", urgenitasId: "-", urgenitasName: "-");
        resep.AddObat(Brg("BRG01"), Satuan(), 5m, 0, "1dd1", "1x1", "-");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP777777")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP777777");

        // Assert
        contract.Urgenitas.Should().Be(0);
    }

    [Fact]
    public void Load_GivenEmptyLayanan_FallsBackToLayananStokId()
    {
        // Arrange
        var resep = CreateSampleResep("KP888888", layananId: "-", layananStokId: "FM01");
        resep.AddObat(Brg("BRG01"), Satuan(), 5m, 0, "1dd1", "1x1", "-");

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP888888")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var contract = sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP888888");

        // Assert
        contract.LayananId.Should().Be("FM01");
    }

    [Fact]
    public void Load_GivenNonExistentResepId_ThrowsKeyNotFoundException()
    {
        // Arrange
        _resepRepoMock
            .Setup(x => x.LoadEntity(It.IsAny<IResepKey>()))
            .Returns(MayBe<ResepModel>.None);

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var act = () => sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP_NOT_EXIST");

        // Assert
        act.Should().Throw<KeyNotFoundException>().WithMessage("*KP_NOT_EXIST*");
    }

    [Fact]
    public void Load_GivenVoidedResep_ThrowsApotekDomainException()
    {
        // Arrange
        var resep = CreateSampleResep("KP_VOIDED");
        resep.AddObat(Brg("BRG01"), Satuan(), 5m, 0, "1dd1", "1x1", "-");
        resep.Void("USER_VOID", "Salah resep", DateTime.Now);

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP_VOIDED")))
            .Returns(MayBe.From(resep));

        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var act = () => sut.Load(ResepKerjaSourceKindEnum.LegacyResep, "KP_VOIDED");

        // Assert
        act.Should().Throw<ApotekDomainException>().WithMessage("*voided*");
    }

    [Fact]
    public void Load_GivenPhysicalSourceKind_ThrowsInvalidOperationException()
    {
        // Arrange
        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var act = () => sut.Load(ResepKerjaSourceKindEnum.Physical, "KP123456");

        // Assert
        act.Should().Throw<InvalidOperationException>().WithMessage("*Physical*");
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Load_GivenNullOrWhiteSpaceSourceResepId_ThrowsArgumentException(string? sourceResepId)
    {
        // Arrange
        var sut = new PrescriptionContractAdapter(_resepRepoMock.Object);

        // Act
        var act = () => sut.Load(ResepKerjaSourceKindEnum.LegacyResep, sourceResepId!);

        // Assert
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public async Task DownstreamIntakeElectronicHandler_WithAdapter_ProcessesPrescriptionSuccessfullyWithoutFailClosedException()
    {
        // Arrange
        var resep = CreateSampleResep("KP999999", iter: 1);
        resep.AddObat(Brg("BRG01", "Paracetamol 500mg"), Satuan("TAB", "Tablet"), 10m, 0,
            EtiketType.Create("3dd1", "3x1 tablet", 3, 1m, "ORAL", "Sesudah Makan"));

        _resepRepoMock
            .Setup(x => x.LoadEntity(It.Is<IResepKey>(k => k.ResepId == "KP999999")))
            .Returns(MayBe.From(resep));

        var adapter = new PrescriptionContractAdapter(_resepRepoMock.Object);
        var resepKerjaRepo = new InMemoryResepKerjaRepo();
        var authPolicy = new AllowAllAuth();
        var intakeHandler = new ResepKerjaIntakeElectronicHandler(resepKerjaRepo, adapter, authPolicy);

        var cmd = new ResepKerjaIntakeElectronicCmd("user01", ResepKerjaSourceKindEnum.LegacyResep, "KP999999");

        // Act
        var response = await intakeHandler.Handle(cmd, default);

        // Assert
        response.Should().NotBeNull();
        response.ResepKerjaId.Should().NotBeNullOrWhiteSpace();
        response.IdempotentReplay.Should().BeFalse();

        var savedResepKerjaOpt = resepKerjaRepo.LoadEntity(ResepKerjaModel.Key(response.ResepKerjaId));
        savedResepKerjaOpt.HasValue.Should().BeTrue();
        var savedResepKerja = savedResepKerjaOpt.Value;
        savedResepKerja.SourceResepId.Should().Be("KP999999");
        savedResepKerja.Items.Should().HaveCount(1);
        savedResepKerja.Items[0].BrgId.Should().Be("BRG01");
        savedResepKerja.Items[0].BrgName.Should().Be("Paracetamol 500mg");
    }
}
