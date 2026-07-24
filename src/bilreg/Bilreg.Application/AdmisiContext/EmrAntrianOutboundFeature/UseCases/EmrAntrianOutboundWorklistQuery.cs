using MediatR;

namespace Bilreg.Application.AdmisiContext.EmrAntrianOutboundFeature.UseCases;

public record EmrAntrianOutboundWorklistQuery(
    int? QueueStatus,
    string? SearchTerm,
    DateTime? Date1,
    DateTime? Date2)
    : IRequest<IEnumerable<EmrAntrianOutboundWorklistView>>;

public class EmrAntrianOutboundWorklistHandler
    : IRequestHandler<EmrAntrianOutboundWorklistQuery, IEnumerable<EmrAntrianOutboundWorklistView>>
{
    private readonly IEmrAntrianOutboundWorklistDal _dal;

    public EmrAntrianOutboundWorklistHandler(IEmrAntrianOutboundWorklistDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<EmrAntrianOutboundWorklistView>> Handle(
        EmrAntrianOutboundWorklistQuery request,
        CancellationToken cancellationToken)
    {
        var filter = new EmrAntrianOutboundWorklistFilter(
            request.QueueStatus,
            request.Date1,
            request.Date2,
            request.SearchTerm);

        var list = _dal.List(filter);
        return Task.FromResult(list);
    }
}
