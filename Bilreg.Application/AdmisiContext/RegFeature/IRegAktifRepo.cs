using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IRegAktifRepo :
    ISaveChange<RegAktifModel>,
    ILoadEntity<RegAktifModel, IRegKey>,
    IDelete<IRegKey>,
    IListData<RegAktifModel, ILayananKey>
{
    
}