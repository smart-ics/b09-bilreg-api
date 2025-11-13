using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IJenisOperasiRepo :
    ISaveChange<JenisOperasiType>,
    IDeleteEntity<IJenisOperasiKey>,
    IListData<JenisOperasiType>
{
}