using Bilreg.Application.Helpers;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.PasienFeature;

public interface IPasienRepo : 
    ISaveChange<PasienModel, IPasienKey>,
    ILoadEntity<PasienModel, IPasienKey>,
    IDeleteEntity<IPasienKey>
{
}