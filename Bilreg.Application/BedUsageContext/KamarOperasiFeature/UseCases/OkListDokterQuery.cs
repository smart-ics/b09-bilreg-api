using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListDokterQuery() : IRequest<IEnumerable<OkListDokterResponse>>;

public record OkListDokterResponse(string DokterId, string DokterName);

public class OkListDokterQueryHandler : IRequestHandler<OkListDokterQuery, IEnumerable<OkListDokterResponse>>
{
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    private readonly IPetugasMedisRepo _petugasMedisRepo;

    public static readonly IEnumerable<string> GROUP_SPESIALIS_IDS = new string[] { "BDH", "OBG" };

    public OkListDokterQueryHandler(IGetSatuanTugasMedisService getSatTugasMedisSvc,
        IPetugasMedisRepo petugasMedisRepo)
    {
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
        _petugasMedisRepo = petugasMedisRepo;
    }

    public Task<IEnumerable<OkListDokterResponse>> Handle(OkListDokterQuery request, CancellationToken cancellationToken)
    {
        var satTgsMed = _getSatTugasMedisSvc.Execute();

        var listFilter = new List<IGroupSpesialisKey>();
        foreach (var groupSpesialisId in GROUP_SPESIALIS_IDS)
        {
            var groupSpesialisKey = GroupSpesialisType.Key(groupSpesialisId);
            listFilter.Add(groupSpesialisKey);
        }
        var listPtgMedis = _petugasMedisRepo.ListData(satTgsMed, listFilter);
        var result = listPtgMedis.Select(x => new OkListDokterResponse(x.PetugasMedisId, x.PetugasMedisName));
        return Task.FromResult(result);
    }
}
