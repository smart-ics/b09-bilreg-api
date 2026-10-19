using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RekapCetakFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature.UseCases;

public class OkCreateOrderOpByRegHandlerTest
{
    private readonly Mock<IOrderOpRepo> _orderOpRepoMock = new();
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<ILayananRepo> _layananRepoMock = new();
    private readonly Mock<IPpaRepo> _dokterRepoMock = new();
    private readonly Mock<IIcd10Repo> _icdRepoMock = new();
    private readonly Mock<IJenisOperasiRepo> _jenisOperasiRepoMock = new();
    private readonly Mock<IOpCaseRepo> _opCaseRepoMock = new();
    private readonly Mock<ITarifRepo> _tarifRepoMock = new();
    private readonly Mock<ITglJamProvider> _tglJamProviderMock = new();

    private readonly DateTime _now = new(2026, 10, 16, 10, 0, 0);

    public OkCreateOrderOpByRegHandlerTest()
    {
        _tglJamProviderMock.Setup(x => x.Now).Returns(_now);
    }

    private OkCreateOrderOpByRegHandler CreateSut()
    {
        return new OkCreateOrderOpByRegHandler(
            _orderOpRepoMock.Object,
            _regRepoMock.Object,
            _layananRepoMock.Object,
            _dokterRepoMock.Object,
            _icdRepoMock.Object,
            _jenisOperasiRepoMock.Object,
            _opCaseRepoMock.Object,
            _tarifRepoMock.Object,
            _tglJamProviderMock.Object);
    }

    private static LayananType CreateSampleLayanan(string id = "LYN-001", string name = "Klinik Bedah") =>
        new(
            id,
            name,
            true,
            InstalasiType.Default.ToReff(),
            LayananDkType.Default.ToReff(),
            TipeLayananDkType.Default,
            InstalasiDkType.Default,
            UnitType.Default.ToReff(),
            new PoliBpjsReff("-", "-"));

    private static PpaType CreateSamplePpa(string id = "PPA-001", string name = "dr. Pengorder, Sp.B") =>
        new(
            id,
            name,
            "Sp.B",
            SmfType.Default,
            GroupSpesialisType.Default,
            [],
            [],
            []);

    private static Icd10Type CreateSampleIcd(string code = "K35.8") =>
        new(code, "Acute appendicitis, other");

    private static JenisOperasiType CreateSampleJenisOp(string id = "JOP-001") =>
        new(id, "Bedah Umum");

    private static TarifType CreateSampleTarif(string id = "TRF-001", string name = "Apendektomi") =>
        new(
            id,
            name,
            GroupTarifType.Default,
            GroupTarifDkType.Default,
            JenisTarifType.Default,
            RekapCetakType.Default.ToReff());

    private static RegModel CreateSampleReg(string id = "REG-001", bool isAktif = true)
    {
        var auditKeluar = isAktif ? AuditInfoType.Default : new AuditInfoType("user-out", new DateTime(2026, 10, 16));
        return new RegModel(
            id,
            new DateOnly(2026, 10, 16),
            new AuditInfoType("user-reg", new DateTime(2026, 10, 16)),
            auditKeluar,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);
    }

    private static OkCreateOrderOpByRegCmd CreateSampleCmd(
        string regId = "REG-001",
        string dokterPengorderId = "DOC-P01",
        string layananPengorderId = "LYN-001",
        string dokterOperatorId = "DOC-OPR",
        string? tarifId = "TRF-001",
        bool isForceCreate = false,
        IEnumerable<OrderOpResourceRequest>? listResource = null)
    {
        return new OkCreateOrderOpByRegCmd(
            RegId: regId,
            DokterPengorderId: dokterPengorderId,
            LayananPengorderId: layananPengorderId,
            DokterOperatorId: dokterOperatorId,
            DiagCode: "K35.8",
            JenisOperasiId: "JOP-001",
            NamaOperasi: "Apendektomi",
            ProsedurTambahan: "Adhesiolisis",
            KlasifikasiOperasi: KlasifikasiOperasiEnum.Sedang,
            AnestesiPlan: AnestesiPlanEnum.RegionalSpinal,
            PostOpCareUnit: PostOpCareUnitEnum.PACU,
            TarifId: tarifId,
            EstimasiDurasiInMinutes: 60,
            PreferedDate: new DateTime(2026, 10, 20),
            SpecialEquipment: "Harmonic Scalpel",
            UserId: "user1",
            IsForceCreate: isForceCreate,
            ListResource: listResource);
    }

