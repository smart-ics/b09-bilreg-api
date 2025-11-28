using MediatR;

namespace Bilreg.Application.BillContext.BedUsageFeature.KamarAgg;

public record KelasListQuery() : IRequest<IEnumerable<KelasListResponse>>;

public record KelasListResponse(string KelasId, string KelasName, string KelasDkId, string KelasDkName);

public class KelasListHandler : IRequestHandler<KelasListQuery, IEnumerable<KelasListResponse>>
{
    private readonly IKelasRepo _kelasRepo;

    public KelasListHandler(IKelasRepo kelasRepo)
    {
        _kelasRepo = kelasRepo;
    }

    public Task<IEnumerable<KelasListResponse>> Handle(KelasListQuery request, CancellationToken cancellationToken)
    {
        var listKelas = _kelasRepo.ListData()?
            .Where(x => x.IsAktif == true)?.ToList() ?? [];
        var result = listKelas.Select(x => new KelasListResponse(x.KelasId, x.KelasName, x.KelasDk.KelasDkId, x.KelasDk.KelasDkName));
        return Task.FromResult(result);
    }
}
