using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record PendidikanDkGetQuery(string PendidikanDkId) : IRequest<PendidikanDkGetResponse>;

[PublicAPI]
public record PendidikanDkGetResponse(string PendidikanDkId, string PendidikanDkName);

public class PendidikanDkGetHandler : IRequestHandler<PendidikanDkGetQuery, PendidikanDkGetResponse>
{
    private readonly IPendidikanDkDal _pendidikanDkDal;

    public PendidikanDkGetHandler(IPendidikanDkDal pendidikanDkDal)
    {
        _pendidikanDkDal = pendidikanDkDal;
    }

    public Task<PendidikanDkGetResponse> Handle(PendidikanDkGetQuery request, CancellationToken cancellationToken)
        =>  _pendidikanDkDal.GetData(PendidikanDkType.Key(request.PendidikanDkId))
            .Match(
                onSome: x => Task.FromResult(new PendidikanDkGetResponse(x.PendidikanDkId, x.PendidikanDkName)), 
                onNone: () => throw new KeyNotFoundException($"PendidikanDk {request.PendidikanDkId} not found"));
}