using MediatR;

namespace Farinv.Application.BrgContext.StandardFeature.UseCases;

public record OriginalListQuery() : IRequest<IEnumerable<OriginalListResponse>>;

public record OriginalListResponse(string OriginalId, string OriginalName);

public class OriginalListHandler 
    : IRequestHandler<OriginalListQuery, IEnumerable<OriginalListResponse>>
{
    private readonly IOriginalRepo _originalRepo;

    public OriginalListHandler(IOriginalRepo originalRepo)
    {
        _originalRepo = originalRepo; 
    }

    public Task<IEnumerable<OriginalListResponse>> Handle(OriginalListQuery request, 
        CancellationToken cancellationToken)
    {
        var listOriginal = _originalRepo.ListData()?.ToList() ?? [];
        var response = listOriginal
            .OrderBy(x => x.OriginalName)
            .Select(x => new OriginalListResponse(x.OriginalId, x.OriginalName));

        return Task.FromResult(response);
    }
}