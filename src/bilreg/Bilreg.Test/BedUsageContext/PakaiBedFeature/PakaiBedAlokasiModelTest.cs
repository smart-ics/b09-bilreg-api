using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using FluentAssertions;

namespace Bilreg.Test.BedUsageContext.PakaiBedFeature;

public class PakaiBedAlokasiModelTest
{
    private static readonly DateTime ProposedAt = new(2026, 7, 17, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime RecordedAt = new(2026, 7, 17, 8, 1, 0, DateTimeKind.Utc);
    private static readonly DateTime AssignedAt = new(2026, 7, 17, 8, 5, 0, DateTimeKind.Utc);

    [Fact]
    public void Propose_Clinical_CreatesProposedAggregateAndTransition()
    {
        var sut = ProposeClinical();

        sut.PakaiBedId.Should().StartWith("PKB");
        sut.Version.Should().Be(0);
        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Proposed);
        sut.IsClinicalPakaiBed.Should().BeFalse();
        sut.IsCompanionBed.Should().BeFalse();
        sut.StartedAt.Should().BeNull();
        sut.ListTransition.Should().ContainSingle()
            .Which.PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Proposed);
    }

    [Fact]
    public void Assign_Clinical_ActivatesClinicalPakaiBed()
    {
        var proposed = ProposeClinical();

        var sut = proposed.Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Bed assignment", "EVD-1");

        sut.PakaiBedId.Should().Be(proposed.PakaiBedId);
        sut.Version.Should().Be(1);
        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Active);
        sut.StartedAt.Should().Be(AssignedAt);
        sut.AssignedBy.Should().Be("PEG-2");
        sut.BedAssignabilityEvidenceId.Should().Be("EVD-1");
        sut.IsClinicalPakaiBed.Should().BeTrue();
        sut.ListTransition.Should().HaveCount(2);
        sut.ListTransition.Last().PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Assigned);
    }

    [Fact]
    public void Assign_Companion_ActivatesWithoutClinicalDesignation()
    {
        var proposed = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-2", string.Empty, "REQ-2",
            PakaiBedPurposeEnum.Companion, OccupantRoleEnum.Companion,
            "PEG-1", ProposedAt, RecordedAt, "Companion proposal");

        var sut = proposed.Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Companion assignment", "EVD-2");

        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Active);
        sut.IsCompanionBed.Should().BeTrue();
        sut.IsClinicalPakaiBed.Should().BeFalse();
        sut.WaitingListId.Should().BeEmpty();
    }

    [Theory]
    [InlineData(PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Companion)]
    [InlineData(PakaiBedPurposeEnum.Companion, OccupantRoleEnum.Primary)]
    [InlineData(PakaiBedPurposeEnum.Retained, OccupantRoleEnum.Primary)]
    [InlineData(PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Associated)]
    public void Propose_UnsupportedP3PurposeRole_Throws(
        PakaiBedPurposeEnum purpose,
        OccupantRoleEnum role)
    {
        var act = () => PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            purpose, role, "PEG-1", ProposedAt, RecordedAt, "Proposal");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Propose_ClinicalWithoutWaitingList_Throws()
    {
        var act = () => PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", string.Empty, "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "PEG-1", ProposedAt, RecordedAt, "Proposal");

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Propose_CompanionWithWaitingList_Throws()
    {
        var act = () => PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Companion, OccupantRoleEnum.Companion,
            "PEG-1", ProposedAt, RecordedAt, "Proposal");

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Propose_WithNonUtcTime_Throws()
    {
        var localTime = DateTime.SpecifyKind(ProposedAt, DateTimeKind.Local);
        var act = () => PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "PEG-1", localTime, RecordedAt, "Proposal");

        act.Should().Throw<ArgumentException>().WithMessage("*DateTimeKind.Utc*");
    }

    [Fact]
    public void Assign_BeforeProposal_Throws()
    {
        var sut = ProposeClinical();
        var act = () => sut.Assign(
            "PEG-2", ProposedAt.AddMinutes(-1), RecordedAt, "Assignment", "EVD-1");

        act.Should().Throw<ArgumentException>().WithMessage("*tidak boleh lebih awal*");
    }

    [Fact]
    public void Assign_Twice_Throws()
    {
        var assigned = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assignment", "EVD-1");

        var act = () => assigned.Assign(
            "PEG-3", AssignedAt.AddMinutes(2), AssignedAt.AddMinutes(3), "Again", "EVD-2");

        act.Should().Throw<InvalidOperationException>().WithMessage("*harus Proposed*");
    }

    [Fact]
    public void Assign_DoesNotMutateOriginalAggregateOrCollections()
    {
        var proposed = ProposeClinical();
        var assigned = proposed.Assign(
            "PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assignment", "EVD-1");

        proposed.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Proposed);
        proposed.ListTransition.Should().ContainSingle();
        assigned.ListTransition.Should().HaveCount(2);
        var collection = proposed.ListTransition.Should().BeAssignableTo<ICollection<PakaiBedTransisiModel>>().Subject;
        var act = () => collection.Add(PakaiBedTransisiModel.Default);
        act.Should().Throw<NotSupportedException>();
    }

    [Fact]
    public void Key_ReturnsLightweightKey()
    {
        var sut = PakaiBedAlokasiModel.Key("PKB-1");

        sut.PakaiBedId.Should().Be("PKB-1");
    }

    [Fact]
    public void Correction_Create_PreservesOriginalFactAndWardReference()
    {
        var sut = PakaiBedKoreksiModel.Create(
            "PKB-1",
            "ATR-1",
            PakaiBedKoreksiEnum.Corrected,
            "BGS-1",
            "Incorrect assignment time",
            "PEG-HEAD",
            AssignedAt,
            AssignedAt.AddMinutes(1),
            "REV-1");

        sut.CorrectionId.Should().StartWith("ACR");
        sut.OriginalTransitionId.Should().Be("ATR-1");
        sut.OriginalBangsalId.Should().Be("BGS-1");
        sut.Reason.Should().Be("Incorrect assignment time");
        sut.ReviewReference.Should().Be("REV-1");
    }

    [Fact]
    public void Correction_WithRecordedTimeBeforeOccurredTime_Throws()
    {
        var act = () => PakaiBedKoreksiModel.Create(
            "PKB-1", "ATR-1", PakaiBedKoreksiEnum.Corrected, "BGS-1",
            "Reason", "PEG-HEAD", AssignedAt, AssignedAt.AddMinutes(-1), string.Empty);

        act.Should().Throw<ArgumentException>().WithMessage("*tidak boleh lebih awal*");
    }

    private static PakaiBedAlokasiModel ProposeClinical() =>
        PakaiBedAlokasiModel.Propose(
            "REG-1",
            "P-1",
            "BGS-1",
            "KMR-1",
            "BED-1",
            "WTL-1",
            "REQ-1",
            PakaiBedPurposeEnum.Clinical,
            OccupantRoleEnum.Primary,
            "PEG-1",
            ProposedAt,
            RecordedAt,
            "Clinical proposal");
}
