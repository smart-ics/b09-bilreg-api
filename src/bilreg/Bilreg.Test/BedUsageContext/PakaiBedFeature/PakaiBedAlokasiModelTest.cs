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
    [InlineData(PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Primary)]
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

    [Fact]
    public void Propose_WithDualClassAndClassificationFlags_PopulatesProperties()
    {
        var sut = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "PEG-1", ProposedAt, RecordedAt, "Titip Kelas proposal",
            physicalKelasId: "K2", billingKelasId: "K1", tipeKamarId: "B", tarif: 350000m,
            isTitipKelas: true, isNaikKelas: false);

        sut.PhysicalKelasId.Should().Be("K2");
        sut.BillingKelasId.Should().Be("K1");
        sut.TipeKamarId.Should().Be("B");
        sut.Tarif.Should().Be(350000m);
        sut.IsTitipKelas.Should().BeTrue();
        sut.IsNaikKelas.Should().BeFalse();
    }

    [Fact]
    public void Assign_PreservesDualClassAndRoomingInProperties()
    {
        var proposed = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "PEG-1", ProposedAt, RecordedAt, "Proposal",
            physicalKelasId: "VIP", billingKelasId: "VVIP", tipeKamarId: "A", tarif: 1200000m,
            motherPakaiBedId: "PKB-PARENT", isTitipKelas: false, isNaikKelas: true);

        var active = proposed.Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");

        active.PhysicalKelasId.Should().Be("VIP");
        active.BillingKelasId.Should().Be("VVIP");
        active.TipeKamarId.Should().Be("A");
        active.Tarif.Should().Be(1200000m);
        active.MotherPakaiBedId.Should().Be("PKB-PARENT");
        active.IsTitipKelas.Should().BeFalse();
        active.IsNaikKelas.Should().BeTrue();
    }

    [Fact]
    public void Propose_RoomingIn_WithAssociatedAndMotherPakaiBedId_CreatesProposedAggregate()
    {
        var sut = PakaiBedAlokasiModel.Propose(
            "REG-BABY", "P-BABY", "BGS-1", "KMR-1", "BED-1", string.Empty, "REQ-BABY",
            PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Associated,
            "PEG-1", ProposedAt, RecordedAt, "Rooming-in proposal",
            physicalKelasId: "K1", billingKelasId: "K1", tipeKamarId: "A", tarif: 0m,
            motherPakaiBedId: "PKB-MOM");

        sut.PakaiBedPurpose.Should().Be(PakaiBedPurposeEnum.RoomingIn);
        sut.OccupantRole.Should().Be(OccupantRoleEnum.Associated);
        sut.MotherPakaiBedId.Should().Be("PKB-MOM");
        sut.IsRoomingIn.Should().BeTrue();
        sut.Tarif.Should().Be(0m);
    }

    [Fact]
    public void Propose_RoomingIn_WithCompanionAndMotherPakaiBedId_CreatesProposedAggregate()
    {
        var sut = PakaiBedAlokasiModel.Propose(
            "REG-BABY", "P-BABY", "BGS-1", "KMR-1", "BED-1", string.Empty, "REQ-BABY",
            PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Companion,
            "PEG-1", ProposedAt, RecordedAt, "Rooming-in companion proposal",
            physicalKelasId: "K1", billingKelasId: "K1", tipeKamarId: "A", tarif: 0m,
            motherPakaiBedId: "PKB-MOM");

        sut.PakaiBedPurpose.Should().Be(PakaiBedPurposeEnum.RoomingIn);
        sut.OccupantRole.Should().Be(OccupantRoleEnum.Companion);
        sut.MotherPakaiBedId.Should().Be("PKB-MOM");
        sut.IsRoomingIn.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Propose_RoomingIn_WithoutMotherPakaiBedId_ThrowsInvalidOperationException(string motherId)
    {
        var act = () => PakaiBedAlokasiModel.Propose(
            "REG-BABY", "P-BABY", "BGS-1", "KMR-1", "BED-1", string.Empty, "REQ-BABY",
            PakaiBedPurposeEnum.RoomingIn, OccupantRoleEnum.Associated,
            "PEG-1", ProposedAt, RecordedAt, "Proposal",
            motherPakaiBedId: motherId);

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*RoomingIn*MotherPakaiBedId*");
    }

    [Fact]
    public void Release_ActiveBed_TransitionsToReleasedAndRecordsTransition()
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");
        var releasedAt = AssignedAt.AddDays(2);

        var sut = active.Release(releasedAt, "Pasien KRS sembuh", "PEG-NURSE");

        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Released);
        sut.Version.Should().Be(active.Version + 1);
        sut.IsClinicalPakaiBed.Should().BeFalse();
        sut.ListTransition.Should().HaveCount(3);
        var transition = sut.ListTransition.Last();
        transition.PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Released);
        transition.ActorId.Should().Be("PEG-NURSE");
        transition.Reason.Should().Be("Pasien KRS sembuh");
        transition.OccurredAt.Should().Be(releasedAt);
    }

    [Fact]
    public void Release_PreservesAllAggregateProperties()
    {
        var active = PakaiBedAlokasiModel.Propose(
            "REG-1", "P-1", "BGS-1", "KMR-1", "BED-1", "WTL-1", "REQ-1",
            PakaiBedPurposeEnum.Clinical, OccupantRoleEnum.Primary,
            "PEG-1", ProposedAt, RecordedAt, "Proposal",
            physicalKelasId: "K1", billingKelasId: "VIP", tipeKamarId: "A", tarif: 750000m,
            motherPakaiBedId: "PKB-MOM", isTitipKelas: true, isNaikKelas: false)
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");

        var released = active.Release(AssignedAt.AddDays(1), "Checkout", "PEG-NURSE");

        released.PhysicalKelasId.Should().Be("K1");
        released.BillingKelasId.Should().Be("VIP");
        released.TipeKamarId.Should().Be("A");
        released.Tarif.Should().Be(750000m);
        released.MotherPakaiBedId.Should().Be("PKB-MOM");
        released.IsTitipKelas.Should().BeTrue();
        released.IsNaikKelas.Should().BeFalse();
        released.RegId.Should().Be("REG-1");
        released.PasienId.Should().Be("P-1");
        released.BedId.Should().Be("BED-1");
    }

    [Fact]
    public void Release_WhenProposed_ThrowsInvalidOperationException()
    {
        var proposed = ProposeClinical();
        var act = () => proposed.Release(AssignedAt, "Checkout", "PEG-1");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Active untuk dilepas*");
    }

    [Fact]
    public void Release_WhenAlreadyReleased_ThrowsInvalidOperationException()
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");
        var released = active.Release(AssignedAt.AddDays(1), "Checkout", "PEG-NURSE");

        var act = () => released.Release(AssignedAt.AddDays(2), "Checkout again", "PEG-NURSE");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*harus Active untuk dilepas*");
    }

    [Fact]
    public void Release_WithOccurredAtEarlierThanStartedAt_ThrowsArgumentException()
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");

        var act = () => active.Release(AssignedAt.AddMinutes(-1), "Checkout", "PEG-NURSE");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh lebih awal dari waktu mulai*");
    }

    [Fact]
    public void Release_WithNonUtcOccurredAt_ThrowsArgumentException()
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");
        var localTime = DateTime.SpecifyKind(AssignedAt.AddDays(1), DateTimeKind.Local);

        var act = () => active.Release(localTime, "Checkout", "PEG-NURSE");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DateTimeKind.Utc*");
    }

    [Theory]
    [InlineData("", "Valid Reason")]
    [InlineData("   ", "Valid Reason")]
    [InlineData("PEG-1", "")]
    [InlineData("PEG-1", "   ")]
    public void Release_WithNullOrWhitespaceArguments_Throws(string actorId, string reason)
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");

        var act = () => active.Release(AssignedAt.AddDays(1), reason, actorId);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Void_ActiveBed_TransitionsToCancelledAndRecordsTransition()
    {
        var active = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1");
        var voidAt = AssignedAt.AddHours(2);

        var sut = active.Void(voidAt, "Salah kamar perawatan", "PEG-ADMIN");

        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Cancelled);
        sut.Version.Should().Be(active.Version + 1);
        sut.IsClinicalPakaiBed.Should().BeFalse();
        sut.ListTransition.Should().HaveCount(3);
        var transition = sut.ListTransition.Last();
        transition.PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Cancelled);
        transition.ActorId.Should().Be("PEG-ADMIN");
        transition.Reason.Should().Be("Salah kamar perawatan");
        transition.OccurredAt.Should().Be(voidAt);
    }

    [Fact]
    public void Void_ProposedBed_TransitionsToCancelled()
    {
        var proposed = ProposeClinical();
        var voidAt = ProposedAt.AddMinutes(30);

        var sut = proposed.Void(voidAt, "Pasien menolak rawat inap", "PEG-ADMIN");

        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Cancelled);
        sut.Version.Should().Be(1);
        sut.ListTransition.Should().HaveCount(2);
        sut.ListTransition.Last().PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Cancelled);
    }

    [Fact]
    public void Void_ReleasedBed_TransitionsToCancelled()
    {
        var released = ProposeClinical()
            .Assign("PEG-2", AssignedAt, AssignedAt.AddMinutes(1), "Assign", "EVD-1")
            .Release(AssignedAt.AddDays(1), "Checkout", "PEG-NURSE");

        var voidAt = AssignedAt.AddDays(1).AddMinutes(10);
        var sut = released.Void(voidAt, "Release dibatalkan karena salah pasien", "PEG-ADMIN");

        sut.PakaiBedStatus.Should().Be(PakaiBedStatusEnum.Cancelled);
        sut.ListTransition.Last().PakaiBedTransition.Should().Be(PakaiBedTransisiEnum.Cancelled);
    }

    [Fact]
    public void Void_WhenAlreadyCancelled_ThrowsInvalidOperationException()
    {
        var proposed = ProposeClinical();
        var voided = proposed.Void(ProposedAt.AddMinutes(10), "Batal", "PEG-ADMIN");

        var act = () => voided.Void(ProposedAt.AddMinutes(20), "Batal lagi", "PEG-ADMIN");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*sudah dibatalkan*");
    }

    [Fact]
    public void Void_WithVoidAtEarlierThanProposedAt_ThrowsArgumentException()
    {
        var proposed = ProposeClinical();
        var act = () => proposed.Void(ProposedAt.AddMinutes(-5), "Batal", "PEG-ADMIN");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*tidak boleh lebih awal dari waktu proposal*");
    }

    [Fact]
    public void Void_WithNonUtcTime_ThrowsArgumentException()
    {
        var proposed = ProposeClinical();
        var localTime = DateTime.SpecifyKind(ProposedAt.AddMinutes(10), DateTimeKind.Local);

        var act = () => proposed.Void(localTime, "Batal", "PEG-ADMIN");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*DateTimeKind.Utc*");
    }

    [Theory]
    [InlineData("", "Valid Reason")]
    [InlineData("   ", "Valid Reason")]
    [InlineData("PEG-1", "")]
    [InlineData("PEG-1", "   ")]
    public void Void_WithNullOrWhitespaceArguments_Throws(string actorId, string reason)
    {
        var proposed = ProposeClinical();

        var act = () => proposed.Void(ProposedAt.AddMinutes(10), reason, actorId);

        act.Should().Throw<ArgumentException>();
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
