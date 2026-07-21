using Ardalis.GuardClauses;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

public record ServiceExecutionFactType(
    string ServiceExecutionFactId,
    int ExecutionRevision,
    BillableClassificationEnum BillableClassification,
    TarifServiceReff? TarifService,
    string NonBillableDescription,
    string PerformerId,
    DateTime PerformedAt,
    DateTime RecordedAt,
    string RecorderActorId,
    string LateEntryReason)
{
    private const string ID_PREFIX = "SEF";

    internal static ServiceExecutionFactType Record(
        int executionRevision,
        BillableClassificationEnum billableClassification,
        TarifServiceReff? tarifService,
        string? nonBillableDescription,
        string performerId,
        DateTime performedAt,
        DateTime recordedAt,
        string recorderActorId,
        string? lateEntryReason)
    {
        Guard.Against.NullOrWhiteSpace(performerId);
        Guard.Against.NullOrWhiteSpace(recorderActorId);
        if (executionRevision <= 0)
            throw new ArgumentOutOfRangeException(nameof(executionRevision));
        if (!Enum.IsDefined(billableClassification))
            throw new ArgumentOutOfRangeException(nameof(billableClassification));
        EnsureUtc(performedAt, nameof(performedAt));
        EnsureUtc(recordedAt, nameof(recordedAt));
        if (recordedAt < performedAt)
            throw new ArgumentException("RecordedAt tidak boleh lebih awal dari PerformedAt.", nameof(recordedAt));
        if (recordedAt > performedAt && string.IsNullOrWhiteSpace(lateEntryReason))
            throw new ArgumentException("Late entry wajib memiliki alasan.", nameof(lateEntryReason));

        if (billableClassification == BillableClassificationEnum.Billable)
        {
            Guard.Against.Null(tarifService);
            if (!string.IsNullOrWhiteSpace(nonBillableDescription))
                throw new ArgumentException("Eksekusi billable tidak boleh memiliki non-billable description.", nameof(nonBillableDescription));
        }
        else
        {
            Guard.Against.NullOrWhiteSpace(nonBillableDescription);
            if (tarifService is not null)
                throw new ArgumentException("Eksekusi non-billable tidak boleh memiliki Tarif Service.", nameof(tarifService));
        }

        return new ServiceExecutionFactType(
            NunaId.New(ID_PREFIX),
            executionRevision,
            billableClassification,
            tarifService,
            nonBillableDescription ?? string.Empty,
            performerId,
            performedAt,
            recordedAt,
            recorderActorId,
            lateEntryReason ?? string.Empty);
    }

    public static ServiceExecutionFactType Default => new(
        "-", 0, BillableClassificationEnum.NonBillable, null, "-", "-",
        RnaServiceExecutionModel.EmptyDate, RnaServiceExecutionModel.EmptyDate, "-", string.Empty);

    private static void EnsureUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
            throw new ArgumentException($"{parameterName} harus menggunakan DateTimeKind.Utc.", parameterName);
    }
}
