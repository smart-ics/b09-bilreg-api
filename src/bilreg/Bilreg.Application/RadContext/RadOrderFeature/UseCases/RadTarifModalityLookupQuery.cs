using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.RadContext.RadOrderFeature.UseCases;

public record RadTarifModalityLookupQuery(string? Modality = null) : IRequest<IEnumerable<RadTarifModalityDto>>;

public class RadTarifModalityLookupHandler : IRequestHandler<RadTarifModalityLookupQuery, IEnumerable<RadTarifModalityDto>>
{
    private readonly IRadTarifModalityDal _dal;

    public RadTarifModalityLookupHandler(IRadTarifModalityDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<RadTarifModalityDto>> Handle(RadTarifModalityLookupQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));

        var result = !string.IsNullOrWhiteSpace(request.Modality)
            ? _dal.ListByModality(request.Modality)
            : _dal.ListActive();

        return Task.FromResult(result);
    }
}
