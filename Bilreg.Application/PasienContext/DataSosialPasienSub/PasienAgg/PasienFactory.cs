using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;

namespace Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienFactory: AggFactory<PasienModel, IPasienKey>
{
    private readonly IPasienDal _pasienDal;
    private readonly IPasienLogDal _pasienLogDal;

    public PasienFactory(IPasienDal pasienDal, IPasienLogDal pasienLogDal)
    {
        _pasienDal = pasienDal;
        _pasienLogDal = pasienLogDal;
    }

    protected override PasienModel LoadAggregate(IPasienKey key)
    {
        var pasien = _pasienDal.GetData(key)
            ?? throw new KeyNotFoundException($"Pasien with id: {key.PasienId} not found");

        var listLog = _pasienLogDal.ListData(key)
            ?? new List<PasienLogModel>();
        
        pasien.Attach(listLog);
        return pasien;
    }
}