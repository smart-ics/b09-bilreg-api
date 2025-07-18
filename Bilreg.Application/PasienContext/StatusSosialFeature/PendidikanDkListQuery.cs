using Bilreg.Application.PasienContext.StatusSosialFeature.PendidikanDkAgg;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record PendidikanDkListQuery : IRequest<IEnumerable<PendidikanDkListResponse>>;

[PublicAPI]
public record PendidikanDkListResponse(string PendidikanDkId, string PendidikanDkName);

public class PendidikanDkListHandler : IRequestHandler<PendidikanDkListQuery, IEnumerable<PendidikanDkListResponse>>
{
    private readonly IPendidikanDkDal _pendidikanDkDal;

    public PendidikanDkListHandler(IPendidikanDkDal pendidikanDkDal)
    {
        _pendidikanDkDal = pendidikanDkDal;
    }

    public Task<IEnumerable<PendidikanDkListResponse>> Handle(PendidikanDkListQuery request, CancellationToken cancellationToken)
        => _pendidikanDkDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new PendidikanDkListResponse(y.PendidikanDkId, y.PendidikanDkName))),
                onNone: () => throw new KeyNotFoundException($"PendidikanDk not found"));
}