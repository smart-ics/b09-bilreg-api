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

public record PpaView(string PpaId, 
    string PpaName, string NamaSingkat, SmfType Smf);
    
    
public record PpaLayananView(
    string PpaId,
    string PpaName,
    LayananReff Layanan,
    GroupSpesialisType GroupSpesialis);