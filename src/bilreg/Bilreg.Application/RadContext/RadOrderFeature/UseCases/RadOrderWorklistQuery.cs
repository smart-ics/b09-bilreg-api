using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadOrderWorklistQuery(DateTime Date, string Modality, int? Status = null) : IRequest<IEnumerable<RadOrderWorklistView>>;

public class RadOrderWorklistHandler : IRequestHandler<RadOrderWorklistQuery, IEnumerable<RadOrderWorklistView>>
{
    private readonly IRadOrderDal _orderDal;

    public RadOrderWorklistHandler(IRadOrderDal orderDal)
    {
        _orderDal = orderDal;
    }

    public Task<IEnumerable<RadOrderWorklistView>> Handle(RadOrderWorklistQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var result = _orderDal.GetWorklist(request.Date, request.Modality, request.Status);
        return Task.FromResult(result);
    }
}
