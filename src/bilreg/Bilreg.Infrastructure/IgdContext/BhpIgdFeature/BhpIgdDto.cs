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
    string? TrsBillId,
    bool IsPosted,
    DateTime? PostedDate,
    string CrtUser,
    DateTime CrtDate)
{
    public BhpIgdDto() : this("", "", "", "", "", 0, 0, null, false, null, "", default) { }

    public static BhpIgdDto FromModel(BhpIgdModel model)
        => new(
            BhpIgdId: model.BhpIgdId,
            IgdVisitId: model.IgdVisitId,
            RegId: model.RegId,
            BhpItemId: model.BhpItemId,
            BhpItemName: model.BhpItemName,
            Qty: model.Qty,
            Price: model.Price,
            TrsBillId: model.TrsBillId,
            IsPosted: model.IsPosted,
            PostedDate: model.PostedDate,
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
            audit: new AuditInfoType(CrtUser, CrtDate),
            trsBillId: TrsBillId ?? string.Empty,
            isPosted: IsPosted,
            postedDate: PostedDate);

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
            CreatedDateTime: CrtDate,
            TrsBillId: TrsBillId ?? string.Empty,
            IsPosted: IsPosted,
            PostedDate: PostedDate);
}
