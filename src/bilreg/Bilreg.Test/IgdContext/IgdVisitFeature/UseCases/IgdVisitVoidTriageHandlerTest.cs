using System.Transactions;
using Bilreg.Application.IgdContext;
using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Application.IgdContext.IgdVisitFeature.UseCases;
using Bilreg.Application.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Application.IgdContext.Integration;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.IgdVisitSmassTaskFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Test.Shared;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.IgdContext.IgdVisitFeature.UseCases;

public class IgdVisitVoidTriageHandlerTest
{
    private readonly Mock<IIgdVisitRepo> _visitRepo = new();
    private readonly Mock<IIgdVisitSmassTaskRepo> _smassTaskRepo = new();
    private readonly Mock<ISmassAssessmentGateway> _smassGateway = new();
    private readonly DateTime _now = new(2026, 1, 1, 10, 0, 0);
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IgdVisitOptions _options = new()
    {
        EnableSmassIntegration = false
    };

    private const string UserId = "PR01";

    public IgdVisitVoidTriageHandlerTest()
    {
        _tglJamProvider = new TestTglJamProvider(_now);
    }

    private IgdVisitVoidTriageHandler CreateSut() => new(
        _visitRepo.Object,
        _tglJamProvider,
        _smassTaskRepo.Object,
        _smassGateway.Object,
        Options.Create(_options));

