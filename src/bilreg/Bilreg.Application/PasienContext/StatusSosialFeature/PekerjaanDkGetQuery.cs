using Bilreg.Domain.PasienContext.StatusSosialFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record PekerjaanDkGetQuery(string PekerjaanDkId) : IRequest<PekerjaanDkGetResponse>;

[PublicAPI]
public record PekerjaanDkGetResponse(string PekerjaanDkId, string PekerjaanDkName);

public class PekerjaanDkGetHandler : IRequestHandler<PekerjaanDkGetQuery, PekerjaanDkGetResponse>
{
    private readonly IPekerjaanDkDal _pekerjaanDkDal;

    public PekerjaanDkGetHandler(IPekerjaanDkDal pekerjaanDkDal)
    {
        _pekerjaanDkDal = pekerjaanDkDal;
    }

    public Task<PekerjaanDkGetResponse> Handle(PekerjaanDkGetQuery request, CancellationToken cancellationToken)
        =>  _pekerjaanDkDal.GetData(PekerjaanDkType.Key(request.PekerjaanDkId))
            .Match(
                onSome: x => Task.FromResult(new PekerjaanDkGetResponse(x.PekerjaanDkId, x.PekerjaanDkName)), 
                onNone: () => throw new KeyNotFoundException($"PekerjaanDk {request.PekerjaanDkId} not found"));
}