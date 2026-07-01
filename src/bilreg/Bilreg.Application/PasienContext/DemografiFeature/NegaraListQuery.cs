using MediatR;

namespace Bilreg.Application.PasienContext.DemografiFeature;

public record NegaraListQuery() : IRequest<IEnumerable<NegaraListResponse>>;

public record NegaraListResponse(string NegaraId, string NegaraName, string NegaraEngName,
    string Alpha3, string NegaraNumeric);

public class NegaraListHandler : IRequestHandler<NegaraListQuery, IEnumerable<NegaraListResponse>>
{
    private readonly INegaraDal _dal;

    public NegaraListHandler(INegaraDal dal)
    {
        _dal = dal;
    }

    public Task<IEnumerable<NegaraListResponse>> Handle(NegaraListQuery request, CancellationToken cancellationToken)
        => _dal.ListData()
        .Match(
            onSome: x => Task.FromResult(x.Select(y 
                => new NegaraListResponse(y.NegaraId, y.NegaraName, y.NegaraEngName, y.Alpha3, y.NegaraNumeric))),
            onNone: () => throw new KeyNotFoundException("Data not found")
            );
    
}
