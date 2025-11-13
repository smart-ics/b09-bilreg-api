using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IJenisOperasiRepo :
    ISaveChange<JenisOperasiType>,
    ILoadEntity<JenisOperasiType, IJenisOperasiKey>,
    IDeleteEntity<IJenisOperasiKey>,
    IListData<JenisOperasiType>
{
}