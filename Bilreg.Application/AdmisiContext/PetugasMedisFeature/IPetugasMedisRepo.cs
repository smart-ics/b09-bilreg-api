using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public interface IPetugasMedisRepo :
    ISaveChange<PetugasMedisType>,
    ILoadEntity<PetugasMedisType, IPetugasMedisKey>,
    IDeleteEntity<IPetugasMedisKey>,
    IListData<PetugasMedisView, ISatTugasKey>,
    IListData<PetugasMedisLayananView, ISatTugasKey, IInstalasiDkKey>,
    IListData<PetugasMedisType, ISatTugasKey, IEnumerable<IGroupSpesialisKey>>
{
}

public record PetugasMedisView(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, SmfType Smf,
    IEnumerable<PetugasMedisSatTugasType> ListSatTugas);