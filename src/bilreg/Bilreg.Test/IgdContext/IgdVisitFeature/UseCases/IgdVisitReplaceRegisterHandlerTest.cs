using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitReplaceRegisterHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ITindakanIgdRepo> _tindakanRepo = new();
    private readonly Mock<IBhpIgdRepo> _bhpRepo = new();
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly Mock<IEmrLabelGateway> _emrLabelGateway = new();

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = true,
        SmassLayananId = "DEFAULT-LAYANAN",
        SmassTriagePaperId = "PP-ICS-TRGE",
        SmassTriagePaperName = "FORMULIR TRIASE IGD"
    };

    private const string IgdVisitId = "IGV-2026-0001";
    private const string OldRegId = "REG-OLD";
    private const string NewRegId = "REG-NEW";
    private const string UserId = "USER-007";

    private IgdVisitReplaceRegisterHandler CreateSut(IgdVisitOptions? options = null)
    {
        var opt = Options.Create(options ?? _options);
        return new IgdVisitReplaceRegisterHandler(
            _visitRepo.Object,
            _regRepo.Object,
            TestTglJamProvider.Instance,
            _tindakanRepo.Object,
            _bhpRepo.Object,
            _taskRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            opt);
    }

    private static IgdVisitModel CreateRegisteredVisit(string igdVisitId = IgdVisitId, string currentRegId = OldRegId)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        return new IgdVisitModel(
            igdVisitId: igdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Registered,
            reg: new RegReff(currentRegId, "P001", "Pasien Tes"),
            redirection: RedirectionType.Default,
            bedId: "-",
            triageMethod: TriageMethodEnum.Unknown,
            triageColor: TriageColorEnum.Unknown,
            lastTriageAt: new DateTime(3000, 1, 1),
            nextReTriageAt: new DateTime(3000, 1, 1),
            auditTrail: AuditTrailType.Create(audit.UserId, audit.Timestamp),
            dischargeAudit: AuditInfoType.Default,
            listTriage: [],
            listEvent: []);
    }

    private static RegModel CreateTestReg(
        string regId = NewRegId,
        string layananId = "1GD01",
        string layananName = "IGD",
        JenisRegEnum jenisReg = JenisRegEnum.Darurat)
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            regId,
            regDate,
            new AuditInfoType("tester", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            jenisReg,
            PasienModel.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.TipeJaminanType.Default.ToReff(),
            Domain.AdmisiContext.JaminanFeature.PolisModel.Default.ToReff(),
            Domain.BedUsageContext.WardFeature.KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            Domain.AdmisiContext.RujukanFeature.RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff(layananId, layananName),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public async Task Handle_ValidRequest_ReplacesRegister_AndWiredEmrLabelGatewayWithUserId()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var newReg = CreateTestReg();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == IgdVisitId)))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);

        _regRepo
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == NewRegId)))
            .Returns(MayBe.From(newReg));

        _visitRepo
            .Setup(r => r.GetByRegId(NewRegId))
            .Returns(MayBe<IgdVisitView>.None);

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-01", null, new List<string> { "ASM-01" }));

        _emrLabelGateway
            .Setup(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmrLabelGatewayResult(true, "Label created"));

        var sut = CreateSut();

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _visitRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitModel>(v =>
            v.IgdVisitId == IgdVisitId &&
            v.Reg.RegId == NewRegId)), Times.Once);

        _taskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.IgdVisitId == IgdVisitId &&
            t.TaskStatus == SmassTaskStatusEnum.Succeeded)), Times.Once);

        // Verify EmrLabelGateway was called with NewRegId and UserId
        _emrLabelGateway.Verify(g => g.AddSmassLabel(It.Is<EmrAddSmassLabelRequest>(req =>
            req.AssesmentId == "ASM-01" &&
            req.RegId == NewRegId &&
            req.UserrId == UserId &&
            req.PaperId == "PP-ICS-TRGE" &&
            req.PaperName == "FORMULIR TRIASE IGD" &&
            req.LayananId == "1GD01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenEmrLabelThrowsException_RegistrationAndSmassTaskSucceed()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var newReg = CreateTestReg();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(newReg));

        _visitRepo
            .Setup(r => r.GetByRegId(NewRegId))
            .Returns(MayBe<IgdVisitView>.None);

        _taskRepo
            .Setup(r => r.FindByBusinessKey(IgdVisitId, 0, SmassTaskTypeEnum.Link))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-01", null, new List<string> { "ASM-01" }));

        _emrLabelGateway
            .Setup(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("EMR 2.0 service down"));

        var sut = CreateSut();

        // Act
        var act = async () => await sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().NotThrowAsync();

        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        _taskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.TaskStatus == SmassTaskStatusEnum.Succeeded)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSmassIntegrationDisabled_DoesNotCallGatewayOrEmr()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var newReg = CreateTestReg();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(newReg));

        _visitRepo
            .Setup(r => r.GetByRegId(NewRegId))
            .Returns(MayBe<IgdVisitView>.None);

        var disabledOptions = new IgdVisitOptions
        {
            EnableSmassIntegration = false,
            SmassLayananId = _options.SmassLayananId,
            SmassTriagePaperId = _options.SmassTriagePaperId,
            SmassTriagePaperName = _options.SmassTriagePaperName
        };
        var sut = CreateSut(disabledOptions);

        // Act
        await sut.Handle(cmd, CancellationToken.None);

        // Assert
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Once);
        _smassGateway.Verify(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()), Times.Never);
        _emrLabelGateway.Verify(g => g.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenHasTindakan_ThrowsInvalidOperationException()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(true);

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*Tindakan*");
    }

    [Fact]
    public async Task Handle_WhenHasBhp_ThrowsInvalidOperationException()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(true);

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*BHP*");
    }

    [Fact]
    public async Task Handle_WhenRegNotDarurat_ThrowsInvalidOperationException()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var nonDaruratReg = CreateTestReg(jenisReg: JenisRegEnum.RegJalan);
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(nonDaruratReg));

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*bukan Register IGD*");
    }

    [Fact]
    public async Task Handle_WhenRegAlreadyLinkedToAnotherVisit_ThrowsInvalidOperationException()
    {
        // Arrange
        var visit = CreateRegisteredVisit();
        var newReg = CreateTestReg();
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(visit));

        _tindakanRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);
        _bhpRepo.Setup(r => r.AnyForVisit(visit)).Returns(false);

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(newReg));

        var existingOtherVisit = new IgdVisitView(
            "IGV-OTHER", DateTime.Now, "Other", "L", "D1", "Dokter", false,
            "-", "-", DateTime.MinValue, DateTime.MinValue, "Registered", NewRegId, "-", "-");

        _visitRepo
            .Setup(r => r.GetByRegId(NewRegId))
            .Returns(MayBe.From(existingOtherVisit));

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*sudah di link-kan dengan IgdVisit*");
    }

    [Fact]
    public async Task Handle_WhenVisitNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        var cmd = new IgdVisitReplaceRegisterCmd(IgdVisitId, NewRegId, UserId);

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var sut = CreateSut();

        // Act & Assert
        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage($"*{IgdVisitId}*");
    }

    [Theory]
    [InlineData("", "REG-NEW", "USER-1")]
    [InlineData("   ", "REG-NEW", "USER-1")]
    [InlineData("IGV-001", "", "USER-1")]
    [InlineData("IGV-001", "   ", "USER-1")]
    [InlineData("IGV-001", "REG-NEW", "")]
    [InlineData("IGV-001", "REG-NEW", "   ")]
    public async Task Handle_EmptyParameters_ThrowsArgumentException(string visitId, string newRegId, string userId)
    {
        var cmd = new IgdVisitReplaceRegisterCmd(visitId, newRegId, userId);
        var sut = CreateSut();

        var act = async () => await sut.Handle(cmd, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>();
    }
}
