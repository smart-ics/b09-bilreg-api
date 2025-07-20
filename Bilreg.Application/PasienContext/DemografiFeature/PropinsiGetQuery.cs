using Bilreg.Domain.PasienContext.DemografiFeature;
using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.DemografiSub;

public record PropinsiGetQuery(string PropinsiId) : IRequest<PropinsiGetResponse>;

[PublicAPI]
public record PropinsiGetResponse(string PropinsiId, string PropinsiName);

public class PropinsiGetHandler : IRequestHandler<PropinsiGetQuery, PropinsiGetResponse>
{
    private readonly IPropinsiDal _propinsiDal;

    public PropinsiGetHandler(IPropinsiDal propinsiDal)
    {
        _propinsiDal = propinsiDal;
    }

    public Task<PropinsiGetResponse> Handle(PropinsiGetQuery request, CancellationToken cancellationToken)
        =>  _propinsiDal.GetData(PropinsiType.Key(request.PropinsiId))
            .Match(
                onSome: x => Task.FromResult(new PropinsiGetResponse(x.PropinsiId, x.PropinsiName)), 
                onNone: () => throw new KeyNotFoundException($"Propinsi {request.PropinsiId} not found"));
}