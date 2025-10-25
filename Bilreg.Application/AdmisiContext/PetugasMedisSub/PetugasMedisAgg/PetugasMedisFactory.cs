using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Nuna.Lib.CleanArchHelper;
using System.Xml.Serialization;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;

public class PetugasMedisFactory : AggFactory<PetugasMedisType, IPetugasMedisKey>
{
    private readonly IPetugasMedisDal _petugasMedisDal;
    private readonly IPetugasMedisLayananDal _petugasMedisLayananDal;
    private readonly IPetugasMedisSatTugasDal _petugasMedisSatTugasDal;

    public PetugasMedisFactory(IPetugasMedisDal petugasMedisDal,
        IPetugasMedisLayananDal petugasMedisLayananDal,
        IPetugasMedisSatTugasDal petugasMedisSatTugasDal)
    {
        _petugasMedisDal = petugasMedisDal;
        _petugasMedisLayananDal = petugasMedisLayananDal;
        _petugasMedisSatTugasDal = petugasMedisSatTugasDal;
    }

    protected override PetugasMedisType LoadAggregate(IPetugasMedisKey key)
    {
        var medis = _petugasMedisDal.GetData(key)
            .Match(
                some => some,
                () => throw new KeyNotFoundException($"{key.PetugasMedisId} not found")
            );

        var listLayanan = _petugasMedisLayananDal.ListData(key)
            .Match(
                some => some,
                () => new List<PetugasMedisLayananType>()
            );
        
        var listSatTugas = _petugasMedisSatTugasDal.ListData(key)
            .Match(
                some => some,
                () => new List<PetugasMedisSatTugasType>()
            );
        
        var result = new PetugasMedisType(medis.PetugasMedisId, medis.PetugasMedisName, 
            medis.NamaSingkat, SmfType.Default,
            listLayanan, listSatTugas);

        return result;
    }
}