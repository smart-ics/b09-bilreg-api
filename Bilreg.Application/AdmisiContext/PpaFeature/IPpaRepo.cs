using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IPpaRepo :
    ISaveChange<PpaType>,
    ILoadEntity<PpaType, IPpaKey>,
    IDeleteEntity<IPpaKey>,
    IListData<PpaView>,
    IListData<PpaView, IProfesiKey>
{
}

public record PpaView(string PetugasMedisId, 
    string PetugasMedisName, string NamaSingkat, SmfType Smf,
    IEnumerable<PpaSatTugasType> ListSatTugas);
    
    
public record PpaLayananView(
    string PpaId,
    string PpaName,
    LayananReff Layanan,
    GroupSpesialisType GroupSpesialis);