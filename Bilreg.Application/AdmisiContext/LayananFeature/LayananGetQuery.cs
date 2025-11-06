using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature;

public record LayananGetQuery(string LayananId) : IRequest<LayananGetResponse>, ILayananKey;
public record LayananGetResponse(
    string LayananId,
    string LayananName,
    bool IsAktif,
    string InstalasiId,
    string InstalasiName);

public class LayananGetHandler : IRequestHandler<LayananGetQuery, LayananGetResponse>
{
    private readonly ILayananRepo _layananRepo;

    public LayananGetHandler(ILayananRepo layananRepo)
    {
        _layananRepo = layananRepo;
    }

    public Task<LayananGetResponse> Handle(LayananGetQuery request, CancellationToken cancellationToken)
    {
        var lyn = _layananRepo.LoadEntity(LayananType.Key(request.LayananId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan {request.LayananId} not found"));
        
        
        
        var response = new LayananGetResponse(lyn.LayananId, lyn.LayananName,
            lyn.IsAktif, lyn.Instalasi.InstalasiId, lyn.Instalasi.InstalasiName);
        return Task.FromResult(response);
    }
}