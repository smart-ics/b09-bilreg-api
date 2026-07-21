using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using FluentAssertions;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class PharmacyQueueEvidenceTest
{
    private static PasienTrackerModel CreateTracker()
    {
        var person = new PersonType("Sinta", new DateOnly(2008, 5, 5));
        return new PasienTrackerModel(
            "01HXYZABCDEFGHJKMNPQRSTVWXY",
            person,
            new DateOnly(2025, 8, 3),
            new DateOnly(2025, 8, 1),
            new DateOnly(2025, 8, 3),
            []);
    }

    [Fact]
    public void AppendApotekStart_AddsEventWithPenjualanReference()
    {
        var tracker = CreateTracker();
        var servedAt = new DateTime(2025, 8, 3, 7, 51, 0);

        PharmacyQueueEvidence.AppendApotekStart(tracker, "DU-071", servedAt);

        tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekStartEventName
            && e.ReffId == "DU-071"
            && e.EventDate == servedAt);
    }

    [Fact]
    public void AppendApotekStart_WhenCalledTwice_IsIdempotent()
    {
        var tracker = CreateTracker();
        var servedAt = new DateTime(2025, 8, 3, 7, 51, 0);

        PharmacyQueueEvidence.AppendApotekStart(tracker, "DU-071", servedAt);
        PharmacyQueueEvidence.AppendApotekStart(tracker, "DU-071", servedAt);

        tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekStartEventName);
    }

    [Fact]
    public void AppendApotekDone_AddsQueueEvidenceReference()
    {
        var tracker = CreateTracker();
        var doneAt = new DateTime(2025, 8, 3, 8, 5, 0);
        var queueRef = QueueEvidenceReference.Create("AN003", 1).Value;

        PharmacyQueueEvidence.AppendApotekDone(tracker, queueRef, doneAt);

        tracker.ListEvent.Should().ContainSingle(e =>
            e.EventName == PharmacyQueueEvidence.ApotekDoneEventName
            && e.ReffId == queueRef
            && e.EventDate == doneAt);
    }
}
