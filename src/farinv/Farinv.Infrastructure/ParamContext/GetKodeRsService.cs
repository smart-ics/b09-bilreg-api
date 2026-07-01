using Farinv.Application.ParamContext.ParamSistemAgg;
using Farinv.Domain.Shared.Param;
using Farinv.Infrastructure.Helpers;

namespace Farinv.Infrastructure.ParamContext;

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
        var encrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value;
        if (encrypted is null)
            return "1000000";

        var result = X1EncryptionHelper.DecodingNeo(encrypted);
        return result;
    }
}