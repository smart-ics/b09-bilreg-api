using Bilreg.Domain.BedUsageContext.RnaServiceExecutionFeature;

namespace Bilreg.Infrastructure.BedUsageContext.RnaServiceExecutionFeature;

public record ServiceWorkSourceRevisionDto(
    string ServiceExecutionId, string SourceContext, string SourceFactId, int SourceRevision,
    int RevisionKind, DateTime EffectiveAt, DateTime RecordedAt, string ActorId, string Reason,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);
    public static ServiceWorkSourceRevisionDto FromModel(string serviceExecutionId, ServiceWorkSourceRevisionType model) => new(
        serviceExecutionId, model.SourceContext, model.SourceFactId, model.SourceRevision,
        (int)model.RevisionKind, model.EffectiveAt, model.RecordedAt, model.ActorId, model.Reason,
        model.ActorId, model.RecordedAt, string.Empty, EmptyDate, string.Empty, EmptyDate);
    public ServiceWorkSourceRevisionType ToModel() => new(SourceContext, SourceFactId, SourceRevision,
        (SourceRevisionKindEnum)RevisionKind, AsUtc(EffectiveAt), AsUtc(RecordedAt), ActorId, Reason);
    private static DateTime AsUtc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
}
