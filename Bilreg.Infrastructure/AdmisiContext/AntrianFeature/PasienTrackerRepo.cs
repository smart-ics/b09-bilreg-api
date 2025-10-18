using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public class PasienTrackerRepo : IPasienTrackerRepo
{
    private readonly IPasienTrackerDal _pasienTrackerdal;
    private readonly IPasienTrackerEventDal _pasienTrackerEventDal;

    public PasienTrackerRepo(IPasienTrackerDal pasienTrackerdal, 
        IPasienTrackerEventDal pasienTrackerEventDal)
    {
        _pasienTrackerdal = pasienTrackerdal;
        _pasienTrackerEventDal = pasienTrackerEventDal;
    }

    //  TODO: Crete REPO BOOKING
    public void SaveChanges(PasienTrackerModel model)
    {
        throw new NotImplementedException();
    }

    public MayBe<PasienTrackerModel> LoadEntity(IPasienTrackerKey key)
    {
        throw new NotImplementedException();
    }

    public void DeleteEntity(IPasienTrackerKey key)
    {
        throw new NotImplementedException();
    }
}

