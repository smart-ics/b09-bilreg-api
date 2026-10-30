using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.TriageEngine;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitReAssessTriageHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IIgdVisitSmassTaskRepo> _smassTaskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly ITglJamProvider _tglJamProvider = new TestTglJamProvider(new DateTime(2026, 1, 1, 10, 0, 0));
    private readonly ITriageMethodEngineResolver _engineResolver =
        new TriageMethodEngineResolver(new[] { new AtsTriageEngine() });

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = false
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const string DoctorUserId = "DK01";
    private const string NurseUserId = "PR01";

    private IgdVisitReAssessTriageHandler CreateSut()
    {
        return new IgdVisitReAssessTriageHandler(
            _visitRepo.Object,
            _ppaRepo.Object,
            _engineResolver,
            _tglJamProvider,
            _smassTaskRepo.Object,
            _smassGateway.Object,
            Options.Create(_options));
    }

    private static IgdVisitModel CreateTestVisitWithInitialTriage(string igdVisitId = IgdVisitId)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var visit = IgdVisitModel.Create(visitor, audit);

        // Initial triage: ATS3 (Yellow)
        visit.AssessTriage(
            assessment: new AtsAssessmentType(0, 3, 2, 4, 6, 5),
            method: TriageMethodEnum.Ats,
            level: TriageLevelEnum.Ats3,
            color: TriageColorEnum.Yellow,
            notes: "Triase Awal",
            isManualOverrideBlack: false,
            overrideByUserId: "-",
            overrideReason: "-",
            nextReTriageAt: new DateTime(2026, 1, 1, 8, 30, 0),
            audit: audit);

        return visit;
    }

    private static PpaType CreateDoctorPpa(string ppaId = DoctorUserId)
    {
        var satTugas = new SatTugasType("ST-DOK", "Dokter Umum", ProfesiType.Dokter);
        var listSatTugas = new[] { new PpaSatTugasType(satTugas, true) };
        return new PpaType(ppaId, "dr. Budi", "Budi", SmfType.Default, GroupSpesialisType.Default,
            listLayanan: [], listSatTugas: listSatTugas, listContact: []);
    }

    private static PpaType CreateNursePpa(string ppaId = NurseUserId)
    {
        var satTugas = new SatTugasType("ST-PRW", "Perawat IGD", ProfesiType.Perawat);
        var listSatTugas = new[] { new PpaSatTugasType(satTugas, true) };
        return new PpaType(ppaId, "Ns. Siti", "Siti", SmfType.Default, GroupSpesialisType.Default,
            listLayanan: [], listSatTugas: listSatTugas, listContact: []);
    }

    [Fact]
    public async Task Handle_WhenManualOverrideBlackAndUserNotDoctor_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        var nurse = CreateNursePpa();
        _ppaRepo.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == NurseUserId)))
            .Returns(MayBe.From(nurse));

        var cmd = new IgdVisitReAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "Kondisi memburuk, henti jantung di ruang tindakan",
            Notes: "Re-triase henti jantung",
            UserId: NurseUserId);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<UnauthorizedAccessException>()
            .WithMessage($"Petugas '{NurseUserId}' bukan dokter; tidak berwenang menetapkan status Black.");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenManualOverrideBlackAndUserNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        const string unknownUser = "UNKNOWN-99";
        _ppaRepo.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == unknownUser)))
            .Returns(MayBe<PpaType>.None);

        var cmd = new IgdVisitReAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "Pasien henti napas saat observasi",
            Notes: "Re-triase",
            UserId: unknownUser);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"Petugas '{unknownUser}' tidak ditemukan.");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData(" - ")]
    public async Task Handle_WhenManualOverrideBlackAndEmptyReason_ThrowsArgumentException(string? badReason)
    {
        // Arrange
        var cmd = new IgdVisitReAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: badReason!,
            Notes: "Re-triase override test",
            UserId: DoctorUserId);

        var sut = CreateSut();

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("Alasan klinis Manual Override Black wajib diisi.");
        _ppaRepo.Verify(r => r.LoadEntity(It.IsAny<IPpaKey>()), Times.Never);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenManualOverrideBlackAndDoctorValid_SavesTriageWithBlackStatus()
    {
        // Arrange
        var doctor = CreateDoctorPpa();
        var visit = CreateTestVisitWithInitialTriage();

        _ppaRepo.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == DoctorUserId)))
            .Returns(MayBe.From(doctor));
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitReAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "   Pasien henti jantung pasca resusitasi 30 menit tidak ada respon   ",
            Notes: "Re-triase meninggal",
            UserId: DoctorUserId);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TriageColor.Should().Be("BLACK");
        response.TriageLevel.Should().Be("ATS1");
        response.NoTriage.Should().Be(2);

        visit.HasTriage.Should().BeTrue();
        visit.TriageColor.Should().Be(TriageColorEnum.Black);
        visit.Triage.Color.Should().Be(TriageColorEnum.Black);
        visit.Triage.IsManualOverrideBlack.Should().BeTrue();
        visit.Triage.OverrideByUserId.Should().Be(DoctorUserId);
        visit.Triage.OverrideReason.Should().Be("Pasien henti jantung pasca resusitasi 30 menit tidak ada respon");
        visit.ListTriage.Should().HaveCount(2);

        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v =>
            v.TriageColor == TriageColorEnum.Black &&
            v.Triage.IsManualOverrideBlack == true &&
            v.Triage.OverrideByUserId == DoctorUserId &&
            v.Triage.OverrideReason == "Pasien henti jantung pasca resusitasi 30 menit tidak ada respon"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNormalReTriageByNurse_SucceedsWithoutQueryingPpaRepo()
    {
        // Arrange
        var visit = CreateTestVisitWithInitialTriage();
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitReAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 2,
            BloodCirculationScore: 1,
            GcsEyeScore: 4,
            GcsMotorScore: 6,
            GcsVoiceScore: 5,
            IsManualOverrideBlack: false,
            OverrideReason: "",
            Notes: "Kondisi pasien membaik",
            UserId: NurseUserId);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TriageColor.Should().Be("GREEN");
        response.TriageLevel.Should().Be("ATS4");
        response.NoTriage.Should().Be(2);

        visit.HasTriage.Should().BeTrue();
        visit.TriageColor.Should().Be(TriageColorEnum.Green);
        visit.Triage.IsManualOverrideBlack.Should().BeFalse();
        visit.Triage.OverrideByUserId.Should().Be("-");
        visit.Triage.OverrideReason.Should().Be("-");

        _ppaRepo.Verify(r => r.LoadEntity(It.IsAny<IPpaKey>()), Times.Never);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
    }
}
