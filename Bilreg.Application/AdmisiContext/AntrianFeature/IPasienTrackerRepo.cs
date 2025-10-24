using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public interface IPasienTrackerRepo :
    ISaveChange<PasienTrackerModel>,
    ILoadEntity<PasienTrackerModel, IPasienTrackerKey>,
    IDeleteEntity<IPasienTrackerKey>
{
    
}