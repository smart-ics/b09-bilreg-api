using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegSub;

public interface IRegAktifRepo :
    ISaveChange<RegAktifModel>,
    ILoadEntity<RegAktifModel, IRegKey>,
    IDelete<IRegKey>,
    IListData<RegAktifModel, Periode>
{
    
}