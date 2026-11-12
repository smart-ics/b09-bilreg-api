using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmUpdateInpatientVisitHandlerTest
{
    private readonly Mock<IRegRepo> _regRepoMock = new();
    private readonly Mock<IRegInapRepo> _regInapRepoMock = new();
    private readonly Mock<IPpaRepo> _ppaRepoMock = new();
    private readonly Mock<IProsedureMasukInapRepo> _prosedurMasukInapRepoMock = new();
    private readonly Mock<ICaraMasukDkRepo> _caraMasukRepoMock = new();
    private readonly Mock<IAuditRepo> _auditRepoMock = new();

    private AdmUpdateInpatientVisitHandler CreateSut() =>
        new(
            _regRepoMock.Object,
            _regInapRepoMock.Object,
            _ppaRepoMock.Object,
            _prosedurMasukInapRepoMock.Object,
            _caraMasukRepoMock.Object,
            _auditRepoMock.Object,
            TestTglJamProvider.Instance);

    private void SetupMasterData(string dokterId = "DOC01", string prosedurId = "PRO01", string caraMasukId = "CM01")
    {
        _ppaRepoMock
            .Setup(x => x.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == dokterId)))
            .Returns(MayBe.From(PpaType.Default with { PpaId = dokterId, PpaName = $"Dokter {dokterId}" }));

        _prosedurMasukInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IProsedurMasukInapKey>(k => k.ProsedurMasukInapId == prosedurId)))
            .Returns(MayBe.From(new ProsedurMasukInapType(prosedurId, $"Prosedur {prosedurId}", ProsedurMasukInapDkType.Default)));

        _caraMasukRepoMock
            .Setup(x => x.LoadEntity(It.Is<ICaraMasukDkKey>(k => k.CaraMasukDkId == caraMasukId)))
            .Returns(MayBe.From(new CaraMasukDkType(caraMasukId, $"Cara Masuk {caraMasukId}")));
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndRegIbuIsEmpty_ThrowsArgumentException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "3", "", "USER01");

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Nomor registrasi ibu wajib diisi untuk jenis inap Perinatal*");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "3", "REG-IBU-999", "USER01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-999")))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi ibu 'REG-IBU-999' tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherIsVoid_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "3", "REG-IBU-001", "USER01");

        var mother = CreateSampleReg("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2",
            regVoidAudit: new AuditInfoType("USER_VOID", new DateTime(2026, 5, 1)));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registrasi ibu sudah berstatus void.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherIsDischarged_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "3", "REG-IBU-001", "USER01");

        var mother = CreateSampleReg("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2",
            regKeluarAudit: new AuditInfoType("USER_OUT", new DateTime(2026, 5, 1)));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("Registrasi ibu sudah berstatus pulang/keluar.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherNotObstetri_ThrowsInvalidOperationException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "3", "REG-IBU-001", "USER01");

        var mother = CreateSampleReg("REG-IBU-001", "CM01", "DOC01", jenisInapId: "1"); // 1 = Umum

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("JENIS INAP IBU invalid: Registrasi ibu harus berjenis inap Obstetri.");
    }

    [Fact]
    public async Task Handle_WhenJenisInapPerinatalAndMotherValid_SuccessAndPersistsBothRegs()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC02", "PRO02", "CM02", "3", "REG-IBU-001", "USER01");

        var mother = CreateSampleReg("REG-IBU-001", "CM01", "DOC01", jenisInapId: "2");
        var patientReg = CreateSampleReg("REG001", "CM01", "DOC01", jenisInapId: "1");
        var patientRegInap = CreateSampleRegInap("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-IBU-001")))
            .Returns(MayBe.From(mother));

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC02", "PRO02", "CM02");

        RegModel? savedReg = null;
        RegInapModel? savedRegInap = null;

        _regRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(m => savedReg = m);

        _regInapRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(m => savedRegInap = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedReg.Should().NotBeNull();
        savedReg!.JenisInap.JenisInapId.Should().Be("3");
        savedReg.RegIbu.Should().Be("REG-IBU-001");
        savedReg.CaraMasukDk.CaraMasukDkId.Should().Be("CM02");
        savedReg.Dokter.PpaId.Should().Be("DOC02");

        savedRegInap.Should().NotBeNull();
        savedRegInap!.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("PRO02");
        savedRegInap.Dpjp.PpaId.Should().Be("DOC02");

        _auditRepoMock.Verify(x => x.SaveChanges(It.IsAny<AuditLog>()), Times.Exactly(2));
    }

    [Fact]
    public async Task Handle_WhenNonPerinatal_ClearsRegIbuAndPersistsSuccess()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "1", "OLD-IBU", "USER01");

        var patientReg = CreateSampleReg("REG001", "CM01", "DOC01", jenisInapId: "3", regIbu: "OLD-IBU");
        var patientRegInap = CreateSampleRegInap("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC01", "PRO01", "CM01");

        RegModel? savedReg = null;
        _regRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegModel>()))
            .Callback<RegModel>(m => savedReg = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedReg.Should().NotBeNull();
        savedReg!.JenisInap.JenisInapId.Should().Be("1");
        savedReg.RegIbu.Should().BeEmpty();
        _regRepoMock.Verify(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "OLD-IBU")), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenDpjpUnchanged_DoesNotThrowDuplicatePrimaryAndUpdatesProsedur()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO02", "CM01", "1", "", "USER01");

        var patientReg = CreateSampleReg("REG001", "CM01", "DOC01", jenisInapId: "1");
        var patientRegInap = CreateSampleRegInap("REG001", "PRO01", "DOC01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientRegInap));

        SetupMasterData("DOC01", "PRO02", "CM01");

        RegInapModel? savedRegInap = null;
        _regInapRepoMock
            .Setup(x => x.SaveChanges(It.IsAny<RegInapModel>()))
            .Callback<RegInapModel>(m => savedRegInap = m);

        await sut.Handle(cmd, CancellationToken.None);

        savedRegInap.Should().NotBeNull();
        savedRegInap!.Dpjp.PpaId.Should().Be("DOC01");
        savedRegInap.ProsedurMasukInap.ProsedurMasukInapId.Should().Be("PRO02");
    }

    [Fact]
    public async Task Handle_WhenTargetRegNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG_MISSING", "DOC01", "PRO01", "CM01", "1", "", "USER01");

        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG_MISSING")))
            .Returns(MayBe<RegModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi 'REG_MISSING' tidak ditemukan*");
    }

    [Fact]
    public async Task Handle_WhenTargetRegInapNotFound_ThrowsKeyNotFoundException()
    {
        var sut = CreateSut();
        var cmd = new AdmUpdateInpatientVisitCmd("REG001", "DOC01", "PRO01", "CM01", "1", "", "USER01");

        var patientReg = CreateSampleReg("REG001", "CM01", "DOC01", jenisInapId: "1");
        _regRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe.From(patientReg));

        _regInapRepoMock
            .Setup(x => x.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG001")))
            .Returns(MayBe<RegInapModel>.None);

        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*Registrasi rawat inap 'REG001' tidak ditemukan*");
    }

    private static RegModel CreateSampleReg(
        string regId,
        string caraMasukDkId,
        string dokterId,
        string? jenisInapId = null,
        string? regIbu = null,
        AuditInfoType? regVoidAudit = null,
        AuditInfoType? regKeluarAudit = null) =>
        new(
            regId,
            new DateOnly(2025, 5, 1),
            AuditInfoType.Default,
            regKeluarAudit ?? AuditInfoType.Default,
            AuditInfoType.Default,
            regVoidAudit ?? AuditInfoType.Default,
            JenisRegEnum.RegInap,
            new PasienReff("PAS001", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            new CaraMasukDkType(caraMasukDkId, "Cara Masuk Test"),
            RujukanType.Default.ToReff(),
            new PpaReff(dokterId, "Dokter Test"),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            [],
            jenisInap: jenisInapId is null ? null : new JenisInapType(jenisInapId, "Test"),
            regIbu: regIbu);

    private static RegInapModel CreateSampleRegInap(string regId, string prosedurMasukInapId, string dpjpId) =>
        RegInapModel.Create(
            regId,
            new ProsedurMasukInapType(prosedurMasukInapId, "Prosedur Test", ProsedurMasukInapDkType.Default),
            new PpaReff(dpjpId, "DPJP Test"),
            new DateOnly(2025, 5, 1));
}
