using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using MediatR;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;

public record OkListDokterQuery() : IRequest<IEnumerable<OkListDokterResponse>>;

public record OkListDokterResponse(string DokterId, string DokterName);

public class OkListDokterQueryHandler : IRequestHandler<OkListDokterQuery, IEnumerable<OkListDokterResponse>>
{
    private readonly IGetSatuanTugasMedisService _getSatTugasMedisSvc;
    private readonly IPpaRepo _ppaRepo;

    public static readonly IEnumerable<string> GROUP_SPESIALIS_IDS = new string[] { "BDH", "OBG" };

    public OkListDokterQueryHandler(IGetSatuanTugasMedisService getSatTugasMedisSvc,
        IPpaRepo ppaRepo)
    {
        _getSatTugasMedisSvc = getSatTugasMedisSvc;
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<OkListDokterResponse>> Handle(OkListDokterQuery request, CancellationToken cancellationToken)
    {
        // var satTgsMed = _getSatTugasMedisSvc.Execute();
        //
        // var listFilter = new List<IGroupSpesialisKey>();
        // foreach (var groupSpesialisId in GROUP_SPESIALIS_IDS)
        // {
        //     var groupSpesialisKey = GroupSpesialisType.Key(groupSpesialisId);
        //     listFilter.Add(groupSpesialisKey);
        // }
        // var listPtgMedis = _ppaRepo.ListData(satTgsMed, listFilter);
        // var result = listPtgMedis.Select(x => new OkListDokterResponse(x.PpaId, x.PpaName));
        // return Task.FromResult(result);
        throw new NotImplementedException();
    }
}
