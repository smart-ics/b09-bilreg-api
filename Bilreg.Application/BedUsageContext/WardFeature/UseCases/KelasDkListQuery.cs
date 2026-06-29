using MediatR;

namespace Bilreg.Application.BedUsageContext.WardFeature.UseCases;

public record KelasDkListQuery() : IRequest<IEnumerable<KelasDkListResponse>>;

public record KelasDkListResponse(string KelasDkId, string KelasDkName);

public class KelasDkListHandler : IRequestHandler<KelasDkListQuery, IEnumerable<KelasDkListResponse>>
{
    private readonly IKelasDkRepo _kelasDkRepo;

    public KelasDkListHandler(IKelasDkRepo kelasDkRepo)
    {
        _kelasDkRepo = kelasDkRepo;
    }

    public Task<IEnumerable<KelasDkListResponse>> Handle(KelasDkListQuery request, CancellationToken cancellationToken)
    {
        var listData = _kelasDkRepo.ListData() ?? [];
        var result = listData.Select(x =>
        new KelasDkListResponse(x.KelasDkId, x.KelasDkName));

        return Task.FromResult(result);
    }
}
