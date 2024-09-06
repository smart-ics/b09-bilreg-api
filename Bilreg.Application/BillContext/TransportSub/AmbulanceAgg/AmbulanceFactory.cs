using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.BillContext.TransportSub.AmbulanceAgg;
using Moq;
using Xunit;

namespace Bilreg.Application.BillContext.TransportSub.AmbulanceAgg;

public class AmbulanceFactory: AggFactory<AmbulanceModel, IAmbulanceKey>
{
    private readonly IAmbulanceDal _ambulanceDal;
    private readonly IAmbulanceKomponenDal _ambulanceKomponenDal;

    public AmbulanceFactory(IAmbulanceDal ambulanceDal, IAmbulanceKomponenDal ambulanceKomponenDal)
    {
        _ambulanceDal = ambulanceDal;
        _ambulanceKomponenDal = ambulanceKomponenDal;
    }

    protected override AmbulanceModel LoadAggregate(IAmbulanceKey key)
    {
        var ambulance = _ambulanceDal.GetData(key)
            ?? throw new KeyNotFoundException($"Ambulance with id {key.AmbulanceId} not found");
        
        var listAmbulanceKomponent = _ambulanceKomponenDal.ListData(key)
            ?? new List<AmbulanceKomponenModel>();
        
        ambulance.Attach(listAmbulanceKomponent);
        return ambulance;
    }
}