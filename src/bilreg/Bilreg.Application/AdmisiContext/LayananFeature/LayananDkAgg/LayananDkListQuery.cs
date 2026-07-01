using Bilreg.Application.AdmisiContext.LayananFeature.TipeLayananDkAgg;
using MediatR;

namespace Bilreg.Application.AdmisiContext.LayananFeature.LayananDkAgg;

public record LayananDkListQuery() : IRequest<IEnumerable<LayananDkListResponse>>;
public record LayananDkListResponse(
    string LayananDkId,
    string LayananDkName);
public class LayananDkListHandler : IRequestHandler<LayananDkListQuery, IEnumerable<LayananDkListResponse>>
{
    private readonly ILayananDkRepo _lynDkRepo;

    public LayananDkListHandler(ILayananDkRepo lynDkRepo)
    {
        _lynDkRepo = lynDkRepo;
    }

    public Task<IEnumerable<LayananDkListResponse>> Handle(LayananDkListQuery request, CancellationToken cancellationToken)
    {
        var listLynDk = _lynDkRepo.ListData()?.ToList() ?? [];
        var response = listLynDk
            .OrderBy(x => x.LayananDkName)
            .Select(x => new LayananDkListResponse(x.LayananDkId, x.LayananDkName));
        
        return Task.FromResult(response);
    }
}
