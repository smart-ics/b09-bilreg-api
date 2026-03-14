using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public class RegPembayaranRepo : IRegPembayaranRepo
{
    private readonly IRegPembayaranDal _regPembayaranDal;
    public RegPembayaranRepo(IRegPembayaranDal regPembayaranDal)
    {
        _regPembayaranDal = regPembayaranDal;
    }
    public IEnumerable<RegPembayaranType> ListData(IRegKey key)
    {
        var listDto = _regPembayaranDal.ListData(key)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
