using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IPpaRepo :
    ISaveChange<PpaType>,
    ILoadEntity<PpaType, IPpaKey>,
    IDeleteEntity<IPpaKey>,
    IListData<PpaLayananView, IProfesiKey, IEnumerable<ILayananKey>>,
    IListData<PpaView, IProfesiKey>
{
}

public record PpaView(string PpaId, 
    string PpaName, string NamaSingkat, SmfType Smf, GroupSpesialisType GroupSpesialis);
    
public record PpaLayananView(string PpaId,
    string PpaName, LayananReff Layanan);