using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature;

public interface IRuangRepo :
    ISaveChange<RuangType>,
    ILoadEntity<RuangType, IRuangKey>,
    IDeleteEntity<IRuangKey>,
    IListData<RuangType>
{
}
