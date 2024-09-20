using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;

namespace Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;

public class KarcisFactory : AggFactory<KarcisModel, IKarcisKey>
{
    private readonly IKarcisDal _karcisDal;
    private readonly IKarcisKomponenDal _karcisKomponenDal;
    private readonly IKarcisLayananDal _karcisLayananDal;

    public KarcisFactory(IKarcisDal karcisDal, 
        IKarcisKomponenDal karcisKomponenDal, 
        IKarcisLayananDal karcisLayananDal)
    {
        _karcisDal = karcisDal;
        _karcisKomponenDal = karcisKomponenDal;
        _karcisLayananDal = karcisLayananDal;
    }

    protected override KarcisModel LoadAggregate(IKarcisKey key)
    {
        var karcis = _karcisDal.GetData(key)
            ?? throw new KeyNotFoundException($"Karcis with id: {key.KarcisId} not found");

        var listKomponen = _karcisKomponenDal.ListData(key)
            ?? new List<KarcisKomponenModel>();
        karcis.Attach(listKomponen);
        karcis.SetNilai();

        var listLayanan = _karcisLayananDal.ListData(key)
            ?? new List<KarcisLayananModel>();
        karcis.Attach(listLayanan);

        return karcis;
    }
}