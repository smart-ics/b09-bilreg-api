using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.IgdContext.BhpIgdFeature;

public interface IBhpIgdRepo :
    ISaveChange<BhpIgdModel>,
    ILoadEntity<BhpIgdModel, IBhpIgdKey>,
    IListData<BhpIgdView, IIgdVisitKey>
{
    bool AnyForVisit(IIgdVisitKey visit);
}

public record BhpIgdView(
    string BhpIgdId,
    string IgdVisitId,
    string RegId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    decimal Subtotal,
    DateTime CreatedDateTime);
