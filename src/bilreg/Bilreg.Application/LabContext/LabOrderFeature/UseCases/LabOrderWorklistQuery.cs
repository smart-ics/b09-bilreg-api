using MediatR;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderWorklistQuery(
    int? LabOrderStatus,
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2) : IRequest<IEnumerable<LabOrderWorklistView>>;

public class LabOrderWorklistHandler : IRequestHandler<LabOrderWorklistQuery, IEnumerable<LabOrderWorklistView>>
{
    private readonly ILabOrderWorklistDal _dal;

    public LabOrderWorklistHandler(ILabOrderWorklistDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<LabOrderWorklistView>> Handle(
        LabOrderWorklistQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabOrderWorklistFilter(
            request.LabOrderStatus,
            request.SearchTerm,
            request.Date1,
            request.Date2);

        return Task.FromResult(_dal.List(filter));
    }
}
