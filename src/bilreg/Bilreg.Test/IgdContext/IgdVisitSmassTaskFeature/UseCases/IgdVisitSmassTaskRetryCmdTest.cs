using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature.UseCases;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitSmassTaskFeature.UseCases;

public class IgdVisitSmassTaskRetryCmdTest
{
    private readonly Mock<IIgdVisitSmassTaskRepo> _taskRepo = new();
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IRegRepo> _regRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly Mock<IEmrLabelGateway> _emrLabelGateway = new();

    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = true,
        SmassLayananId = "DEFAULT-LAYANAN",
        SmassTriagePaperId = "PP-ICS-TRGE",
        SmassTriagePaperName = "FORMULIR TRIASE IGD"
    };

    private const string TaskId = "IST-001";
    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string UserId = "USER-OP";

    private IgdVisitSmassTaskRetryHandler CreateSut()
    {
        return new IgdVisitSmassTaskRetryHandler(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            Options.Create(_options));
    }

    private static IgdVisitModel CreateTestVisit()
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");

        return new IgdVisitModel(
            igdVisitId: IgdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: false,
            triage: IgdVisitTriageType.Default,
            administrativeState: AdministrativeStateEnum.Daftar,
            reg: new RegReff(RegId, "PAS-001", "Pasien Tes"),
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

    private static RegModel CreateTestReg()
    {
        var regDate = new DateOnly(2026, 9, 28);
        return new RegModel(
            RegId,
            regDate,
            new AuditInfoType("audit-user", regDate.ToDateTime(TimeOnly.MinValue)),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.Darurat,
            PasienModel.Default.ToReff(),
            TipeJaminanType.Default.ToReff(),
            PolisModel.Default.ToReff(),
            KelasType.Default.ToReff(),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            new LayananReff("1GD01", "IGD"),
            KarcisType.Default.ToReff(),
            RegEligibilityType.Default,
            []);
    }

    [Fact]
    public async Task Handle_ValidFailedLinkTask_ExecutesRetryAndEmrGateway_ReturnsView()
    {
        // Arrange
        var task = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 0, SmassTaskTypeEnum.Link);
        task.MarkFailed("Network timeout");

        _taskRepo
            .Setup(r => r.LoadEntity(It.Is<IIgdVisitSmassTaskKey>(k => k.IgdVisitSmassTaskId == TaskId)))
            .Returns(MayBe.From(task));

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(CreateTestVisit()));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateTestReg()));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-RETRY", null, new[] { "ASSESS-RETRY" }));

        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskRetryCmd(TaskId, UserId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Should().NotBeNull();
        result.TaskStatus.Should().Be("SUCCEEDED");
        result.AssessmentId.Should().Be("ASSESS-RETRY");

        _taskRepo.Verify(r => r.SaveChanges(task), Times.Once);
        _emrLabelGateway.Verify(e => e.AddSmassLabel(
            It.Is<EmrAddSmassLabelRequest>(r => r.AssesmentId == "ASSESS-RETRY" && r.UserrId == UserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenTaskIdIsEmpty_ThrowsArgumentException()
    {
        // Arrange
        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskRetryCmd("");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
    }

    [Fact]
    public async Task Handle_WhenTaskNotFound_ThrowsKeyNotFoundException()
    {
        // Arrange
        _taskRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitSmassTaskKey>()))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskRetryCmd("NON-EXISTENT");

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<KeyNotFoundException>();
    }

    [Fact]
    public async Task Handle_WhenTaskNotFailed_ThrowsInvalidOperationException()
    {
        // Arrange
        var task = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 0, SmassTaskTypeEnum.Link);
        // Task is Pending, not Failed

        _taskRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitSmassTaskKey>()))
            .Returns(MayBe.From(task));

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskRetryCmd(TaskId);

        // Act
        var act = () => sut.Handle(cmd, CancellationToken.None);

        // Assert
        await act.Should().ThrowAsync<InvalidOperationException>();
    }
}
