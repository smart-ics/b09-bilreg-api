using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public record ServiceExecutionFactDto(
    string ServiceExecutionFactId, string ServiceExecutionId, int ExecutionRevision,
    int BillableClassification, string TarifServiceId, string TarifServiceName,
    string NonBillableDescription, string PerformerId, DateTime PerformedAt,
    DateTime RecordedAt, string RecorderActorId, string LateEntryReason,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);
    public static ServiceExecutionFactDto FromModel(string serviceExecutionId, ServiceExecutionFactType model) => new(
        model.ServiceExecutionFactId, serviceExecutionId, model.ExecutionRevision, (int)model.BillableClassification,
        model.TarifService?.TarifServiceId ?? string.Empty, model.TarifService?.TarifServiceName ?? string.Empty,
        model.NonBillableDescription, model.PerformerId, model.PerformedAt, model.RecordedAt,
        model.RecorderActorId, model.LateEntryReason, model.RecorderActorId, model.RecordedAt,
        string.Empty, EmptyDate, string.Empty, EmptyDate);
    public ServiceExecutionFactType ToModel() => new(ServiceExecutionFactId, ExecutionRevision,
        (BillableClassificationEnum)BillableClassification,
        string.IsNullOrWhiteSpace(TarifServiceId) ? null : new TarifServiceReff(TarifServiceId, TarifServiceName),
        NonBillableDescription, PerformerId, AsUtc(PerformedAt), AsUtc(RecordedAt), RecorderActorId, LateEntryReason);
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
