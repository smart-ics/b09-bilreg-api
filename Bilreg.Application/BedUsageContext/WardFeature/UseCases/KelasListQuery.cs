using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.WardFeature.UseCases;

public record KelasListQuery() : IRequest<IEnumerable<KelasListResponse>>;

public record KelasListResponse(
    string KelasId,
    string KelasName,
    bool IsAktif,
    KelasDkType KelasDk
    );
public class KelasListHandler : IRequestHandler<KelasListQuery, IEnumerable<KelasListResponse>>
{
    private readonly IKelasRepo _klsRepo;

    public KelasListHandler(IKelasRepo klsRepo)
    {
        _klsRepo = klsRepo;
    }

    public Task<IEnumerable<KelasListResponse>> Handle(KelasListQuery request, CancellationToken cancellationToken)
    {
        var listKls = _klsRepo.ListData()?.ToList() ?? [];
        var result = listKls.Select(x => new KelasListResponse(x.KelasId, x.KelasName, x.IsAktif, x.KelasDk));

        return Task.FromResult(result);
    }
}
