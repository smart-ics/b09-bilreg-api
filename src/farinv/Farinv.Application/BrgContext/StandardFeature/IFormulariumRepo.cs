using Farinv.Domain.BrgContext.StandardFeature;
using Nuna.Lib.DataAccessHelper;

namespace Farinv.Application.BrgContext.StandardFeature;

public interface IFormulariumRepo :
    ISaveChange<FormulariumType>,
    ILoadEntity<FormulariumType, IFormulariumKey>,
    IDeleteEntity<IFormulariumKey>,
    IListData<FormulariumType>
{
}