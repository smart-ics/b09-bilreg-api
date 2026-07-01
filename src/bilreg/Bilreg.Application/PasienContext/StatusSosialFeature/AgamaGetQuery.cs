using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record AgamaGetQuery(string AgamaId) : IRequest<AgamaGetResponse>;

[PublicAPI]
public record AgamaGetResponse(string AgamaId, string AgamaName);

public class AgamaGetHandler : IRequestHandler<AgamaGetQuery, AgamaGetResponse>
{
    private readonly IAgamaDal _agamaDal;

    public AgamaGetHandler(IAgamaDal agamaDal)
    {
        _agamaDal = agamaDal;
    }

    public Task<AgamaGetResponse> Handle(AgamaGetQuery request, CancellationToken cancellationToken)
        =>  _agamaDal.GetData(AgamaType.Key(request.AgamaId))
            .Match(
                onSome: x => Task.FromResult(new AgamaGetResponse(x.AgamaId, x.AgamaName)), 
                onNone: () => throw new KeyNotFoundException($"Agama {request.AgamaId} not found"));
}