using Bilreg.Domain.AdmPasienContext.PendidikanDkAgg;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmPasienContext.PendidikanDkAgg;

public interface IPendidikanDkDal:
    IInsert<PendidikanDkModel>,
    IUpdate<PendidikanDkModel>,
    IDelete<IPendidikanDkKey>,
    IGetData<PendidikanDkModel, IPendidikanDkKey>,
    IListData<PendidikanDkModel>
{
    
}