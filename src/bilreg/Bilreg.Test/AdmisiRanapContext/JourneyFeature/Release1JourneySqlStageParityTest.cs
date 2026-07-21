using Bilreg.Application.AdmisiRanapContext.JourneyFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using Bilreg.Domain.AdmisiRanapContext.ReservationFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class Release1JourneySqlStageParityTest
{
    private static readonly DateTime T0 = new(2026, 7, 1, 8, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T1 = new(2026, 7, 2, 9, 0, 0, DateTimeKind.Utc);
    private static readonly DateTime T2 = new(2026, 7, 3, 10, 0, 0, DateTimeKind.Utc);

    private static JourneyNormalizedFacts Facts(
        JourneyOriginKind origin,
        OpnameRequestJourneyFact? opn = null,
        ReservationJourneyFact? rsv = null,
        AdmissionJourneyFact[]? admissions = null,
        WaitingListJourneyFact[]? waitingLists = null) =>
        new(
            origin,
            new JourneyPatientSummary("P001", "RM001", "Pasien Uji"),
            opn,
            rsv,
            admissions ?? [],
            waitingLists ?? []);

    public static IEnumerable<object[]> ParityCases()
    {
        yield return
        [
            "requested_opname",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Requested, "-", T0))
        ];
        yield return
        [
            "reserved_reservation",
            Facts(JourneyOriginKind.Reservation,
                rsv: new("RSV1", ReservationStatusEnum.Reserved, "-", T0))
        ];
        yield return
        [
            "maintained_reservation",
            Facts(JourneyOriginKind.Reservation,
                rsv: new("RSV1", ReservationStatusEnum.Maintained, "-", T0))
        ];
        yield return
        [
            "cancelled_opname",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Cancelled, "-", T0))
        ];
        yield return
        [
            "admission_handover_required",
            Facts(JourneyOriginKind.DirectOrLegacyAdmission,
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "-", "-", T1, "B01", "Bangsal")])
        ];
        yield return
        [
            "wl_waiting",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Fulfilled, "REG1", T0),
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "OPN1", "-", T1, "B01", "Bangsal")],
                waitingLists: [new("WL1", "REG1", WaitingListStatusEnum.Waiting, T2, "B01", "Bangsal")])
        ];
        yield return
        [
            "wl_accepted",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Fulfilled, "REG1", T0),
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "OPN1", "-", T1, "B01", "Bangsal")],
                waitingLists: [new("WL1", "REG1", WaitingListStatusEnum.Accepted, T2, "B01", "Bangsal")])
        ];
        yield return
        [
            "wl_closed_placement_unavailable",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Fulfilled, "REG1", T0),
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "OPN1", "-", T1, "B01", "Bangsal")],
                waitingLists: [new("WL1", "REG1", WaitingListStatusEnum.Closed, T2, "B01", "Bangsal")])
        ];
        yield return
        [
            "wl_cancelled_restart_handover",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Fulfilled, "REG1", T0),
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "OPN1", "-", T1, "B01", "Bangsal")],
                waitingLists: [new("WL1", "REG1", WaitingListStatusEnum.Cancelled, T2, "B01", "Bangsal")])
        ];
        yield return
        [
            "admission_cancelled",
            Facts(JourneyOriginKind.DirectOrLegacyAdmission,
                admissions: [new("REG1", AdmissionStatusEnum.Cancelled, "-", "-", T1)])
        ];
        yield return
        [
            "admission_completed_recon",
            Facts(JourneyOriginKind.DirectOrLegacyAdmission,
                admissions: [new("REG1", AdmissionStatusEnum.Completed, "-", "-", T1)])
        ];
        yield return
        [
            "both_sources_recon",
            Facts(JourneyOriginKind.DirectOrLegacyAdmission,
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "OPN1", "RSV1", T1)])
        ];
        yield return
        [
            "multiple_active_wl_recon",
            Facts(JourneyOriginKind.DirectOrLegacyAdmission,
                admissions: [new("REG1", AdmissionStatusEnum.Admitted, "-", "-", T1, "B01", "Bangsal")],
                waitingLists:
                [
                    new("WL1", "REG1", WaitingListStatusEnum.Waiting, T2, "B01", "Bangsal"),
                    new("WL2", "REG1", WaitingListStatusEnum.Accepted, T2.AddMinutes(1), "B01", "Bangsal")
                ])
        ];
        yield return
        [
            "fulfilled_without_admission_recon",
            Facts(JourneyOriginKind.OpnameRequest,
                opn: new("OPN1", OpnameRequestStatusEnum.Fulfilled, "REG_MISSING", T0))
        ];
    }

    [Theory]
    [MemberData(nameof(ParityCases))]
    public void SqlStageExpression_Matches_B1Resolver(string caseName, JourneyNormalizedFacts facts)
    {
        var b1 = Release1JourneyStageResolver.Resolve(facts).Stage;
        var sqlInputs = Release1JourneySqlStageExpression.FromNormalizedFacts(facts);
        var sqlStage = Release1JourneySqlStageExpression.Derive(sqlInputs);

        sqlStage.Should().Be(b1, because: $"case '{caseName}' must keep SQL stage parity with B1");
        b1.Should().NotBe(JourneyOperationalStage.InWard);
        if (facts.WaitingLists.Any(w => w.Status == WaitingListStatusEnum.Closed)
            && facts.Admissions.Any(a => a.Status is AdmissionStatusEnum.Admitted
                or AdmissionStatusEnum.Updated or AdmissionStatusEnum.Waiting)
            && !facts.WaitingLists.Any(w => w.IsActive)
            && !JourneyIntegrityValidator.Validate(facts).HasBlockingIssues)
        {
            b1.Should().Be(JourneyOperationalStage.PlacementStatusUnavailable);
            b1.Should().NotBe(JourneyOperationalStage.Completed);
        }
    }

    [Fact]
    public void Cursor_IsStable_ForEqualTimestamps()
    {
        var sortAt = new DateTime(2026, 7, 13, 10, 0, 0);
        var c1 = JourneyListCursor.Encode(sortAt, "opn:A");
        var c2 = JourneyListCursor.Encode(sortAt, "opn:B");
        c1.Should().NotBe(c2);

        JourneyListCursor.TryDecode(c1, out var s1, out var j1).Should().BeTrue();
        JourneyListCursor.TryDecode(c2, out var s2, out var j2).Should().BeTrue();
        s1.Should().Be(sortAt);
        s2.Should().Be(sortAt);
        j1.Should().Be("opn:A");
        j2.Should().Be("opn:B");
    }
}
