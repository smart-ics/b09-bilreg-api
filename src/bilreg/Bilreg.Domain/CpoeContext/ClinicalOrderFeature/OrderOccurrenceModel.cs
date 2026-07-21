using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.CpoeContext.ClinicalOrderFeature;

public record OrderOccurrenceModel(
    string OrderOccurrenceId,
    DateTime PlannedExecutionAt,
    DestinationType Destination,
    OrderOccurrenceStatusEnum OrderOccurrenceStatus,
    FulfilmentEvidenceType? FulfilmentEvidence)
{
    private const string IdPrefix = "COO";

    internal static OrderOccurrenceModel Create(OrderOccurrencePlanType plan)
    {
        Guard.Against.Null(plan);
        return new OrderOccurrenceModel(
            NunaId.New(IdPrefix), plan.PlannedExecutionAt, plan.Destination,
            OrderOccurrenceStatusEnum.Active, null);
    }

    internal OrderOccurrenceModel ChangeDestination(DestinationType destination)
    {
        Guard.Against.Null(destination);
        EnsureActive();
        return this with { Destination = destination };
    }

    internal OrderOccurrenceModel ChangePlannedExecutionAt(DateTime plannedExecutionAt)
    {
        EnsureActive();
        if (plannedExecutionAt == default)
            throw new ArgumentException("Planned execution time wajib diisi.", nameof(plannedExecutionAt));
        return this with { PlannedExecutionAt = plannedExecutionAt };
    }

    internal OrderOccurrenceModel RecordFulfilment(FulfilmentEvidenceType fulfilmentEvidence)
    {
        Guard.Against.Null(fulfilmentEvidence);
        EnsureActive();
        if (!string.Equals(fulfilmentEvidence.DestinationId, Destination.DestinationId, StringComparison.Ordinal))
            throw new InvalidOperationException("Fulfilment evidence harus berasal dari Destination yang bertanggung jawab.");

        var status = fulfilmentEvidence.Outcome == FulfilmentOutcomeEnum.Completed
            ? OrderOccurrenceStatusEnum.Completed
            : OrderOccurrenceStatusEnum.NotPerformed;
        return this with { OrderOccurrenceStatus = status, FulfilmentEvidence = fulfilmentEvidence };
    }

    internal OrderOccurrenceModel Cancel()
    {
        EnsureActive();
        return this with { OrderOccurrenceStatus = OrderOccurrenceStatusEnum.Cancelled };
    }

    private void EnsureActive()
    {
        if (OrderOccurrenceStatus != OrderOccurrenceStatusEnum.Active)
            throw new InvalidOperationException(
                $"Order occurrence {OrderOccurrenceId} berstatus {OrderOccurrenceStatus} dan tidak dapat diubah.");
    }
}
