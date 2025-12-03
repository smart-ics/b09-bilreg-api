using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature;

public record LayananListByInstalasiDkQuery(string InstalasiDkId) : IRequest<IEnumerable<LayananListByInstalasiDkResponse>>, IInstalasiDkKey;

public record LayananListByInstalasiDkResponse(
    string LayananId,
    string LayananName,
    bool IsAktif,
    string InstalasiId,
    string InstalasiName
    );

public class LayananListByInstalasiDkHandler : IRequestHandler<LayananListByInstalasiDkQuery, IEnumerable<LayananListByInstalasiDkResponse>>
{
    private readonly ILayananRepo _lynRepo;

    public LayananListByInstalasiDkHandler(ILayananRepo lynRepo)
    {
        _lynRepo = lynRepo;
    }

    public Task<IEnumerable<LayananListByInstalasiDkResponse>> Handle(LayananListByInstalasiDkQuery request, CancellationToken cancellationToken)
    {
        var lyns = _lynRepo.ListData(request)?.ToList() ?? [];
        var result = lyns
            .Select(x => new LayananListByInstalasiDkResponse(
                x.LayananId,
                x.LayananName,
                x.IsAktif,
                x.Instalasi.InstalasiId,
                x.Instalasi.InstalasiName
            ));
        
        return Task.FromResult(result);     
    }
}
