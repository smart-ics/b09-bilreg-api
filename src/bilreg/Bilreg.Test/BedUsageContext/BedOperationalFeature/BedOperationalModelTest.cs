using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.BedOperationalFeature;

public class BedOperationalModelTest
{
    private static readonly DateTime OccurredAt =
        new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RecordedAt = OccurredAt.AddMinutes(1);

    [Fact]
    public void Create_HasUnverifiedReadinessAndEmptyHistory()
    {
        var sut = CreateSut();

        sut.BedId.Should().Be("BED-1");
        sut.Version.Should().Be(0);
        sut.OccupancyEpoch.Should().Be(0);
        sut.CurrentReadiness.Should().BeNull();
        sut.LatestReadinessTransactionId.Should().BeEmpty();
        sut.IsReadinessVerified.Should().BeFalse();
        sut.HasUnresolvedBlocker.Should().BeFalse();
        sut.ListReadinessTransaction.Should().BeEmpty();
        sut.ListReadinessCorrection.Should().BeEmpty();
    }

    [Fact]
    public void RecordReadiness_CleaningRequired_ProjectsBlocker()
    {
        var sut = RecordCleaning(CreateSut());

        sut.Version.Should().Be(1);
        sut.CurrentReadiness.Should().Be(BedReadinessStatusEnum.CleaningRequired);
        sut.HasUnresolvedBlocker.Should().BeTrue();
        sut.CurrentBlockerState.RestrictionType
            .Should().Be(BedRestrictionTypeEnum.Cleaning);
        sut.CurrentBlockerState.OriginatingTransactionId
            .Should().Be(sut.LatestReadinessTransactionId);
        sut.ListReadinessTransaction.Should().ContainSingle();
    }

    [Fact]
    public void VerifyReady_AppendsVerifiedFactAndClearsBlocker()
    {
        var blocked = RecordCleaning(CreateSut());

        var sut = VerifyReady(blocked, OccurredAt.AddHours(1), "REQ-2");

        sut.Version.Should().Be(2);
        sut.CurrentReadiness.Should().Be(BedReadinessStatusEnum.Ready);
        sut.IsReadinessVerified.Should().BeTrue();
        sut.HasUnresolvedBlocker.Should().BeFalse();
        sut.CurrentBlockerState.Should().Be(BedBlockerStateType.None);
        sut.ListReadinessTransaction.Should().HaveCount(2);
        sut.ListReadinessTransaction.Last().VerifiedByActorId.Should().Be("PEG-VERIFIER");
    }

    [Fact]
    public void RecordReadiness_Ready_Throws()
    {
        var act = () => CreateSut().RecordReadiness(
            BedReadinessStatusEnum.Ready,
            BedRestrictionTypeEnum.None,
            "Ready",
            string.Empty,
            "PEG-1",
            OccurredAt,
            RecordedAt,
            string.Empty,
            "REQ-1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*verifikasi*");
    }

