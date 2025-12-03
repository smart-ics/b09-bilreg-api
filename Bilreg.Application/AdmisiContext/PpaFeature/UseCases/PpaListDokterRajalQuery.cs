using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.PpaFeature.UseCases;

public record PpaListDokterRajalQuery() : IRequest<IEnumerable<PpaListDokterRajalResponse>>;

public record PpaListDokterRajalResponse(
    string GroupSpesialisId,
    string GroupSpesialisName,
    IEnumerable<PpaListDokterRajalResponseDokter> ListDokter);

public record PpaListDokterRajalResponseDokter(
    string DokterId,
    string DokterName);

public class PpaListDokterRajalHandler : IRequestHandler<PpaListDokterRajalQuery, IEnumerable<PpaListDokterRajalResponse>>
{
    private readonly IPpaRepo _ppaRepo;
    private readonly IGroupSpesialisRepo _groupSpesialisRepo;
    private readonly ILayananRepo _layananRepo;
    public PpaListDokterRajalHandler(IPpaRepo ppaRepo,
        IGroupSpesialisRepo groupSpesialisRepo,
        ILayananRepo layananRepo)
    {
        _ppaRepo = ppaRepo;
        _groupSpesialisRepo = groupSpesialisRepo;
        _layananRepo = layananRepo;
    }

    public Task<IEnumerable<PpaListDokterRajalResponse>> Handle(
        PpaListDokterRajalQuery request, CancellationToken cancellationToken)
    {
        var listPpa = _ppaRepo.ListData(ProfesiType.Dokter)?.ToList() ?? [];
        var listGroupSpesialis = _groupSpesialisRepo.ListData()?.ToList() ?? [];

        var result =
            listGroupSpesialis
                .GroupJoin(
                    listPpa,
                    g => g.GroupSpesialisId,
                    d => d.GroupSpesialis.GroupSpesialisId,
                    (g, dokterGroup) => new PpaListDokterRajalResponse(
                        g.GroupSpesialisId,
                        g.GroupSpesialisName,
                        dokterGroup.Select(d => new PpaListDokterRajalResponseDokter(
                            d.PpaId,
                            d.PpaName
                        ))
                    )
                );





        return Task.FromResult(result.AsEnumerable());
    }
}
