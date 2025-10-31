using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Domain.AdmisiContext.RujukanSub;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.RujukanSub.CaraMasukDkAgg;

public class CaraMasukDkRepo : ICaraMasukDkRepo
{
    private readonly ICaraMasukDkDal _caraMasukDkDal;

    public CaraMasukDkRepo(ICaraMasukDkDal caraMasukDkDal)
    {
        _caraMasukDkDal = caraMasukDkDal;
    }

    public MayBe<CaraMasukDkType> LoadEntity(ICaraMasukDkKey key)
    {
        var caraMasuk = _caraMasukDkDal.GetData(key);
        return MayBe.From(caraMasuk!);
    }

    public IEnumerable<CaraMasukDkType> ListData()
    {
        var listData = _caraMasukDkDal.ListData();
        return listData;
    }
}