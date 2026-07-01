using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record AgamaListQuery : IRequest<IEnumerable<AgamaListResponse>>;

[PublicAPI]
public record AgamaListResponse(string AgamaId, string AgamaName);

public class AgamaListHandler : IRequestHandler<AgamaListQuery, IEnumerable<AgamaListResponse>>
{
    private readonly IAgamaDal _agamaDal;

    public AgamaListHandler(IAgamaDal agamaDal)
    {
        _agamaDal = agamaDal;
    }

    public Task<IEnumerable<AgamaListResponse>> Handle(AgamaListQuery request, CancellationToken cancellationToken)
        => _agamaDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new AgamaListResponse(y.AgamaId, y.AgamaName))),
                onNone: () => throw new KeyNotFoundException($"Agama not found"));
}