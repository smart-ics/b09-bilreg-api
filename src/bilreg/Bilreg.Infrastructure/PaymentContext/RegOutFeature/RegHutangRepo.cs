using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Infrastructure.PaymentContext.RegOutFeature;

public class RegHutangRepo : IRegHutangRepo
{
    private readonly IRegHutangDal _regHutangDal;
    public RegHutangRepo(IRegHutangDal regHutangDal)
    {
        _regHutangDal = regHutangDal;
    }
    public IEnumerable<RegHutangType> ListData(IPasienKey key, DateOnly businessDate)
    {
        var listDto = _regHutangDal.ListData(key, businessDate)?.ToList() ?? [];
        var result = listDto.Select(x => x.ToModel()).ToList();
        return result;
    }
}
