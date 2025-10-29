using Bilreg.Application.ParamContext.ParamSistemAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;

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
        var encrypted = _paramSistemDal.GetData(KODE_RS_PARAM_KEY)?.Value;
        if (encrypted is null)
            return "1000000";

        var result = X1EncryptionHelper.DecodingNeo(encrypted);
        return result;
    }
}