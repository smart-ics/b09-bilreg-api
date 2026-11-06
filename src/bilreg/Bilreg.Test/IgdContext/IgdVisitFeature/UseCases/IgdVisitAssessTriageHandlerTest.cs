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

public class IgdVisitAssessTriageHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IPpaRepo> _ppaRepo = new();
    private readonly Mock<IIgdVisitSmassTaskRepo> _smassTaskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly ITglJamProvider _tglJamProvider = new TestTglJamProvider(new DateTime(2026, 1, 1, 9, 0, 0));
    private readonly ITriageMethodEngineResolver _engineResolver =
        new TriageMethodEngineResolver(new[] { new AtsTriageEngine() });

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = false
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const string DoctorUserId = "DK01";
    private const string NurseUserId = "PR01";

    private IgdVisitAssessTriageHandler CreateSut()
    {
        return new IgdVisitAssessTriageHandler(
            _visitRepo.Object,
            _ppaRepo.Object,
            _engineResolver,
            _tglJamProvider,
            _smassTaskRepo.Object,
            _smassGateway.Object,
            Options.Create(_options));
    }

    private static IgdVisitModel CreateTestVisit(string igdVisitId = IgdVisitId)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        return IgdVisitModel.Create(visitor, audit);
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

        var cmd = new IgdVisitAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "Pasien DOA tiba di IGD henti sirkulasi",
            Notes: "Triase DOA",
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

        var cmd = new IgdVisitAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "Pasien henti napas",
            Notes: "DOA",
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
        var cmd = new IgdVisitAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: badReason!,
            Notes: "Override test",
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
        var visit = CreateTestVisit();

        _ppaRepo.Setup(r => r.LoadEntity(It.Is<IPpaKey>(k => k.PpaId == DoctorUserId)))
            .Returns(MayBe.From(doctor));
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 0,
            BloodCirculationScore: 0,
            GcsEyeScore: 1,
            GcsMotorScore: 1,
            GcsVoiceScore: 1,
            IsManualOverrideBlack: true,
            OverrideReason: "   Pasien DOA di TKP lebam mayat positif   ",
            Notes: "Kasus DOA",
            UserId: DoctorUserId);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TriageColor.Should().Be("BLACK");
        response.TriageLevel.Should().Be("ATS1");

        visit.HasTriage.Should().BeTrue();
        visit.TriageColor.Should().Be(TriageColorEnum.Black);
        visit.Triage.Color.Should().Be(TriageColorEnum.Black);
        visit.Triage.IsManualOverrideBlack.Should().BeTrue();
        visit.Triage.OverrideByUserId.Should().Be(DoctorUserId);
        visit.Triage.OverrideReason.Should().Be("Pasien DOA di TKP lebam mayat positif");

        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v =>
            v.TriageColor == TriageColorEnum.Black &&
            v.Triage.IsManualOverrideBlack == true &&
            v.Triage.OverrideByUserId == DoctorUserId &&
            v.Triage.OverrideReason == "Pasien DOA di TKP lebam mayat positif"
        )), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNormalTriageByNurse_SucceedsWithoutQueryingPpaRepo()
    {
        // Arrange
        var visit = CreateTestVisit();
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitAssessTriageCmd(
            IgdVisitId: IgdVisitId,
            AirwaysScore: 0,
            BreathingScore: 3,
            BloodCirculationScore: 2,
            GcsEyeScore: 4,
            GcsMotorScore: 6,
            GcsVoiceScore: 5,
            IsManualOverrideBlack: false,
            OverrideReason: "",
            Notes: "Triase normal oleh perawat",
            UserId: NurseUserId);

        var sut = CreateSut();

        // Act
        var response = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        response.Should().NotBeNull();
        response.TriageColor.Should().Be("YELLOW");
        response.TriageLevel.Should().Be("ATS3");

        visit.HasTriage.Should().BeTrue();
        visit.TriageColor.Should().Be(TriageColorEnum.Yellow);
        visit.Triage.IsManualOverrideBlack.Should().BeFalse();
        visit.Triage.OverrideByUserId.Should().Be("-");
        visit.Triage.OverrideReason.Should().Be("-");

        _ppaRepo.Verify(r => r.LoadEntity(It.IsAny<IPpaKey>()), Times.Never);
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
    }
}
