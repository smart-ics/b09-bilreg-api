using Bilreg.Domain.AdmisiContext.BookingFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IRuangRepo :
    ISaveChange<RuangType>,
    ILoadEntity<RuangType, IRuangKey>,
    IDeleteEntity<IRuangKey>,
    IListData<RuangType>
{
}
