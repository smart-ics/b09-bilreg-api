using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public class PolisFactory : AggFactory<PolisModel, IPolisKey>
{
    private readonly IPolisDal _polisDal;
    private readonly IPolisCoverDal _polisCoverDal;

    public PolisFactory(IPolisDal polisDal, IPolisCoverDal polisCoverDal)
    {
        _polisDal = polisDal;
        _polisCoverDal = polisCoverDal;
    }

    protected override PolisModel LoadAggregate(IPolisKey key)
    {
        var polis = _polisDal.GetData(key)
            ?? throw new KeyNotFoundException($"{key.PolisId} not found");
        var listLayanan = _polisCoverDal.ListData(key)
            ?? new List<PolisCoverModel>();
        
        var result = new PolisBuilder(polis);
        result.Attach(listLayanan);
        
        return polis;
    }
}