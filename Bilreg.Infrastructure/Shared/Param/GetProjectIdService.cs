using Bilreg.Application.Shared.Param.ParamSistemAgg;
using Bilreg.Domain.Shared.Param;

namespace Bilreg.Infrastructure.Shared.Param;

public class GetProjectIdService : IGetProjectIdService
{
    private readonly IParamSistemDal _paramSistemDal;
    private const string PROJECT_ID_PARAM_KEY = "RS__XXXXXX_PROJECT_ID_";

    public GetProjectIdService(IParamSistemDal paramSistemDal)
    {
        _paramSistemDal = paramSistemDal;
    }

    public string Execute()
    {
        var result = _paramSistemDal.GetData(PROJECT_ID_PARAM_KEY)?.Value;
        return result;
    }
}
