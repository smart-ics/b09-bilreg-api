namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature;

public record EmrAntrianOutboundWorklistFilter(
    int? QueueStatus,
    DateTime? Date1,
    DateTime? Date2,
    string? SearchTerm);

public record EmrAntrianOutboundWorklistView(
    string QueueId,
    string SourceId,
    string MessageType,
    int QueueStatus,
    int RetryCount,
    string LastError,
    DateTime CrtDate,
    DateTime ProcessedDate);

public interface IEmrAntrianOutboundWorklistDal
{
    IEnumerable<EmrAntrianOutboundWorklistView> List(EmrAntrianOutboundWorklistFilter filter);
}
