using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.CpoeContext.ClinicalOrderFeature;

public record OrderTypeType(string OrderTypeId, string OrderTypeName)
{
    public static OrderTypeType Create(string orderTypeId, string orderTypeName)
    {
        Guard.Against.NullOrWhiteSpace(orderTypeId);
        Guard.Against.NullOrWhiteSpace(orderTypeName);
        return new OrderTypeType(orderTypeId, orderTypeName);
    }

    public static OrderTypeType Default => new("-", "-");
}

public record OrderSpecificationType(string RequestedWork, string ClinicalInstructions)
{
    public static OrderSpecificationType Create(string requestedWork, string? clinicalInstructions)
    {
        Guard.Against.NullOrWhiteSpace(requestedWork);
        return new OrderSpecificationType(requestedWork, clinicalInstructions ?? string.Empty);
    }

    public static OrderSpecificationType Default => new("-", string.Empty);
}

public record DestinationType(string DestinationId, string DestinationName)
{
    public static DestinationType Create(string destinationId, string destinationName)
    {
        Guard.Against.NullOrWhiteSpace(destinationId);
        Guard.Against.NullOrWhiteSpace(destinationName);
        return new DestinationType(destinationId, destinationName);
    }

    public static DestinationType Default => new("-", "-");
}

public record OrderOccurrencePlanType(DateTime PlannedExecutionAt, DestinationType Destination)
{
    public static OrderOccurrencePlanType Create(DateTime plannedExecutionAt, DestinationType destination)
    {
        Guard.Against.Null(destination);
        if (plannedExecutionAt == default)
            throw new ArgumentException("Planned execution time wajib diisi.", nameof(plannedExecutionAt));

        return new OrderOccurrencePlanType(plannedExecutionAt, destination);
    }
}

public record FulfilmentEvidenceType(
    string FulfilmentEvidenceId,
    FulfilmentOutcomeEnum Outcome,
    string DestinationId,
    string AttesterId,
    DateTime RecordedAt,
    string Narrative,
    string NotPerformedReason)
{
    private const string IdPrefix = "CFE";

    public static FulfilmentEvidenceType Create(
        FulfilmentOutcomeEnum outcome,
        string destinationId,
        string attesterId,
        DateTime recordedAt,
        string? narrative,
        string? notPerformedReason)
    {
        if (!Enum.IsDefined(outcome))
            throw new ArgumentOutOfRangeException(nameof(outcome));
        Guard.Against.NullOrWhiteSpace(destinationId);
        Guard.Against.NullOrWhiteSpace(attesterId);
        if (recordedAt == default)
            throw new ArgumentException("Recorded time wajib diisi.", nameof(recordedAt));
        if (outcome == FulfilmentOutcomeEnum.NotPerformed)
            Guard.Against.NullOrWhiteSpace(notPerformedReason);
        if (outcome == FulfilmentOutcomeEnum.Completed && !string.IsNullOrWhiteSpace(notPerformedReason))
            throw new ArgumentException("Completed fulfilment tidak boleh memiliki alasan Not Performed.", nameof(notPerformedReason));

        return new FulfilmentEvidenceType(
            NunaId.New(IdPrefix), outcome, destinationId, attesterId, recordedAt,
            narrative ?? string.Empty, notPerformedReason ?? string.Empty);
    }
}

public record CompletionProgressType(int Total, int Active, int Completed, int NotPerformed, int Cancelled)
{
    public bool IsComplete => Active == 0;
}
