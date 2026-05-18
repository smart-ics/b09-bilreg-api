using Bilreg.Application.LabContext.LabOrderFeature;
using MediatR;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderReleaseWorklistQuery(
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2) : IRequest<IEnumerable<LabReleaseView>>;

public class LabOrderReleaseWorklistHandler
    : IRequestHandler<LabOrderReleaseWorklistQuery, IEnumerable<LabReleaseView>>
{
    private readonly ILabOrderReleaseWorklistDal _dal;

    public LabOrderReleaseWorklistHandler(ILabOrderReleaseWorklistDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<LabReleaseView>> Handle(
        LabOrderReleaseWorklistQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new LabOrderReleaseWorklistFilter(
            request.SearchTerm,
            request.Date1,
            request.Date2);

        return Task.FromResult(_dal.List(filter));
    }
}
