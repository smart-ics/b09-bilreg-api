using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisFeature;

public interface IPetugasMedisRepo :
    ISaveChange<PetugasMedisType>,
    ILoadEntity<PetugasMedisType, IPetugasMedisKey>,
    IDeleteEntity<IPetugasMedisKey>,
    IListData<PetugasMedisView, ISatTugasKey>
{
}

public record PetugasMedisView(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, SmfType Smf,
    IEnumerable<PetugasMedisSatTugasType> ListSatTugas);