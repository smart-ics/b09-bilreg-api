using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Infrastructure.ParamContext;

public class GetKodeRsService : IGetKodeRsService
{
    private readonly IParamSistemDal _paramSistemDal;
    private const string KODE_RS_PARAM_KEY = "RS__XXXXXX_KODE";

    public GetKodeRsService(IParamSistemDal paramSistemDal)
    {
        _paramSistemDal = paramSistemDal;
    }

    public string Execute()
    {
        var result = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value ?? "0000000";
        return result;
    }
}