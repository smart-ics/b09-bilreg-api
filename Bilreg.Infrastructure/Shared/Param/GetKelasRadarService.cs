using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Infrastructure.Shared.Param;

public class GetKelasRadarService : IGetKelasRadarService
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly IKelasRepo _kelasRepo;
    private const string KELAS_RADAR_PARAM_KEY = "SIS_XXXXXX_KELAS_RD";
    public GetKelasRadarService(IParamSistemDal paramSistemDal, IKelasRepo kelasRepo)
    {
        _paramSistemDal = paramSistemDal;
        _kelasRepo = kelasRepo;
    }


    public KelasType Execute()
    {
        var kelasId = _paramSistemDal.GetData(KELAS_RADAR_PARAM_KEY)?.Value ?? string.Empty;
        var kelas = _kelasRepo.LoadEntity(KelasType.Key(kelasId))
            .GetValueOrThrow("Kelas tidak ditemukan");
        return kelas;
    }
}
