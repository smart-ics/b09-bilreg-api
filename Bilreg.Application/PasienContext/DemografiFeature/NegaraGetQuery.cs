using Bilreg.Domain.PasienContext.DemografiFeature;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record NegaraGetQuery(string NegaraId) : IRequest<NegaraGetResponse>, INegaraKey;
public record NegaraGetResponse(string NegaraId, string NegaraName, string NegaraEngName,
    string Alpha3, string NegaraNumeric);

public class NegaraGetHandler : IRequestHandler<NegaraGetQuery, NegaraGetResponse>
{
    private readonly INegaraDal _dal;

    public NegaraGetHandler(INegaraDal dal)
    {
        _dal = dal;
    }

    public Task<NegaraGetResponse> Handle(NegaraGetQuery request, CancellationToken cancellationToken)
        => _dal.GetData(NegaraType.Key(request.NegaraId))
        .Match(
            onSome: x => Task.FromResult(new NegaraGetResponse(x.NegaraId, x.NegaraName, x.NegaraEngName, x.Alpha3, x.NegaraNumeric)),
            onNone: () => throw new KeyNotFoundException($"Negara {request.NegaraId} not found")
            );
    
}
