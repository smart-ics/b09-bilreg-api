using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.PpaFeature;

public interface IPpaRepo :
    ISaveChange<PpaType>,
    ILoadEntity<PpaType, IPpaKey>,
    ILoadEntity<PpaType, IContactFinder>,
    IDeleteEntity<IPpaKey>,
    IListData<PpaLayananView, IProfesiKey, IEnumerable<ILayananKey>>,
    IListData<PpaView, IProfesiKey>,
    IListData<PpaView, IEnumerable<ISatTugasKey>>

{
}

public record PpaView(string PpaId, 
    string PpaName, string NamaSingkat, SmfType Smf, GroupSpesialisType GroupSpesialis);
    
public record PpaLayananView(string PpaId,
    string PpaName, LayananReff Layanan);
    
public interface IContactFinder
{
    JenisContactEnum JenisContact { get; }
    string ContactDetail { get; }
}

public record ContactFinder(JenisContactEnum JenisContact, string ContactDetail) : IContactFinder;
