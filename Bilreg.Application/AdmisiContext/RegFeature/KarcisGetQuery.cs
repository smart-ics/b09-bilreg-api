using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using System.Reflection.Metadata.Ecma335;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record KarcisGetQuery(string KarcisId) : IRequest<KarcisType>, IKarcisKey;

public class KarcisGetHandler : IRequestHandler<KarcisGetQuery, KarcisType>
{
    private readonly IKarcisRepo _karcisRepo;

    public KarcisGetHandler(IKarcisRepo karcisRepo)
    {
        _karcisRepo = karcisRepo;
    }

    public Task<KarcisType> Handle(KarcisGetQuery request, CancellationToken cancellationToken)
    {
        var karcis = _karcisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"karcis {request.KarcisId} not found")
            );

        return Task.FromResult( karcis);
    }
}
