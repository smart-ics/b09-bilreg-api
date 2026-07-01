using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.PasienContext.StatusSosialFeature;

public record PekerjaanDkListQuery : IRequest<IEnumerable<PekerjaanDkListResponse>>;

[PublicAPI]
public record PekerjaanDkListResponse(string PekerjaanDkId, string PekerjaanDkName);

public class PekerjaanDkListHandler : IRequestHandler<PekerjaanDkListQuery, IEnumerable<PekerjaanDkListResponse>>
{
    private readonly IPekerjaanDkDal _pekerjaanDkDal;

    public PekerjaanDkListHandler(IPekerjaanDkDal pekerjaanDkDal)
    {
        _pekerjaanDkDal = pekerjaanDkDal;
    }

    public Task<IEnumerable<PekerjaanDkListResponse>> Handle(PekerjaanDkListQuery request, CancellationToken cancellationToken)
        => _pekerjaanDkDal.ListData()
            .Match(
                onSome: x => Task.FromResult(x.Select(y 
                    => new PekerjaanDkListResponse(y.PekerjaanDkId, y.PekerjaanDkName))),
                onNone: () => throw new KeyNotFoundException($"PekerjaanDk not found"));
}