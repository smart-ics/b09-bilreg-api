using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.RegOutFeature;

namespace Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;

public interface IRegHutangRepo
{
    IEnumerable<RegHutangType> ListData(IPasienKey key, DateOnly businessDate);
}