    private static IgdVisitModel CreateVisitWithTriages(int count)
    {
        var createAudit = new AuditInfoType("creator", new DateTime(2026, 1, 1, 8, 0, 0));
        var visitor = new VisitorType("Pasien Tes", "L", new DateOnly(1990, 1, 1), "0812000");
        var visit = IgdVisitModel.Create(visitor, createAudit);

        if (count >= 1)
        {
            var triage1Time = new DateTime(2026, 1, 1, 8, 30, 0);
            visit.AssessTriage(
                assessment: new AtsAssessmentType(0, 1, 1, 4, 6, 5),
                method: TriageMethodEnum.Ats,
                level: TriageLevelEnum.Ats3,
                color: TriageColorEnum.Yellow,
                notes: "Triase awal",
                isManualOverrideBlack: false,
                overrideByUserId: "-",
                overrideReason: "-",
                nextReTriageAt: triage1Time.AddMinutes(30),
                audit: new AuditInfoType("PR01", triage1Time));
        }

        if (count >= 2)
        {
            var triage2Time = new DateTime(2026, 1, 1, 9, 0, 0);
            visit.AssessTriage(
                assessment: new AtsAssessmentType(1, 2, 2, 4, 5, 4),
                method: TriageMethodEnum.Ats,
                level: TriageLevelEnum.Ats2,
                color: TriageColorEnum.Red,
                notes: "Re-triase",
                isManualOverrideBlack: false,
                overrideByUserId: "-",
                overrideReason: "-",
                nextReTriageAt: triage2Time.AddMinutes(15),
                audit: new AuditInfoType("PR02", triage2Time));
        }

        return visit;
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenIgdVisitIdInvalid_ThrowsArgumentException(string? badId)
    {
        var cmd = new IgdVisitVoidTriageCmd(badId!, 1, UserId, "Salah input");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Handle_WhenNoTriageInvalid_ThrowsArgumentException(int badNoTriage)
    {
        var cmd = new IgdVisitVoidTriageCmd("IGV-001", badNoTriage, UserId, "Salah input");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Handle_WhenUserIdInvalid_ThrowsArgumentException(string? badUser)
    {
        var cmd = new IgdVisitVoidTriageCmd("IGV-001", 1, badUser!, "Salah input");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("-")]
    [InlineData(" - ")]
    public async Task Handle_WhenVoidReasonInvalid_ThrowsArgumentException(string? badReason)
    {
        var cmd = new IgdVisitVoidTriageCmd("IGV-001", 1, UserId, badReason!);
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentException>()
            .WithMessage("*Alasan pembatalan triage wajib diisi*");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenVisitNotFound_ThrowsKeyNotFoundException()
    {
        _visitRepo.Setup(r => r.LoadEntity(It.IsAny<IIgdVisitKey>()))
            .Returns(MayBe<IgdVisitModel>.None);

        var cmd = new IgdVisitVoidTriageCmd("IGV-404", 1, UserId, "Salah pasien");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>()
            .WithMessage("*IGV-404*");
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
        _smassGateway.Verify(g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenOnlyTriageVoided_ResetsHasTriageToFalseAndReturnsDisabledWhenSmassOff()
    {
        var visit = CreateVisitWithTriages(1);
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 1, UserId, "Salah input skor GCS");
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IgdVisitId.Should().Be(visit.IgdVisitId);
        result.NoTriage.Should().Be(1);
        result.IsVoided.Should().BeTrue();
        result.HasTriage.Should().BeFalse();
        result.ActiveTriageLevel.Should().BeEmpty();
        result.ActiveTriageColor.Should().BeEmpty();
        result.LastTriageAt.Should().Be(new DateTime(3000, 1, 1));
        result.NextReTriageAt.Should().Be(new DateTime(3000, 1, 1));
        result.SmassVoidStatus.Should().Be("Disabled");

        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
        _smassTaskRepo.Verify(r => r.FindByBusinessKey(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<SmassTaskTypeEnum>()), Times.Never);
        _smassGateway.Verify(g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenLatestTriageVoidedAndEarlierActiveExists_RollsBackActiveTriageState()
    {
        var visit = CreateVisitWithTriages(2);
        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 2, UserId, "Duplikat re-triase");
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IgdVisitId.Should().Be(visit.IgdVisitId);
        result.NoTriage.Should().Be(2);
        result.IsVoided.Should().BeTrue();
        result.HasTriage.Should().BeTrue();
        result.ActiveTriageLevel.Should().Be("ATS3");
        result.ActiveTriageColor.Should().Be("YELLOW");
        result.LastTriageAt.Should().Be(new DateTime(2026, 1, 1, 8, 30, 0));
        result.NextReTriageAt.Should().Be(new DateTime(2026, 1, 1, 9, 0, 0));
        result.SmassVoidStatus.Should().Be("Disabled");

        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSmassEnabled_InvokesDeleteAssesmentPostCommitOutsideTransactionScope()
    {
        _options.EnableSmassIntegration = true;
        var visit = CreateVisitWithTriages(1);
        var generateTask = IgdVisitSmassTaskModel.CreatePending(
            visit.IgdVisitId, 1, SmassTaskTypeEnum.Generate);
        generateTask.MarkSucceeded("ASM-12345");

        var callOrder = new List<string>();
        Transaction? ambientTxDuringSave = null;
        Transaction? ambientTxDuringSmass = null;

        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));
        _visitRepo.Setup(r => r.SaveChanges(visit))
            .Callback(() =>
            {
                callOrder.Add("SaveVisit");
                ambientTxDuringSave = Transaction.Current;
            });

        _smassTaskRepo.Setup(r => r.FindByBusinessKey(visit.IgdVisitId, 1, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _smassTaskRepo.Setup(r => r.FindByBusinessKey(visit.IgdVisitId, 1, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        _smassGateway.Setup(g => g.DeleteAssesment("ASM-12345", It.IsAny<CancellationToken>()))
            .Callback(() =>
            {
                callOrder.Add("SmassDelete");
                ambientTxDuringSmass = Transaction.Current;
            })
            .ReturnsAsync(new SmassGatewayResult(true, "ASM-12345", null));

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 1, UserId, "Salah input");
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsVoided.Should().BeTrue();
        result.HasTriage.Should().BeFalse();
        result.SmassVoidStatus.Should().Be("Voided");

        callOrder.Should().ContainInOrder("SaveVisit", "SmassDelete");
        ambientTxDuringSave.Should().NotBeNull("visit persistence must run inside TransHelper scope");
        ambientTxDuringSmass.Should().BeNull("SMASS HTTP call must execute strictly post-commit outside TransHelper scope");
        _smassTaskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.TaskType == SmassTaskTypeEnum.Void
            && t.TaskStatus == SmassTaskStatusEnum.Succeeded
            && t.AssessmentId == "ASM-12345")), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSmassGatewayFails_DoesNotThrowAndReturnsFailedStatus()
    {
        _options.EnableSmassIntegration = true;
        var visit = CreateVisitWithTriages(1);
        var generateTask = IgdVisitSmassTaskModel.CreatePending(
            visit.IgdVisitId, 1, SmassTaskTypeEnum.Generate);
        generateTask.MarkSucceeded("ASM-999");

        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));
        _smassTaskRepo.Setup(r => r.FindByBusinessKey(visit.IgdVisitId, 1, SmassTaskTypeEnum.Generate))
            .Returns(MayBe.From(generateTask));
        _smassTaskRepo.Setup(r => r.FindByBusinessKey(visit.IgdVisitId, 1, SmassTaskTypeEnum.Void))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);
        _smassGateway.Setup(g => g.DeleteAssesment("ASM-999", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new SmassGatewayResult(false, null, "HTTP 503 Service Unavailable"));

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 1, UserId, "Salah input");
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsVoided.Should().BeTrue();
        result.HasTriage.Should().BeFalse();
        result.SmassVoidStatus.Should().Be("Failed");
        _visitRepo.Verify(r => r.SaveChanges(visit), Times.Once);
        _smassTaskRepo.Verify(r => r.SaveChanges(It.Is<IgdVisitSmassTaskModel>(t =>
            t.TaskType == SmassTaskTypeEnum.Void
            && t.TaskStatus == SmassTaskStatusEnum.Failed)), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenSmassEnabledButNoGenerateTask_ReturnsSkipped()
    {
        _options.EnableSmassIntegration = true;
        var visit = CreateVisitWithTriages(1);

        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));
        _smassTaskRepo.Setup(r => r.FindByBusinessKey(visit.IgdVisitId, 1, SmassTaskTypeEnum.Generate))
            .Returns(MayBe<IgdVisitSmassTaskModel>.None);

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 1, UserId, "Salah input");
        var sut = CreateSut();

        var result = await sut.Handle(cmd, CancellationToken.None);

        result.IsVoided.Should().BeTrue();
        result.SmassVoidStatus.Should().Be("Skipped");
        _smassGateway.Verify(g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenTriageAlreadyVoided_ThrowsInvalidOperationAndDoesNotSaveOrCallSmass()
    {
        _options.EnableSmassIntegration = true;
        var visit = CreateVisitWithTriages(1);
        visit.VoidTriage(1, "Void pertama", new AuditInfoType(UserId, _now.AddMinutes(-5)));

        _visitRepo.Setup(r => r.LoadEntity(It.Is<IIgdVisitKey>(k => k.IgdVisitId == visit.IgdVisitId)))
            .Returns(MayBe.From(visit));

        var cmd = new IgdVisitVoidTriageCmd(visit.IgdVisitId, 1, UserId, "Void ulang");
        var sut = CreateSut();

        var act = () => sut.Handle(cmd, CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
        _visitRepo.Verify(r => r.SaveChanges(It.IsAny<IgdVisitModel>()), Times.Never);
        _smassGateway.Verify(g => g.DeleteAssesment(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
