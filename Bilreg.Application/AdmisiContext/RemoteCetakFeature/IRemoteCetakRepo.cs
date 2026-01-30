using Bilreg.Domain.AdmisiContext.RemotCetakFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RemoteCetakFeature;

public interface IRemoteCetakRepo :
    ISaveChange<RemoteCetakType>
{
}