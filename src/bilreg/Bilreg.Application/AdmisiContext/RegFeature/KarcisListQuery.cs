using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record KarcisListQuery(string InstalasiDkId) : IRequest<IEnumerable<KarcisView>>, IInstalasiDkKey;

public class KarcisListHandler : IRequestHandler<KarcisListQuery, IEnumerable<KarcisView>>
{
    private readonly IKarcisRepo _karcisRepo;

    public KarcisListHandler(IKarcisRepo karcisRepo)
    {
        _karcisRepo = karcisRepo;
    }

    public Task<IEnumerable<KarcisView>> Handle(KarcisListQuery request, CancellationToken cancellationToken)
    {
        var listKarcis = _karcisRepo.ListData(request)?.ToList() ??
            throw new KeyNotFoundException($"karcis instalasi dk {request.InstalasiDkId} not found");
        return Task.FromResult( listKarcis.AsEnumerable() );
    }
}
