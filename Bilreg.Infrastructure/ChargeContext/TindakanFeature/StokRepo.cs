using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class StokRepo : IStokRepo
{
    private readonly IStokDal _stokDal;

    public StokRepo(IStokDal stokDal)
    {
        _stokDal = stokDal;
    }

    public IEnumerable<StokView> ListData(ILayananKey filter1, string filter2)
    {
        return filter2.Length >= 3 
            ? _stokDal.ListData(filter1, filter2)
            : [];
    }
}

