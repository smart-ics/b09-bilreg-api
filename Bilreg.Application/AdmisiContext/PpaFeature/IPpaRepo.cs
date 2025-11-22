using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IPpaRepo :
    ISaveChange<PpaType>,
    ILoadEntity<PpaType, IPpaKey>,
    IDeleteEntity<IPpaKey>,
    IListData<PetugasMedisView, ISatTugasKey>,
    IListData<PpaLayananView, ISatTugasKey, IInstalasiDkKey>,
    IListData<PpaType, ISatTugasKey, IEnumerable<IGroupSpesialisKey>>
{
}

public record PetugasMedisView(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, SmfType Smf,
    IEnumerable<PpaSatTugasType> ListSatTugas);