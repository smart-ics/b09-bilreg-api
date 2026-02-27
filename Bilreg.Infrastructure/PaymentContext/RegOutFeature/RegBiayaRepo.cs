using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public class RegBiayaRepo : IRegBiayaRepo
{
    private readonly IRegBiayaDal _regBiayaDal;
    public RegBiayaRepo(IRegBiayaDal regBiayaDal)
    {
        _regBiayaDal = regBiayaDal;
    }
    public IEnumerable<RegBiayaType> ListData(IRegKey key)
    {
        var listDto = _regBiayaDal.ListData(key)?.ToList() ?? [];
        var result = listDto.SelectMany(x => x.ToModels()).ToList(); 
        return result;
    }
}
