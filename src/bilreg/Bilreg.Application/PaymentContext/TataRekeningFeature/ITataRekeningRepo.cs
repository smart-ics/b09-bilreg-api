using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

public interface ITataRekeningRepo :
    ISaveChange<TataRekeningModel>,
    ILoadEntity<TataRekeningModel, IRegKey>,
    IDeleteEntity<IRegKey>
{
}
