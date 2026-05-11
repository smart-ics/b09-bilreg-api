using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature;

public interface ITindakanIgdRepo :
    ISaveChange<TindakanIgdModel>,
    ILoadEntity<TindakanIgdModel, ITindakanIgdKey>,
    IListData<TindakanIgdView, IIgdVisitKey>
{
    bool AnyForVisit(IIgdVisitKey visit);
}

public record TindakanIgdView(
    string TindakanIgdId,
    string IgdVisitId,
    string RegId,
    string TarifId,
    string TarifName,
    int Qty,
    decimal Price,
    decimal Subtotal,
    DateTime CreatedDateTime);