    [Fact]
    public void VerifyReady_WithoutVerifier_Throws()
    {
        var act = () => CreateSut().VerifyReady(
            "Ready",
            string.Empty,
            "PEG-1",
            string.Empty,
            OccurredAt,
            RecordedAt,
            string.Empty,
            "REQ-1");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void RecordReadiness_WithNonUtcTime_Throws()
    {
        var localTime = DateTime.SpecifyKind(OccurredAt, DateTimeKind.Local);

        var act = () => CreateSut().RecordReadiness(
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety issue",
            string.Empty,
            "PEG-1",
            localTime,
            RecordedAt,
            string.Empty,
            "REQ-1");

        act.Should().Throw<ArgumentException>().WithMessage("*DateTimeKind.Utc*");
    }

    [Fact]
    public void AssertReadyForAllocation_WithVerifiedActiveBed_Succeeds()
    {
        var sut = VerifyReady(CreateSut(), OccurredAt, "REQ-1");

        var act = () => sut.AssertReadyForAllocation(ActiveMaster());

        act.Should().NotThrow();
    }

    [Fact]
    public void AssertReadyForAllocation_WithInactiveMaster_Throws()
    {
        var sut = VerifyReady(CreateSut(), OccurredAt, "REQ-1");
        var inactiveMaster = ActiveMaster() with { IsActive = false };

        var act = () => sut.AssertReadyForAllocation(inactiveMaster);

        act.Should().Throw<InvalidOperationException>().WithMessage("*tidak aktif*");
    }

    [Fact]
    public void AssertReadyForAllocation_WithUnverifiedReadiness_Throws()
    {
        var act = () => CreateSut().AssertReadyForAllocation(ActiveMaster());

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*belum terverifikasi Ready*");
    }

    [Fact]
    public void AssertReadyForAllocation_WithBlockedBed_Throws()
    {
        var sut = RecordCleaning(CreateSut());

        var act = () => sut.AssertReadyForAllocation(ActiveMaster());

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void AdvanceOccupancyEpoch_IncrementsEpochAndVersion()
    {
        var original = CreateSut();

        var sut = original.AdvanceOccupancyEpoch();

        sut.OccupancyEpoch.Should().Be(1);
        sut.Version.Should().Be(1);
        original.OccupancyEpoch.Should().Be(0);
        original.Version.Should().Be(0);
    }

    [Fact]
    public void CorrectReadiness_AppendsReplacementAndPreservesOriginal()
    {
        var ready = VerifyReady(CreateSut(), OccurredAt, "REQ-1");
        var original = ready.ListReadinessTransaction.Single();

        var sut = ready.CorrectReadiness(
            original.TransactionId,
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety blocker remains",
            "Ready fact was recorded incorrectly",
            "EVIDENCE-2",
            "PEG-HEAD-NURSE",
            string.Empty,
            OccurredAt.AddHours(1),
            OccurredAt.AddHours(1).AddMinutes(1),
            "REQ-2");

        sut.Version.Should().Be(2);
        sut.CurrentReadiness.Should().Be(BedReadinessStatusEnum.Blocked);
        sut.HasUnresolvedBlocker.Should().BeTrue();
        sut.ListReadinessTransaction.Should().HaveCount(2);
        sut.ListReadinessTransaction.Should().Contain(original);
        sut.ListReadinessCorrection.Should().ContainSingle()
            .Which.OriginalTransactionId.Should().Be(original.TransactionId);
        ready.CurrentReadiness.Should().Be(BedReadinessStatusEnum.Ready);
        ready.ListReadinessTransaction.Should().ContainSingle();
        ready.ListReadinessCorrection.Should().BeEmpty();
    }

    [Fact]
    public void CorrectReadiness_SameOriginalTwice_Throws()
    {
        var ready = VerifyReady(CreateSut(), OccurredAt, "REQ-1");
        var originalId = ready.ListReadinessTransaction.Single().TransactionId;
        var corrected = ready.CorrectReadiness(
            originalId,
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety blocker remains",
            "Ready fact was recorded incorrectly",
            string.Empty,
            "PEG-HEAD-NURSE",
            string.Empty,
            OccurredAt.AddHours(1),
            OccurredAt.AddHours(1).AddMinutes(1),
            "REQ-2");

        var act = () => corrected.CorrectReadiness(
            originalId,
            BedReadinessStatusEnum.OutOfService,
            BedRestrictionTypeEnum.Maintenance,
            "Maintenance required",
            "Replacement readiness also incorrect",
            string.Empty,
            "PEG-HEAD-NURSE",
            string.Empty,
            OccurredAt.AddHours(2),
            OccurredAt.AddHours(2).AddMinutes(1),
            "REQ-3");

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dikoreksi*");
    }

    [Fact]
    public void Behaviour_DoesNotMutateOriginalOrExposeMutableCollection()
    {
        var original = CreateSut();
        var changed = RecordCleaning(original);

        original.ListReadinessTransaction.Should().BeEmpty();
        changed.ListReadinessTransaction.Should().ContainSingle();
        var collection = changed.ListReadinessTransaction
            .Should().BeAssignableTo<ICollection<BedReadinessTransactionModel>>()
            .Subject;

        var act = () => collection.Add(BedReadinessTransactionModel.Default);

        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Key_ReturnsLightweightBedIdentity()
    {
        var key = BedOperationalModel.Key("BED-9");

        key.BedId.Should().Be("BED-9");
    }

    [Fact]
    public void RecordReadiness_DuplicateSourceFact_Throws()
    {
        var first = CreateSut().RecordReadiness(
            BedReadinessStatusEnum.Blocked,
            BedRestrictionTypeEnum.Safety,
            "Safety issue",
            string.Empty,
            "PEG-1",
            OccurredAt,
            RecordedAt,
            "SOURCE-1",
            "REQ-1");

        var act = () => first.RecordReadiness(
            BedReadinessStatusEnum.OutOfService,
            BedRestrictionTypeEnum.Maintenance,
            "Maintenance issue",
            string.Empty,
            "PEG-2",
            OccurredAt.AddMinutes(2),
            RecordedAt.AddMinutes(2),
            "SOURCE-1",
            "REQ-2");

        act.Should().Throw<InvalidOperationException>().WithMessage("*sudah dicatat*");
    }

    private static BedOperationalModel CreateSut() =>
        BedOperationalModel.Create(
            "BED-1",
            "BGS-1",
            "KMR-1",
            new OccupancyPolicyReff("POL-1", "Standard inpatient"));

    private static BedMasterSnapshotType ActiveMaster() =>
        new("BED-1", "BGS-1", "KMR-1", true);

    private static BedOperationalModel RecordCleaning(BedOperationalModel source) =>
        source.RecordReadiness(
            BedReadinessStatusEnum.CleaningRequired,
            BedRestrictionTypeEnum.None,
            "Post-use cleaning required",
            string.Empty,
            "PEG-1",
            OccurredAt,
            RecordedAt,
            string.Empty,
            "REQ-1");

    private static BedOperationalModel VerifyReady(
        BedOperationalModel source,
        DateTime occurredAt,
        string requestId) =>
        source.VerifyReady(
            "Recovery verified",
            "EVIDENCE-1",
            "PEG-1",
            "PEG-VERIFIER",
            occurredAt,
            occurredAt.AddMinutes(1),
            string.Empty,
            requestId);
}
