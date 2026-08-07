using Bilreg.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BrgContext.StandardFeature;

public interface IFormulariumRepo :
    ISaveChange<FormulariumType>,
    ILoadEntity<FormulariumType, IFormulariumKey>,
    IDeleteEntity<IFormulariumKey>,
    IListData<FormulariumType>
{
}