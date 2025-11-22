using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Infrastructure.AdmisiContext.PpaFeature;

namespace Bilreg.Infrastructure.ParamContext;

public class GetSatuanTugasMedisService : IGetSatuanTugasMedisService
{
    private readonly IParamSistemDal _paramDal;
    private readonly ISatTugasDal _satTugasDal;
    private const string SAT_TUGAS_MEDIS_KEY = "SIS_XXXXXX_SAT_MED";
    public GetSatuanTugasMedisService(IParamSistemDal paramDal,
        ISatTugasDal satTugasDal)
    {
        _paramDal = paramDal;
        _satTugasDal = satTugasDal;
    }

    public SatTugasType Execute()
    {
        var satTugasParam = _paramDal.GetData(SAT_TUGAS_MEDIS_KEY)?.Value ?? string.Empty;
        var satTugas = _satTugasDal.GetData(SatTugasType.Key(satTugasParam)) ??
            throw new KeyNotFoundException("Satuan tugas medis not found");
        return satTugas.ToModel();
    }
}
