using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PetugasMedisSub;

public interface IPetugasMedisRepo :
    ISaveChange<PetugasMedisType>,
    ILoadEntity<PetugasMedisType, IPetugasMedisKey>,
    IDeleteEntity<IPetugasMedisKey>,
    IListData<PetugasMedisView>
{
}

public record PetugasMedisView(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, SmfType Smf);