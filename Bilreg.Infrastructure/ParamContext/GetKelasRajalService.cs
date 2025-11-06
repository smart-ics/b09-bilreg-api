using Bilreg.Application.BillContext.BedUsageFeature;
using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.BedUsageFeature;
using Bilreg.Infrastructure.Helpers;

namespace Bilreg.Infrastructure.ParamContext;

public class GetKelasRajalService : IGetKelasRajalService
{
    private readonly IParamSistemDal _paramSistemDal;
    private readonly IKelasRepo _kelasRepo;
    private const string KELAS_RAJAL_PARAM_KEY = "SIS_XXXXXX_KELAS_RJ";

    public GetKelasRajalService(IParamSistemDal paramSistemDal, IKelasRepo kelasRepo)
    {
        _paramSistemDal = paramSistemDal;
        _kelasRepo = kelasRepo;
    }

    public KelasType Execute()
    {
        var kelasId = _paramSistemDal.GetData(KELAS_RAJAL_PARAM_KEY)?.Value ?? string.Empty;
        var kelas = _kelasRepo.LoadEntity(KelasType.Key(kelasId))
            .GetValueOrThrow("Kelas tidak ditemukan");
        return kelas;
    }    
}