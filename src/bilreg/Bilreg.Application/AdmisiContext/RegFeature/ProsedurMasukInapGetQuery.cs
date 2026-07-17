using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record ProsedurMasukInapGetQuery(string ProsedurMasukInapId) 
    : IRequest<ProsedurMasukInapGetResponse>, IProsedurMasukInapKey;

public record ProsedurMasukInapGetResponse(
    string ProsedurMasukInapId, string ProsedurMasukInapName); 

public class ProsedurMasukInapGethandler : IRequestHandler<ProsedurMasukInapGetQuery, ProsedurMasukInapGetResponse>
{
    private readonly IProsedureMasukInapRepo _prosedurMasukInapRepo;

    public ProsedurMasukInapGethandler(IProsedureMasukInapRepo prosedurMasukInapRepo)
    {
        _prosedurMasukInapRepo = prosedurMasukInapRepo;
    }

    public Task<ProsedurMasukInapGetResponse> Handle(ProsedurMasukInapGetQuery request, CancellationToken cancellationToken)
    {
        var prosedurMasukInap = _prosedurMasukInapRepo.LoadEntity(request)
            .GetValueOrThrow($"Prosedur Masuk Inap {request.ProsedurMasukInapId} not found");
        var result = new ProsedurMasukInapGetResponse(
            prosedurMasukInap.ProsedurMasukInapId, 
            prosedurMasukInap.ProsedurMasukInapName);

        return Task.FromResult(result);
    }
}