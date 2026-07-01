namespace Bilreg.Application.LabContext.LabOwareFeature;

public record LabOwareQueueWorklistFilter(
    int? QueueStatus,
    DateTime? Date1,
    DateTime? Date2,
    string? SearchTerm);

public record LabOwareQueueWorklistView(
    string QueueId,
    string OrderNo,
    int QueueStatus,
    int RetryCount,
    string LastError,
    DateTime CrtDate,
    DateTime ProcessedDate);

public interface ILabOwareQueueWorklistDal
{
    IEnumerable<LabOwareQueueWorklistView> List(LabOwareQueueWorklistFilter filter);
}
