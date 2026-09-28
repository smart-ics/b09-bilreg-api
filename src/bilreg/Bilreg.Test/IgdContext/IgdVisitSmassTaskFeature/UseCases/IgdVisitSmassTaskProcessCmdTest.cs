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

public class IgdVisitSmassTaskProcessCmdTest
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

    private const string IgdVisitId = "IGV-2026-0001";
    private const string RegId = "REG-001";
    private const string BatchUserId = "BATCH-USER-01";

    private IgdVisitSmassTaskProcessHandler CreateSut()
    {
        return new IgdVisitSmassTaskProcessHandler(
            _taskRepo.Object,
            _visitRepo.Object,
            _regRepo.Object,
            _smassGateway.Object,
            _emrLabelGateway.Object,
            Options.Create(_options));
    }

    private static IgdVisitModel CreateTestVisit(bool hasTriage = true)
    {
        var audit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var listTriage = hasTriage
            ? new List<IgdVisitTriageType> { IgdVisitTriageType.Default with { NoTriage = 1 } }
            : new List<IgdVisitTriageType>();

        return new IgdVisitModel(
            igdVisitId: IgdVisitId,
            daftarDateTime: audit.Timestamp,
            visitor: visitor,
            dokter: PpaType.Default.ToReff(),
            hasTriage: hasTriage,
            triage: hasTriage ? listTriage[0] : IgdVisitTriageType.Default,
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
            listTriage: listTriage,
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
    public async Task Handle_WhenProcessableTasksExist_ExecutesRetryAndEmrGatewayForLinkTasks()
    {
        // Arrange
        var linkTask = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 0, SmassTaskTypeEnum.Link);
        linkTask.MarkFailed("Link error");

        var generateTask = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 1, SmassTaskTypeEnum.Generate);
        generateTask.MarkFailed("Generate error");

        _taskRepo
            .Setup(r => r.ListProcessable())
            .Returns(new[] { linkTask, generateTask });

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(CreateTestVisit(hasTriage: true)));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateTestReg()));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-BATCH", null, new[] { "ASSESS-BATCH" }));

        _smassGateway
            .Setup(g => g.GenerateIgdTriage(It.IsAny<SmassGenerateIgdTriageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-GEN", null));

        _emrLabelGateway
            .Setup(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new EmrLabelGatewayResult(true, null));

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskProcessCmd(BatchUserId);

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(2);
        result.Succeeded.Should().Be(2);
        result.Failed.Should().Be(0);

        linkTask.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);
        generateTask.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);

        _emrLabelGateway.Verify(e => e.AddSmassLabel(
            It.Is<EmrAddSmassLabelRequest>(r => r.AssesmentId == "ASSESS-BATCH" && r.UserrId == BatchUserId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenOneTaskFails_ContinuesBatchAndReportsSummary()
    {
        // Arrange
        var failLinkTask = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 0, SmassTaskTypeEnum.Link);
        failLinkTask.MarkFailed("Initial link failure");

        var succGenerateTask = IgdVisitSmassTaskModel.CreatePending(IgdVisitId, 1, SmassTaskTypeEnum.Generate);
        succGenerateTask.MarkFailed("Initial gen failure");

        _taskRepo
            .Setup(r => r.ListProcessable())
            .Returns(new[] { failLinkTask, succGenerateTask });

        _visitRepo
            .Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe.From(CreateTestVisit(hasTriage: true)));

        _regRepo
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe.From(CreateTestReg()));

        _smassGateway
            .Setup(g => g.LinkIgdVisit(It.IsAny<SmassLinkIgdVisitRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(false, null, "Link gateway persistent failure"));

        _smassGateway
            .Setup(g => g.GenerateIgdTriage(It.IsAny<SmassGenerateIgdTriageRequest>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(true, "ASSESS-GEN-OK", null));

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskProcessCmd();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(2);
        result.Succeeded.Should().Be(1);
        result.Failed.Should().Be(1);

        failLinkTask.TaskStatus.Should().Be(SmassTaskStatusEnum.Failed);
        succGenerateTask.TaskStatus.Should().Be(SmassTaskStatusEnum.Succeeded);

        _emrLabelGateway.Verify(e => e.AddSmassLabel(It.IsAny<EmrAddSmassLabelRequest>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoProcessableTasks_ReturnsZeroCounts()
    {
        // Arrange
        _taskRepo
            .Setup(r => r.ListProcessable())
            .Returns(Array.Empty<IgdVisitSmassTaskModel>());

        var sut = CreateSut();
        var cmd = new IgdVisitSmassTaskProcessCmd();

        // Act
        var result = await sut.Handle(cmd, CancellationToken.None);

        // Assert
        result.Total.Should().Be(0);
        result.Succeeded.Should().Be(0);
        result.Failed.Should().Be(0);
    }
}
