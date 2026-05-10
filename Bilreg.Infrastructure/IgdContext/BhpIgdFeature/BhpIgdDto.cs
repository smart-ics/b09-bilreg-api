using Bilreg.Application.IgdContext.BhpIgdFeature;
using Bilreg.Domain.IgdContext.BhpIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.IgdContext.BhpIgdFeature;

public record BhpIgdDto(
    string BhpIgdId,
    string IgdVisitId,
    string RegId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string CrtUser,
    DateTime CrtDate)
{
    public static BhpIgdDto FromModel(BhpIgdModel model)
        => new(
            BhpIgdId: model.BhpIgdId,
            IgdVisitId: model.IgdVisitId,
            RegId: model.RegId,
            BhpItemId: model.BhpItemId,
            BhpItemName: model.BhpItemName,
            Qty: model.Qty,
            Price: model.Price,
            CrtUser: model.Audit.UserId,
            CrtDate: model.Audit.Timestamp);

    public BhpIgdModel ToModel()
        => new(
            bhpIgdId: BhpIgdId,
            igdVisitId: IgdVisitId,
            regId: RegId,
            bhpItemId: BhpItemId,
            bhpItemName: BhpItemName,
            qty: Qty,
            price: Price,
            audit: new AuditInfoType(CrtUser, CrtDate));

    public BhpIgdView ToView()
        => new(
            BhpIgdId: BhpIgdId,
            IgdVisitId: IgdVisitId,
            RegId: RegId,
            BhpItemId: BhpItemId,
            BhpItemName: BhpItemName,
            Qty: Qty,
            Price: Price,
            Subtotal: Qty * Price,
            CreatedDateTime: CrtDate);
}
