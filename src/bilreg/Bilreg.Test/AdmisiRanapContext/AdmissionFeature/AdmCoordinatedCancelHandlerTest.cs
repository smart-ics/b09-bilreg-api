using System.Text.Json;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using FluentAssertions;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmCoordinatedCancelHandlerTest
{
    [Fact]
    public async Task OpnameCancellation_CancelsEveryRequiredState_AndWritesCorrelatedAudits()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>();
        var eligibility = new Mock<IRegistrationCancellationEligibilityRepo>();
        var audit = new Mock<IAuditRepo>();
        var state = State(sourceId: "OPN00000001", sourceKind: "OpnameRequestModel");
        repo.Setup(x => x.LockLedger("REQ1")).Returns((CoordinatedCancellationLedger?)null);
        repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        repo.Setup(x => x.LockState("RG00000001")).Returns(state);
        repo.Setup(x => x.CancelWaitingList(state, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.EndDoctorAssignments("RG00000001", It.IsAny<DateOnly>())).Returns(2);
        repo.Setup(x => x.VoidRegistration("RG00000001", It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.DeleteRegAktif("RG00000001")).Returns(1);
        repo.Setup(x => x.RestoreSource(state, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.CancelAdmission(state, It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.CompleteLedger("REQ1", It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        eligibility.Setup(x => x.HasBillingItems("RG00000001")).Returns(false);
        var sut = new AdmCoordinatedCancelHandler(repo.Object, eligibility.Object, audit.Object);

        var result = await sut.Handle(Command(), CancellationToken.None);

        result.AdmissionStatus.Should().Be(AdmissionStatusEnum.Cancelled);
        result.RestoredSource.Should().Be("OPN00000001");
        audit.Verify(x => x.SaveChanges(It.IsAny<Bilreg.Domain.Shared.AuditLogFeature.AuditLog>()), Times.Exactly(6));
        repo.Verify(x => x.DeleteRegAktif("RG00000001"), Times.Once);
        repo.Verify(x => x.CancelAdmission(state, It.IsAny<DateTime?>(), "user1", It.IsAny<DateTime>()), Times.Once);
    }

    [Fact]
    public async Task CancellationAudits_UseTheEntitySpecificPreChangeSnapshots()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>();
        var eligibility = new Mock<IRegistrationCancellationEligibilityRepo>();
        var audit = new Mock<IAuditRepo>();
        var state = State(sourceId: "OPN00000001", sourceKind: "OpnameRequestModel") with
        {
            AuditSnapshots = new CoordinatedCancellationAuditSnapshots(
                new { marker = "admission-before" }, new { marker = "registration-before" },
                new { marker = "doctor-history-before" }, new { marker = "regaktif-before" },
                new { marker = "waiting-before" }, new { marker = "source-before" })
        };
        repo.Setup(x => x.LockLedger("REQ1")).Returns((CoordinatedCancellationLedger?)null);
        repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        repo.Setup(x => x.LockState("RG00000001")).Returns(state);
        repo.Setup(x => x.CancelWaitingList(state, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.EndDoctorAssignments("RG00000001", It.IsAny<DateOnly>())).Returns(1);
        repo.Setup(x => x.VoidRegistration("RG00000001", It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.DeleteRegAktif("RG00000001")).Returns(1);
        repo.Setup(x => x.RestoreSource(state, It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.CancelAdmission(state, It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        repo.Setup(x => x.CompleteLedger("REQ1", It.IsAny<string>(), It.IsAny<DateTime>())).Returns(1);
        eligibility.Setup(x => x.HasBillingItems("RG00000001")).Returns(false);

        await new AdmCoordinatedCancelHandler(repo.Object, eligibility.Object, audit.Object).Handle(Command(), CancellationToken.None);

        var saved = audit.Invocations.Where(x => x.Method.Name == nameof(IAuditRepo.SaveChanges))
            .Select(x => (Bilreg.Domain.Shared.AuditLogFeature.AuditLog)x.Arguments[0]).ToList();
        saved.Single(x => x.EntityName == "RegInapModel").OriginalDataJson.Should().Contain("doctor-history-before");
        saved.Single(x => x.EntityName == "RegAktifModel").OriginalDataJson.Should().Contain("regaktif-before");
        saved.Single(x => x.EntityName == "AdmissionModel").OriginalDataJson.Should().Contain("admission-before");
        saved.Single(x => x.EntityName == "RegInapModel").OriginalDataJson.Should().NotContain("regaktif-before");
    }

    [Fact]
    public async Task BillingItems_RejectsBeforeAnyMutation()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>(); var eligibility = new Mock<IRegistrationCancellationEligibilityRepo>();
        repo.Setup(x => x.LockLedger("REQ1")).Returns((CoordinatedCancellationLedger?)null);
        repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        repo.Setup(x => x.LockState("RG00000001")).Returns(State());
        eligibility.Setup(x => x.HasBillingItems("RG00000001")).Returns(true);
        var sut = new AdmCoordinatedCancelHandler(repo.Object, eligibility.Object, Mock.Of<IAuditRepo>());

        var act = () => sut.Handle(Command(), CancellationToken.None);

        var ex = await act.Should().ThrowAsync<CoordinatedCancellationException>();
        ex.Which.Code.Should().Be(CoordinatedCancellationErrorCode.RegistrationHasBillingItems);
        repo.Verify(x => x.CancelAdmission(It.IsAny<CoordinatedCancellationState>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task SameCompletedRequest_ReplaysStoredResponseWithoutMutations()
    {
        var response = new AdmCoordinatedCancelResponse("RG00000001", AdmissionStatusEnum.Cancelled, true, false, null, null, "REQ1");
        var ledger = new CoordinatedCancellationLedger("REQ1", FingerprintFor(Command()), "RG00000001", "Completed", JsonSerializer.Serialize(response), DateTime.UtcNow, DateTime.UtcNow, "REQ1");
        var repo = new Mock<ICoordinatedCancellationRepo>(); repo.Setup(x => x.LockLedger("REQ1")).Returns(ledger);
        var sut = new AdmCoordinatedCancelHandler(repo.Object, Mock.Of<IRegistrationCancellationEligibilityRepo>(), Mock.Of<IAuditRepo>());

        var replay = await sut.Handle(Command(), CancellationToken.None);

        replay.AlreadyCancelled.Should().BeTrue();
        repo.Verify(x => x.LockState(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task ReusedRequestId_WithDifferentIntent_ReturnsFrozenConflictCode()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>();
        repo.Setup(x => x.LockLedger("REQ1")).Returns(new CoordinatedCancellationLedger("REQ1", "OTHER", "RG00000001", "Completed", "{}", DateTime.UtcNow, DateTime.UtcNow, "REQ1"));
        var sut = new AdmCoordinatedCancelHandler(repo.Object, Mock.Of<IRegistrationCancellationEligibilityRepo>(), Mock.Of<IAuditRepo>());
        var act = () => sut.Handle(Command(), CancellationToken.None);
        (await act.Should().ThrowAsync<CoordinatedCancellationException>()).Which.Code.Should().Be(CoordinatedCancellationErrorCode.RequestIdReused);
    }

    [Fact]
    public async Task SourceMismatch_ReturnsFrozenConflictCodeBeforeMutation()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>();
        repo.Setup(x => x.LockLedger("REQ1")).Returns((CoordinatedCancellationLedger?)null);
        repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        repo.Setup(x => x.LockState("RG00000001")).Returns(State("OPN00000001", "OpnameRequestModel") with { SourceOwnedAndCancellable = false });
        var sut = new AdmCoordinatedCancelHandler(repo.Object, Mock.Of<IRegistrationCancellationEligibilityRepo>(), Mock.Of<IAuditRepo>());
        var act = () => sut.Handle(Command(), CancellationToken.None);
        (await act.Should().ThrowAsync<CoordinatedCancellationException>()).Which.Code.Should().Be(CoordinatedCancellationErrorCode.SourceStateMismatch);
        repo.Verify(x => x.CancelAdmission(It.IsAny<CoordinatedCancellationState>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    [Fact]
    public async Task MissingAdmission_ReturnsNotFoundBeforeEligibilityOrMutation()
    {
        var repo = new Mock<ICoordinatedCancellationRepo>();
        var eligibility = new Mock<IRegistrationCancellationEligibilityRepo>();
        repo.Setup(x => x.LockLedger("REQ1")).Returns((CoordinatedCancellationLedger?)null);
        repo.Setup(x => x.TryStartLedger(It.IsAny<CoordinatedCancellationLedger>())).Returns(true);
        repo.Setup(x => x.LockState("RG00000001")).Returns(State() with { AdmissionExists = false });
        var sut = new AdmCoordinatedCancelHandler(repo.Object, eligibility.Object, Mock.Of<IAuditRepo>());

        var act = () => sut.Handle(Command(), CancellationToken.None);

        await act.Should().ThrowAsync<KeyNotFoundException>();
        eligibility.Verify(x => x.HasBillingItems(It.IsAny<string>()), Times.Never);
        repo.Verify(x => x.CancelAdmission(It.IsAny<CoordinatedCancellationState>(), It.IsAny<DateTime?>(), It.IsAny<string>(), It.IsAny<DateTime>()), Times.Never);
    }

    private static AdmCoordinatedCancelCmd Command() => new("RG00000001", " alasan ", "user1", AdmissionStatusEnum.Admitted, null, "REQ1", null, null);
    private static CoordinatedCancellationState State(string? sourceId = null, string? sourceKind = null) => new("RG00000001", AdmissionStatusEnum.Admitted, new DateTime(3000, 1, 1), AdmissionSourceEnum.Legacy, "-", "-", true, true, 1, "WTL00000001", Bilreg.Domain.AdmisiRanapContext.WaitingListFeature.WaitingListStatusEnum.Waiting, 1, sourceId, sourceKind, true, false, new { RegId = "RG00000001" });
    private static string FingerprintFor(AdmCoordinatedCancelCmd cmd)
    {
        // The handler's public replay contract is exercised with a matching deterministic hash.
        using var sha = System.Security.Cryptography.SHA256.Create();
        var value = string.Join("|", cmd.RegId.Trim(), cmd.Reason.Trim(), cmd.UserId.Trim(), (int)cmd.ExpectedAdmissionStatus, "");
        return Convert.ToHexString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(value)));
    }
}
