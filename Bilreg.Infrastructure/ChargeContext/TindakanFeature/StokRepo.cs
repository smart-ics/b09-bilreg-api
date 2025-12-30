using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;

namespace Bilreg.Infrastructure.ChargeContext.TindakanFeature;

public class StokRepo : IStokRepo
{
    private readonly IStokRepo _stokRepo;

    public StokRepo(IStokRepo stokRepo)
    {
        _stokRepo = stokRepo;
    }

    public IEnumerable<StokView> ListData(ILayananKey filter1, string filter2)
    {
        return filter2.Length >= 3 
            ? _stokRepo.ListData(filter1, filter2)
            : [];
    }
}