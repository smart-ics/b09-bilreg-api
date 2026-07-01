using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record PropinsiListQuery : IRequest<IEnumerable<PropinsiListResponse>>;

[PublicAPI]
public record PropinsiListResponse(string PropinsiId, string PropinsiName);

public class PropinsiListHandler : IRequestHandler<PropinsiListQuery, IEnumerable<PropinsiListResponse>>
{
    private readonly IPropinsiDal _propinsiDal;

    public PropinsiListHandler(IPropinsiDal propinsiDal)
    {
        _propinsiDal = propinsiDal;
    }

    public Task<IEnumerable<PropinsiListResponse>> Handle(PropinsiListQuery request, CancellationToken cancellationToken)
        => _propinsiDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new PropinsiListResponse(y.PropinsiId, y.PropinsiName))),
                onNone: () => throw new KeyNotFoundException($"Propinsi not found"));
}