    [Fact]
    public async Task Handle_WithFullValidRequest_ShouldSaveOrderOpAndOpCase_AndReturnSuccessResponse()
    {
        // Arrange
        var cmd = CreateSampleCmd();
        var reg = CreateSampleReg(cmd.RegId);
        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId, "dr. Pengorder");
        var dokterOperator = CreateSamplePpa(cmd.DokterOperatorId, "dr. Operator");
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);
        var tarif = CreateSampleTarif(cmd.TarifId!);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == cmd.RegId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == cmd.LayananPengorderId)))
            .Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterPengorderId)))
            .Returns(MayBe.From(dokterPengorder));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterOperatorId)))
            .Returns(MayBe.From(dokterOperator));
        _icdRepoMock.Setup(x => x.LoadEntity(It.Is<IIcd10Key>(k => k.Icd10Id == cmd.DiagCode)))
            .Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.Is<IJenisOperasiKey>(k => k.JenisOperasiId == cmd.JenisOperasiId)))
            .Returns(MayBe.From(jenisOp));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == cmd.TarifId)))
            .Returns(MayBe.From(tarif));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(new List<OrderOpView>());

        OrderOpModel? savedOrderOp = null;
        OpCaseModel? savedOpCase = null;
        _orderOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<OrderOpModel>()))
            .Callback<OrderOpModel>(m => savedOrderOp = m);
        _opCaseRepoMock.Setup(x => x.SaveChanges(It.IsAny<OpCaseModel>()))
            .Callback<OpCaseModel>(m => savedOpCase = m);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDuplicated.Should().BeFalse();
        response.IsForceCreate.Should().BeTrue();
        response.OrderOpId.Should().NotBe("-");
        response.Requested.RegId.Should().Be(cmd.RegId);
        response.Requested.NamaOperasi.Should().Be(cmd.NamaOperasi);

        savedOrderOp.Should().NotBeNull();
        savedOrderOp!.DokterPengorder.PpaId.Should().Be(cmd.DokterPengorderId);
        savedOrderOp.LayananPengorder.LayananId.Should().Be(cmd.LayananPengorderId);
        savedOrderOp.DokterOperator.PpaId.Should().Be(cmd.DokterOperatorId);
        savedOrderOp.ProsedurTambahan.Should().Be(cmd.ProsedurTambahan);
        savedOrderOp.KlasifikasiOperasi.Should().Be(cmd.KlasifikasiOperasi);
        savedOrderOp.AnestesiPlan.Should().Be(cmd.AnestesiPlan);
        savedOrderOp.PostOpCareUnit.Should().Be(cmd.PostOpCareUnit);
        savedOrderOp.TarifOperasi.TarifId.Should().Be(cmd.TarifId);

        savedOpCase.Should().NotBeNull();
        savedOpCase!.OrderOpId.Should().Be(savedOrderOp.OrderOpId);
        savedOpCase.OrderOpState.Should().Be(OpCaseStateEnum.Requested);

        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Once);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithoutOptionalTarifAndDokterOperator_ShouldSaveSuccessfullyWithDefaults()
    {
        // Arrange
        var cmd = CreateSampleCmd(dokterOperatorId: "", tarifId: null);
        var reg = CreateSampleReg(cmd.RegId);
        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId, "dr. Pengorder");
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == cmd.RegId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == cmd.LayananPengorderId)))
            .Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterPengorderId)))
            .Returns(MayBe.From(dokterPengorder));
        _icdRepoMock.Setup(x => x.LoadEntity(It.Is<IIcd10Key>(k => k.Icd10Id == cmd.DiagCode)))
            .Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.Is<IJenisOperasiKey>(k => k.JenisOperasiId == cmd.JenisOperasiId)))
            .Returns(MayBe.From(jenisOp));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns(new List<OrderOpView>());

        OrderOpModel? savedOrderOp = null;
        _orderOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<OrderOpModel>()))
            .Callback<OrderOpModel>(m => savedOrderOp = m);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDuplicated.Should().BeFalse();
        savedOrderOp.Should().NotBeNull();
        savedOrderOp!.TarifOperasi.Should().Be(TarifReff.Default);
        savedOrderOp.DokterOperator.Should().Be(PpaReff.Default);
        _tarifRepoMock.Verify(x => x.LoadEntity(It.IsAny<ITarifKey>()), Times.Never);
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithResources_ShouldAddAllResourcesToOrderOp()
    {
        // Arrange
        var resources = new List<OrderOpResourceRequest>
        {
            new(OrderOpResourceTypeEnum.Darah, "PRC", 2, "Kolf", "Siapkan pra bedah"),
            new(OrderOpResourceTypeEnum.Implan, "Mesh Ventral Hernia", 1, "Pcs", "Ukuran 15x15"),
            new(OrderOpResourceTypeEnum.AlatKhusus, "Harmonic Scalpel", 1, "Set", "Minta dari CSSD")
        };
        var cmd = CreateSampleCmd(listResource: resources);
        var reg = CreateSampleReg(cmd.RegId);
        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId);
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);

        var tarif = CreateSampleTarif();
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokterPengorder));
        _icdRepoMock.Setup(x => x.LoadEntity(It.IsAny<IIcd10Key>())).Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJenisOperasiKey>())).Returns(MayBe.From(jenisOp));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>())).Returns(MayBe.From(tarif));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns(new List<OrderOpView>());

        OrderOpModel? savedOrderOp = null;
        _orderOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<OrderOpModel>()))
            .Callback<OrderOpModel>(m => savedOrderOp = m);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        savedOrderOp.Should().NotBeNull();
        savedOrderOp!.ListResource.Should().HaveCount(3);
        savedOrderOp.ListResource.Select(r => r.ItemName).Should().Contain(["PRC", "Mesh Ventral Hernia", "Harmonic Scalpel"]);
    }

    [Fact]
    public async Task Handle_WhenExistingOrderFound_AndNotForceCreate_ShouldReturnDuplicatedWithoutSaving()
    {
        // Arrange
        var cmd = CreateSampleCmd(isForceCreate: false);
        var existingOrder = new OrderOpView(
            "ORD-EXISTING",
            cmd.RegId,
            "PAS-001",
            "Pasien Test",
            "Operasi Lama",
            JenisOperasiType.Default,
            new DateTime(2026, 10, 18));

        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>()))
            .Returns([existingOrder]);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDuplicated.Should().BeTrue();
        response.IsForceCreate.Should().BeFalse();
        response.OrderOpId.Should().Be("-");
        response.Existing.RegId.Should().Be(cmd.RegId);
        response.Existing.NamaOperasi.Should().Be("Operasi Lama");

        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Never);
        _opCaseRepoMock.Verify(x => x.SaveChanges(It.IsAny<OpCaseModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenExistingOrderFound_AndForceCreate_ShouldCreateAndSave()
    {
        // Arrange
        var cmd = CreateSampleCmd(isForceCreate: true);
        var reg = CreateSampleReg(cmd.RegId);
        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId);
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);

        var tarif = CreateSampleTarif();
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>())).Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.IsAny<ILayananKey>())).Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.IsAny<IPpaKey>())).Returns(MayBe.From(dokterPengorder));
        _icdRepoMock.Setup(x => x.LoadEntity(It.IsAny<IIcd10Key>())).Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.IsAny<IJenisOperasiKey>())).Returns(MayBe.From(jenisOp));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.IsAny<ITarifKey>())).Returns(MayBe.From(tarif));

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.IsDuplicated.Should().BeFalse();
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenRegNotFound_ShouldThrowKeyNotFoundException()
    {
        // Arrange
        var cmd = CreateSampleCmd();
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns([]);

        var sut = CreateSut();

        // Act & Assert
        await FluentActions.Awaiting(() => sut.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Reg ID invalid*");
    }

    [Fact]
    public async Task Handle_WhenRegInactive_ShouldThrowArgumentException()
    {
        // Arrange
        var cmd = CreateSampleCmd();
        var inactiveReg = CreateSampleReg(cmd.RegId, isAktif: false);
        _regRepoMock.Setup(x => x.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(inactiveReg));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns([]);

        var sut = CreateSut();

        // Act & Assert
        await FluentActions.Awaiting(() => sut.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>()
            .WithMessage("*tidak aktif*");
    }

    [Theory]
    [InlineData("", "DOC", "LYN", "K35", "JOP", "Nama")]
    [InlineData("REG", "", "LYN", "K35", "JOP", "Nama")]
    [InlineData("REG", "DOC", "", "K35", "JOP", "Nama")]
    [InlineData("REG", "DOC", "LYN", "", "JOP", "Nama")]
    [InlineData("REG", "DOC", "LYN", "K35", "", "Nama")]
    [InlineData("REG", "DOC", "LYN", "K35", "JOP", "")]
    [InlineData("REG", "DOC", "LYN", "K35", "JOP", "   ")]
    public async Task Handle_WhenMandatoryFieldEmpty_ShouldThrowArgumentException(
        string regId, string docId, string lynId, string diag, string jenis, string nama)
    {
        // Arrange
        var cmd = new OkCreateOrderOpByRegCmd(
            RegId: regId,
            DokterPengorderId: docId,
            LayananPengorderId: lynId,
            DokterOperatorId: "",
            DiagCode: diag,
            JenisOperasiId: jenis,
            NamaOperasi: nama,
            ProsedurTambahan: "",
            KlasifikasiOperasi: KlasifikasiOperasiEnum.Kecil,
            AnestesiPlan: AnestesiPlanEnum.General,
            PostOpCareUnit: PostOpCareUnitEnum.Ward,
            TarifId: null,
            EstimasiDurasiInMinutes: 30,
            PreferedDate: new DateTime(2026, 10, 20),
            SpecialEquipment: "-",
            UserId: "u1",
            IsForceCreate: false);

        var sut = CreateSut();

        // Act & Assert
        await FluentActions.Awaiting(() => sut.Handle(cmd, CancellationToken.None))
            .Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenRegHasWhitespaceAuditFields_ShouldBeTreatedAsActive_AndAllowSuccessfulOrderCreation()
    {
        // Arrange
        var cmd = CreateSampleCmd("RG01378117");
        var auditKeluar = new AuditInfoType("   ", "3000-01-01", "00:00:00");
        var auditVoid = new AuditInfoType(" ", "3000-01-01", "00:00:00");
        var auditCancelOut = new AuditInfoType("  ", "3000-01-01", "00:00:00");
        var reg = new RegModel(
            cmd.RegId,
            new DateOnly(2026, 10, 16),
            new AuditInfoType("user-reg", new DateTime(2026, 10, 16)),
            auditKeluar,
            auditCancelOut,
            auditVoid,
            JenisRegEnum.RegInap,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId);
        var dokterOperator = CreateSamplePpa(cmd.DokterOperatorId);
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);
        var tarif = CreateSampleTarif(cmd.TarifId!);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == cmd.RegId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == cmd.LayananPengorderId)))
            .Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterPengorderId)))
            .Returns(MayBe.From(dokterPengorder));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterOperatorId)))
            .Returns(MayBe.From(dokterOperator));
        _icdRepoMock.Setup(x => x.LoadEntity(It.Is<IIcd10Key>(k => k.Icd10Id == cmd.DiagCode)))
            .Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.Is<IJenisOperasiKey>(k => k.JenisOperasiId == cmd.JenisOperasiId)))
            .Returns(MayBe.From(jenisOp));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == cmd.TarifId)))
            .Returns(MayBe.From(tarif));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns(new List<OrderOpView>());

        OrderOpModel? savedOrderOp = null;
        _orderOpRepoMock.Setup(x => x.SaveChanges(It.IsAny<OrderOpModel>()))
            .Callback<OrderOpModel>(m => savedOrderOp = m);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.OrderOpId.Should().NotBe("-");
        savedOrderOp.Should().NotBeNull();
        _orderOpRepoMock.Verify(x => x.SaveChanges(It.IsAny<OrderOpModel>()), Times.Once);
    }

    [Theory]
    [InlineData("", 2, "Kolf", "itemName")]
    [InlineData("   ", 2, "Kolf", "itemName")]
    [InlineData("PRC", 0, "Kolf", "jumlah")]
    [InlineData("PRC", -1, "Kolf", "jumlah")]
    [InlineData("PRC", 2, "", "satuan")]
    [InlineData("PRC", 2, "   ", "satuan")]
    public async Task Handle_WhenRegHasWhitespaceAuditFields_AndInvalidResource_ShouldTriggerResourceInvariantValidation_NotInactiveRegistrationException(
        string itemName, int jumlah, string satuan, string expectedParam)
    {
        // Arrange: Registration with whitespace audit fields (reproducing legacy RG01378117 scenario for TC-KMO-06)
        var invalidResource = new OrderOpResourceRequest(
            OrderOpResourceTypeEnum.Darah,
            itemName,
            jumlah,
            satuan,
            "Keterangan");
        var cmd = CreateSampleCmd(regId: "RG01378117", listResource: [invalidResource]);

        var auditKeluar = new AuditInfoType("   ", "3000-01-01", "00:00:00");
        var auditVoid = new AuditInfoType(" ", "3000-01-01", "00:00:00");
        var reg = new RegModel(
            cmd.RegId,
            new DateOnly(2026, 10, 16),
            new AuditInfoType("user-reg", new DateTime(2026, 10, 16)),
            auditKeluar,
            AuditInfoType.Default,
            auditVoid,
            JenisRegEnum.RegInap,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        var layanan = CreateSampleLayanan(cmd.LayananPengorderId);
        var dokterPengorder = CreateSamplePpa(cmd.DokterPengorderId);
        var icd = CreateSampleIcd(cmd.DiagCode);
        var jenisOp = CreateSampleJenisOp(cmd.JenisOperasiId);
        var tarif = CreateSampleTarif(cmd.TarifId!);

        _regRepoMock.Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == cmd.RegId)))
            .Returns(MayBe.From(reg));
        _layananRepoMock.Setup(x => x.LoadEntity(It.Is<ILayananKey>(k => k.LayananId == cmd.LayananPengorderId)))
            .Returns(MayBe.From(layanan));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterPengorderId)))
            .Returns(MayBe.From(dokterPengorder));
        _dokterRepoMock.Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == cmd.DokterOperatorId)))
            .Returns(MayBe.From(dokterPengorder));
        _icdRepoMock.Setup(x => x.LoadEntity(It.Is<IIcd10Key>(k => k.Icd10Id == cmd.DiagCode)))
            .Returns(MayBe.From(icd));
        _jenisOperasiRepoMock.Setup(x => x.LoadEntity(It.Is<IJenisOperasiKey>(k => k.JenisOperasiId == cmd.JenisOperasiId)))
            .Returns(MayBe.From(jenisOp));
        _tarifRepoMock.Setup(x => x.LoadEntity(It.Is<ITarifKey>(k => k.TarifId == cmd.TarifId)))
            .Returns(MayBe.From(tarif));
        _orderOpRepoMock.Setup(x => x.ListData(It.IsAny<Periode>())).Returns(new List<OrderOpView>());

        var sut = CreateSut();

        // Act & Assert
        // TC-KMO-06 verification: Active status validation passes, invariant failure comes from child resource validation
        var exception = await Assert.ThrowsAnyAsync<ArgumentException>(() => sut.Handle(cmd, CancellationToken.None));
        exception.Message.Should().NotContain("tidak aktif");
        exception.Message.Should().Contain(expectedParam);
    }
}
