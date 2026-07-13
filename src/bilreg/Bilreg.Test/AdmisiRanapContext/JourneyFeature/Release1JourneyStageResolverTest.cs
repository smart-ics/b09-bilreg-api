using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class Release1JourneyStageResolverTest
{
    private static readonly DateTime T0 = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    private static JourneyPatientSummary Patient(string id = "P001") =>
        new(id, "RM001", "Pasien Uji", "L", new DateOnly(1990, 1, 1));

    private static OpnameRequestJourneyFact Opname(
        string id,
        OpnameRequestStatusEnum status,
        string fulfilledRegId = "-") =>
        new(id, status, fulfilledRegId, T0);

    private static ReservationJourneyFact Reservation(
        string id,
        ReservationStatusEnum status,
        string realizedRegId = "-") =>
        new(id, status, realizedRegId, T0);

    private static AdmissionJourneyFact Admission(
        string regId,
        AdmissionStatusEnum status,
        string opnameRequestId = "-",
        string reservationId = "-") =>
        new(regId, status, opnameRequestId, reservationId, T1, "B01", "Bangsal Melati");

    private static WaitingListJourneyFact WaitingList(
        string id,
        string regId,
        WaitingListStatusEnum status,
        DateTime? at = null) =>
        new(id, regId, status, at ?? T2, "B01", "Bangsal Melati");

    private static JourneyNormalizedFacts Facts(
        JourneyOriginKind origin,
        OpnameRequestJourneyFact? opn = null,
        ReservationJourneyFact? rsv = null,
        AdmissionJourneyFact[]? admissions = null,
        WaitingListJourneyFact[]? waitingLists = null,
        bool orphanRegistration = false,
        string patientId = "P001") =>
        new(
            origin,
            Patient(patientId),
            opn,
            rsv,
            admissions ?? [],
            waitingLists ?? [],
            orphanRegistration);

    [Fact]
    public void RequestedOpnameRequest_ResolvesRegistrationRequired_WithStableOpnJourneyId()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Requested));

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Identity.JourneyId.Should().Be("opn:OPN1");
        result.Stage.Should().Be(JourneyOperationalStage.RegistrationRequired);
        result.NextTask!.Code.Should().Be(JourneyActionCode.CompleteRegistration);
        result.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Admisi);
        result.NextTask.CanExecute.Should().BeTrue();
        result.ReconciliationIssues.Should().BeEmpty();
        result.AllowedAdmisiActions.Should().Contain(a =>
            a.Code == JourneyActionCode.CancelOpnameRequest && a.CanExecute);
    }

    [Theory]
    [InlineData(ReservationStatusEnum.Reserved)]
    [InlineData(ReservationStatusEnum.Maintained)]
    public void ReservedOrMaintainedReservation_ResolvesRegistrationRequired(ReservationStatusEnum status)
    {
        var facts = Facts(
            JourneyOriginKind.Reservation,
            rsv: Reservation("RSV1", status));

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Identity.JourneyId.Should().Be("rsv:RSV1");
        result.Stage.Should().Be(JourneyOperationalStage.RegistrationRequired);
        result.NextTask!.CanExecute.Should().BeTrue();
        result.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Admisi);
    }

    [Fact]
    public void AdmissionWithNoWaitingList_ResolvesHandoverRequired()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Identity.JourneyId.Should().Be("reg:RG1");
        result.Stage.Should().Be(JourneyOperationalStage.HandoverRequired);
        result.NextTask!.Code.Should().Be(JourneyActionCode.CreateAccommodationHandover);
        result.NextTask.CanExecute.Should().BeTrue();
        result.AllowedAdmisiActions.Should().Contain(a =>
            a.Code == JourneyActionCode.CancelAdmission && a.CanExecute);
    }

    [Theory]
    [InlineData(AdmissionStatusEnum.Admitted)]
    [InlineData(AdmissionStatusEnum.Updated)]
    [InlineData(AdmissionStatusEnum.Waiting)]
    public void ActiveAdmissionStatuses_WithoutHandover_AreHandoverRequired(AdmissionStatusEnum status)
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", status)]);

        Release1JourneyStageResolver.Resolve(facts).Stage
            .Should().Be(JourneyOperationalStage.HandoverRequired);
    }

    [Fact]
    public void WaitingListWaiting_ResolvesWardAcceptanceRequired_NotExecutableFromAdmisi()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Waiting)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired);
        result.NextTask!.Code.Should().Be(JourneyActionCode.WardAcceptHandover);
        result.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Ward);
        result.NextTask.CanExecute.Should().BeFalse();
        result.NextTask.BlockedReason.Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public void WaitingListAccepted_ResolvesHandoverAccepted_NotBedAssigned()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Accepted)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.HandoverAccepted);
        result.NextTask!.Code.Should().Be(JourneyActionCode.AwaitWardPlacement);
        result.NextTask.CanExecute.Should().BeFalse();
        result.PlacementAvailability.Status.Should().Be(JourneyPlacementAvailabilityStatus.Unavailable);
        result.Stage.Should().NotBe(JourneyOperationalStage.InWard);
    }

    [Fact]
    public void WaitingListClosed_ResolvesPlacementStatusUnavailable_NeverInWardOrCompleted()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Closed)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.PlacementStatusUnavailable);
        result.Stage.Should().NotBe(JourneyOperationalStage.InWard);
        result.Stage.Should().NotBe(JourneyOperationalStage.Completed);
        result.NextTask!.CanExecute.Should().BeFalse();
        result.NextTask.Owner.Domain.Should().Be(JourneyOwnerDomain.Ward);
    }

    [Fact]
    public void WaitingListCancelled_WithoutPriorClosed_ReturnsHandoverRequired()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Cancelled)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.HandoverRequired);
        result.NextTask!.Code.Should().Be(JourneyActionCode.CreateAccommodationHandover);
        result.NextTask.CanExecute.Should().BeTrue();
        result.HandoverSummary.CancelledCount.Should().Be(1);
    }

    [Fact]
    public void WaitingListCancelled_AfterClosed_RemainsPlacementStatusUnavailable()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Updated)],
            waitingLists:
            [
                WaitingList("WL1", "RG1", WaitingListStatusEnum.Closed, T1),
                WaitingList("WL2", "RG1", WaitingListStatusEnum.Cancelled, T2)
            ]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.PlacementStatusUnavailable);
        result.Stage.Should().NotBe(JourneyOperationalStage.Completed);
        result.Stage.Should().NotBe(JourneyOperationalStage.InWard);
    }

    [Fact]
    public void AdmissionCancellation_ResolvesCancelled_ReadOnly()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Cancelled)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.Cancelled);
        result.Identity.IsTerminal.Should().BeTrue();
        result.NextTask.Should().BeNull();
        result.AllowedAdmisiActions.Should().BeEmpty();
    }

    [Fact]
    public void CancelledUnregisteredOpname_ResolvesCancelled()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Cancelled));

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.Cancelled);
        result.Identity.JourneyId.Should().Be("opn:OPN1");
    }

    [Fact]
    public void FulfilledSourceLinkedToAdmission_FollowsAdmissionAndKeepsOpnJourneyId()
    {
        var before = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Requested));
        var beforeId = Release1JourneyStageResolver.Resolve(before).Identity.JourneyId;

        var after = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"),
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1")],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Waiting)]);

        var result = Release1JourneyStageResolver.Resolve(after);

        beforeId.Should().Be("opn:OPN1");
        result.Identity.JourneyId.Should().Be(beforeId);
        result.Stage.Should().Be(JourneyOperationalStage.WardAcceptanceRequired);
        result.ReconciliationIssues.Should().BeEmpty();
    }

    [Fact]
    public void RealizedReservationLinkedToAdmission_KeepsRsvJourneyId()
    {
        var facts = Facts(
            JourneyOriginKind.Reservation,
            rsv: Reservation("RSV1", ReservationStatusEnum.Realized, "RG9"),
            admissions: [Admission("RG9", AdmissionStatusEnum.Admitted, reservationId: "RSV1")]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Identity.JourneyId.Should().Be("rsv:RSV1");
        result.Stage.Should().Be(JourneyOperationalStage.HandoverRequired);
    }

    [Fact]
    public void FulfilledSourceWithoutAdmission_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG-MISSING"));

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.NextTask!.CanExecute.Should().BeFalse();
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.FulfilledSourceWithoutAdmission);
    }

    [Fact]
    public void RealizedReservationWithoutAdmission_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.Reservation,
            rsv: Reservation("RSV1", ReservationStatusEnum.Realized, "RG-MISSING"));

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.FulfilledSourceWithoutAdmission);
    }

    [Fact]
    public void MultipleAdmissionsForOneSource_NeedsReconciliation_UsesRegJourneyId()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"),
            admissions:
            [
                Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1"),
                Admission("RG2", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1")
            ]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.Identity.JourneyId.Should().StartWith("reg:");
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.MultipleAdmissionsForSource);
    }

    [Fact]
    public void AdmissionContainingBothSourceTypes_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, "OPN1", "RSV1")]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.AdmissionHasBothSources);
    }

    [Fact]
    public void MultipleActiveWaitingLists_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            waitingLists:
            [
                WaitingList("WL1", "RG1", WaitingListStatusEnum.Waiting),
                WaitingList("WL2", "RG1", WaitingListStatusEnum.Accepted)
            ]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.MultipleActiveWaitingLists);
    }

    [Fact]
    public void OrphanWaitingList_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Requested),
            waitingLists: [WaitingList("WL1", "RG-ORPHAN", WaitingListStatusEnum.Waiting)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.OrphanWaitingList);
    }

    [Fact]
    public void OrphanRegistration_WithoutEpisodeRoot_IsReportedByIntegrity()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            orphanRegistration: true);

        var integrity = JourneyIntegrityValidator.Validate(facts);
        integrity.Issues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.OrphanRegistration);
        integrity.HasBlockingIssues.Should().BeTrue();
    }

    [Fact]
    public void OrphanRegistrationWithAdmissionFacts_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)],
            orphanRegistration: true);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.OrphanRegistration);
    }

    [Fact]
    public void JourneyId_StableBeforeAndAfterRegistration()
    {
        var prospective = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN-STABLE", OpnameRequestStatusEnum.Requested));
        var registered = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN-STABLE", OpnameRequestStatusEnum.Fulfilled, "RG100"),
            admissions: [Admission("RG100", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN-STABLE")]);

        var idBefore = Release1JourneyStageResolver.Resolve(prospective).Identity.JourneyId;
        var idAfter = Release1JourneyStageResolver.Resolve(registered).Identity.JourneyId;

        idBefore.Should().Be("opn:OPN-STABLE");
        idAfter.Should().Be(idBefore);
    }

    [Fact]
    public void TwoEpisodesSamePatient_RemainDistinctJourneyIds()
    {
        var episode1 = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN-A", OpnameRequestStatusEnum.Requested),
            patientId: "P-SAME");
        var episode2 = Facts(
            JourneyOriginKind.Reservation,
            rsv: Reservation("RSV-B", ReservationStatusEnum.Reserved),
            patientId: "P-SAME");

        var id1 = Release1JourneyStageResolver.Resolve(episode1).Identity.JourneyId;
        var id2 = Release1JourneyStageResolver.Resolve(episode2).Identity.JourneyId;

        id1.Should().Be("opn:OPN-A");
        id2.Should().Be("rsv:RSV-B");
        id1.Should().NotBe(id2);
    }

    [Fact]
    public void AdmissionCompleted_IsNeedsReconciliation_CompletedUnreachable()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Completed)],
            waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Closed)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.Stage.Should().NotBe(JourneyOperationalStage.Completed);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.AdmissionCompletedWithoutAuthoritativeProcess);
    }

    [Fact]
    public void SourceFulfillmentRegIdMismatch_NeedsReconciliation()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"),
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN-OTHER")]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.Stage.Should().Be(JourneyOperationalStage.NeedsReconciliation);
        result.ReconciliationIssues.Should().Contain(i =>
            i.Code == JourneyReconciliationCodes.SourceFulfillmentRegIdMismatch);
    }

    [Fact]
    public void PlacementAvailability_AlwaysUnavailableInRelease1()
    {
        var facts = Facts(
            JourneyOriginKind.DirectOrLegacyAdmission,
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)]);

        var result = Release1JourneyStageResolver.Resolve(facts);

        result.PlacementAvailability.Status.Should().Be(JourneyPlacementAvailabilityStatus.Unavailable);
        result.PlacementAvailability.ReasonCode.Should().Be(
            JourneyPlacementUnavailableReasonCode.WardManagementNotImplemented);
    }

    [Fact]
    public void EverySupportedFactCombination_ProducesExactlyOneDeterministicStage()
    {
        var cases = BuildExhaustiveCases();
        foreach (var (name, facts) in cases)
        {
            var first = Release1JourneyStageResolver.Resolve(facts);
            var second = Release1JourneyStageResolver.Resolve(facts);

            first.Stage.Should().Be(second.Stage, because: name);
            first.Identity.JourneyId.Should().Be(second.Identity.JourneyId, because: name);
            first.CurrentCondition.Title.Should().Be(second.CurrentCondition.Title, because: name);
            if (first.NextTask is null)
                second.NextTask.Should().BeNull(because: name);
            else
            {
                second.NextTask.Should().NotBeNull(because: name);
                first.NextTask.Code.Should().Be(second.NextTask!.Code, because: name);
                first.NextTask.CanExecute.Should().Be(second.NextTask.CanExecute, because: name);
                first.NextTask.Owner.Domain.Should().Be(second.NextTask.Owner.Domain, because: name);
            }

            // Exactly one stage; Completed and InWard are never emitted in Release 1.
            first.Stage.Should().NotBe(JourneyOperationalStage.Completed, because: name);
            first.Stage.Should().NotBe(JourneyOperationalStage.InWard, because: name);
        }

        cases.Select(c => c.Name).Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void LegacyRecordResolution_OpnameAndReservation_AreDirect()
    {
        JourneyIdFactory.ResolveLegacyRecord("opnameRequest", "OPN9")
            .Should().Be("opn:OPN9");
        JourneyIdFactory.ResolveLegacyRecord("reservation", "RSV9")
            .Should().Be("rsv:RSV9");
    }

    [Fact]
    public void LegacyRecordResolution_Admission_UsesSourceWhenKnown()
    {
        var facts = Facts(
            JourneyOriginKind.OpnameRequest,
            opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"),
            admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1")]);

        JourneyIdFactory.ResolveLegacyRecord("admission", "RG1", facts)
            .Should().Be("opn:OPN1");
    }

    private static List<(string Name, JourneyNormalizedFacts Facts)> BuildExhaustiveCases() =>
    [
        ("opn-requested", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Requested))),
        ("opn-cancelled", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Cancelled))),
        ("rsv-reserved", Facts(JourneyOriginKind.Reservation, rsv: Reservation("RSV1", ReservationStatusEnum.Reserved))),
        ("rsv-maintained", Facts(JourneyOriginKind.Reservation, rsv: Reservation("RSV1", ReservationStatusEnum.Maintained))),
        ("rsv-cancelled", Facts(JourneyOriginKind.Reservation, rsv: Reservation("RSV1", ReservationStatusEnum.Cancelled))),
        ("adm-no-wl", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)])),
        ("adm-updated-no-wl", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Updated)])),
        ("adm-waiting-no-wl", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Waiting)])),
        ("adm-cancelled", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Cancelled)])),
        ("wl-waiting", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Waiting)])),
        ("wl-accepted", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Accepted)])),
        ("wl-closed", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Closed)])),
        ("wl-cancelled", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Cancelled)])),
        ("wl-closed-then-cancelled", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Closed, T1), WaitingList("WL2", "RG1", WaitingListStatusEnum.Cancelled, T2)])),
        ("fulfilled-linked", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"), admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1")])),
        ("realized-linked", Facts(JourneyOriginKind.Reservation, rsv: Reservation("RSV1", ReservationStatusEnum.Realized, "RG1"), admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, reservationId: "RSV1")])),
        ("fulfilled-missing-adm", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RGX"))),
        ("multi-adm", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Fulfilled, "RG1"), admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1"), Admission("RG2", AdmissionStatusEnum.Admitted, opnameRequestId: "OPN1")])),
        ("both-sources", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted, "OPN1", "RSV1")])),
        ("multi-active-wl", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], waitingLists: [WaitingList("WL1", "RG1", WaitingListStatusEnum.Waiting), WaitingList("WL2", "RG1", WaitingListStatusEnum.Accepted)])),
        ("orphan-wl", Facts(JourneyOriginKind.OpnameRequest, opn: Opname("OPN1", OpnameRequestStatusEnum.Requested), waitingLists: [WaitingList("WL1", "RGX", WaitingListStatusEnum.Waiting)])),
        ("orphan-reg", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Admitted)], orphanRegistration: true)),
        ("adm-completed", Facts(JourneyOriginKind.DirectOrLegacyAdmission, admissions: [Admission("RG1", AdmissionStatusEnum.Completed)]))
    ];
}
