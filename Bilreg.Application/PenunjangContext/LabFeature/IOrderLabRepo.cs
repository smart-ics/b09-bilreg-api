using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PenunjangContext.LabFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PenunjangContext.LabFeature;

public interface IOrderLabRepo : 
    ISaveChange<OrderLabModel>,
    ILoadEntity<OrderLabModel, IOrderLabKey>,
    ILoadEntity<OrderLabModel, ITindakanKey>,
    IListData<OrderLabModel, Periode>,
    IDeleteEntity<IOrderLabKey>
{
}

