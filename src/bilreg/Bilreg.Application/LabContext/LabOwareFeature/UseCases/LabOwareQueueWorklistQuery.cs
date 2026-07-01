using MediatR;

namespace Bilreg.Application.LabContext.LabOwareFeature.UseCases;

public record LabOwareQueueWorklistQuery(
    int? QueueStatus,
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2)
    : IRequest<IEnumerable<LabOwareQueueWorklistView>>;

public class LabOwareQueueWorklistHandler
    : IRequestHandler<LabOwareQueueWorklistQuery, IEnumerable<LabOwareQueueWorklistView>>
{
    private readonly ILabOwareQueueWorklistDal _dal;

    public LabOwareQueueWorklistHandler(ILabOwareQueueWorklistDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<LabOwareQueueWorklistView>> Handle(
        LabOwareQueueWorklistQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabOwareQueueWorklistFilter(
            request.QueueStatus,
            request.Date1,
            request.Date2,
            request.SearchTerm);

        var list = _dal.List(filter);
        return Task.FromResult(list);
    }
}
