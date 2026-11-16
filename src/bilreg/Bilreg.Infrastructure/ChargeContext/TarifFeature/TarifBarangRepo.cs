using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.TarifFeature;

public class TarifBarangRepo : ITarifBarangRepo
{
    private readonly ITarifBarangDal _tarifBarangDal;

    public TarifBarangRepo(ITarifBarangDal tarifBarangDal)
    {
        _tarifBarangDal = tarifBarangDal;
    }

    public IEnumerable<TarifBarangType> ListData(ITarifKey filter)
    {
        var listDto = _tarifBarangDal.ListData(filter)?.ToList() ?? [];
        return listDto.Select(x => x.ToModel());
    }
}
