using Bilreg.Domain.CpoeContext.ClinicalOrderFeature;
using FluentAssertions;

namespace Bilreg.Test.CpoeContext.ClinicalOrderFeature;

public class ClinicalOrderModelTest
{
    private static readonly DateTime CreatedAt = new(2026, 7, 20, 8, 0, 0, DateTimeKind.Utc);

    private static ClinicalOrderModel Create(params OrderOccurrencePlanType[] plans) =>
        ClinicalOrderModel.Create(
            "PS001", "RG001", "DOK001", OrderTypeType.Create("MED", "Medication"),
            OrderSpecificationType.Create("Antibiotik IV", "Berikan sesuai dosis"),
            plans.Length == 0 ? [Plan(0)] : plans, CreatedAt);

    private static OrderOccurrencePlanType Plan(int hourOffset) =>
        OrderOccurrencePlanType.Create(CreatedAt.AddHours(hourOffset), DestinationType.Create("WARD-A", "Ward A"));

    [Fact]
    public void GivenValidOrder_WhenCreated_ThenItIsActiveWithRoutedOccurrencesAndHistory()
    {
        var order = Create(Plan(0), Plan(8));

        order.ClinicalOrderStatus.Should().Be(ClinicalOrderStatusEnum.Active);
        order.ListOccurrence.Should().HaveCount(2).And.OnlyContain(x =>
            x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Active && x.Destination.DestinationId == "WARD-A");
        order.ListOccurrence.Select(x => x.OrderOccurrenceId).Should().OnlyHaveUniqueItems();
        order.ListHistory.Should().ContainSingle().Which.Action.Should().Be(OrderHistoryActionEnum.Created);
        order.IsScheduled.Should().BeTrue();
    }

    [Fact]
    public void GivenFinalOccurrence_WhenModifyingIntentOrPlan_ThenItIsRejected()
    {
        var order = Fulfil(Create());

        var modify = () => order.ModifyIntent(
            OrderTypeType.Create("LAB", "Laboratory"), OrderSpecificationType.Create("CBC", null),
            "DOK001", CreatedAt.AddHours(1), "Koreksi klinis");
        var replacePlan = () => order.ReplaceOccurrencePlan([Plan(2)], "DOK001", CreatedAt.AddHours(1), "Jadwal baru");

        modify.Should().Throw<InvalidOperationException>();
        replacePlan.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenFinalOccurrence_WhenChangingDestinationOrTime_ThenItIsRejected()
    {
        var order = Fulfil(Create());
        var occurrenceId = order.ListOccurrence.Single().OrderOccurrenceId;

        var destination = () => order.ChangeOccurrenceDestination(
            occurrenceId, DestinationType.Create("WARD-B", "Ward B"), "DOK002", CreatedAt.AddHours(1), "Pindah ward");
        var time = () => order.ChangeOccurrencePlannedExecutionTime(
            occurrenceId, CreatedAt.AddHours(2), "DOK002", CreatedAt.AddHours(1), "Jadwal baru");

        destination.Should().Throw<InvalidOperationException>();
        time.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenEvidenceFromDifferentDestination_WhenRecorded_ThenItIsRejected()
    {
        var order = Create();
        var evidence = FulfilmentEvidenceType.Create(
            FulfilmentOutcomeEnum.Completed, "WARD-B", "PEG001", CreatedAt.AddHours(1), "Diberikan", null);

        var act = () => order.RecordFulfilment(order.ListOccurrence.Single().OrderOccurrenceId, evidence);

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void GivenNotPerformedEvidenceWithoutReason_WhenCreated_ThenItIsRejected()
    {
        var act = () => FulfilmentEvidenceType.Create(
            FulfilmentOutcomeEnum.NotPerformed, "WARD-A", "PEG001", CreatedAt.AddHours(1), null, null);

        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GivenAllOccurrencesFulfilled_WhenLastIsCompleted_ThenProgressAndStatusAreDerived()
    {
        var order = Create(Plan(0), Plan(8));
        var first = order.ListOccurrence.First();
        order = order.RecordFulfilment(first.OrderOccurrenceId, NotPerformedEvidence());
        var second = order.ListOccurrence.Single(x => x.OrderOccurrenceStatus == OrderOccurrenceStatusEnum.Active);
        order = order.RecordFulfilment(second.OrderOccurrenceId, CompletedEvidence());

        order.ClinicalOrderStatus.Should().Be(ClinicalOrderStatusEnum.Completed);
        order.CompletionProgress.Should().Be(new CompletionProgressType(2, 0, 1, 1, 0));
        order.CompletionProgress.Total.Should().Be(
            order.CompletionProgress.Active + order.CompletionProgress.Completed +
            order.CompletionProgress.NotPerformed + order.CompletionProgress.Cancelled);
    }

    [Fact]
    public void GivenAllOccurrencesNotPerformed_WhenLastIsFinalized_ThenOrderIsNotPerformed()
    {
        var order = Create(Plan(0), Plan(8));
        foreach (var occurrence in order.ListOccurrence.ToList())
            order = order.RecordFulfilment(occurrence.OrderOccurrenceId, NotPerformedEvidence());

        order.ClinicalOrderStatus.Should().Be(ClinicalOrderStatusEnum.NotPerformed);
        order.CompletionProgress.Should().Be(new CompletionProgressType(2, 0, 0, 2, 0));
    }

    [Fact]
    public void GivenHistoryTimestampBeforePriorEntry_WhenIntentChanged_ThenItIsRejected()
    {
        var order = Create();

        var act = () => order.ModifyIntent(
            OrderTypeType.Create("MED", "Medication"), OrderSpecificationType.Create("Dosis baru", null),
            "DOK001", CreatedAt.AddMinutes(-1), "Koreksi resep");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*Order History harus dicatat secara kronologis*");
    }

    [Fact]
    public void GivenActiveOrderWithCompletedOccurrence_WhenCancelled_ThenPreviousOutcomeIsRetained()
    {
        var order = Create(Plan(0), Plan(8));
        var first = order.ListOccurrence.First();
        order = order.RecordFulfilment(first.OrderOccurrenceId, CompletedEvidence());
        order = order.Cancel("DOK001", CreatedAt.AddHours(2), "Terapi dihentikan");

        order.ClinicalOrderStatus.Should().Be(ClinicalOrderStatusEnum.Cancelled);
        order.CompletionProgress.Should().Be(new CompletionProgressType(2, 0, 1, 0, 1));
        order.ListHistory.Last().Action.Should().Be(OrderHistoryActionEnum.Cancelled);
    }

    private static ClinicalOrderModel Fulfil(ClinicalOrderModel order)
    {
        var occurrence = order.ListOccurrence.Single();
        return order.RecordFulfilment(occurrence.OrderOccurrenceId, CompletedEvidence());
    }

    private static FulfilmentEvidenceType CompletedEvidence() =>
        FulfilmentEvidenceType.Create(
            FulfilmentOutcomeEnum.Completed, "WARD-A", "PEG001", CreatedAt.AddHours(1), "Diberikan", null);

    private static FulfilmentEvidenceType NotPerformedEvidence() =>
        FulfilmentEvidenceType.Create(
            FulfilmentOutcomeEnum.NotPerformed, "WARD-A", "PEG001", CreatedAt.AddHours(1), null, "Pasien menolak");
}
