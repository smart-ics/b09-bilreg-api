using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.WardFeature;

public interface IKamarRepo :
    ISaveChange<KamarType>,
    ILoadEntity<KamarType, IKamarKey>,
    IDeleteEntity<IKamarKey>,
    IListData<KamarType>,
    IListData<KamarType, IBangsalKey>
{
}

