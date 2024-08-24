using Bilreg.Domain.PasienContext.StatusSosialSub.PendidikanDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.StatusSosialSub.PendidikanDkAgg;

public interface IPendidikanDkDal:
    IInsert<PendidikanDkModel>,
    IUpdate<PendidikanDkModel>,
    IDelete<IPendidikanDkKey>,
    IGetData<PendidikanDkModel, IPendidikanDkKey>,
    IListData<PendidikanDkModel>
{
    
}