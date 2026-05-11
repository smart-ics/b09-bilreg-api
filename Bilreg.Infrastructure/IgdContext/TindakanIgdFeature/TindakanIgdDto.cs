using Bilreg.Application.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.IgdContext.TindakanIgdFeature;

public record TindakanIgdDto(
    string TindakanIgdId,
    string IgdVisitId,
    string RegId,
    string TarifId,
    string TarifName,
    int Qty,
    decimal Price,
    string CrtUser,
    DateTime CrtDate)
{
    public static TindakanIgdDto FromModel(TindakanIgdModel model)
        => new(
            TindakanIgdId: model.TindakanIgdId,
            IgdVisitId: model.IgdVisitId,
            RegId: model.RegId,
            TarifId: model.TarifId,
            TarifName: model.TarifName,
            Qty: model.Qty,
            Price: model.Price,
            CrtUser: model.Audit.UserId,
            CrtDate: model.Audit.Timestamp);

    public TindakanIgdModel ToModel()
        => new(
            tindakanIgdId: TindakanIgdId,
            igdVisitId: IgdVisitId,
            regId: RegId,
            tarifId: TarifId,
            tarifName: TarifName,
            qty: Qty,
            price: Price,
            audit: new AuditInfoType(CrtUser, CrtDate));

    public TindakanIgdView ToView()
        => new(
            TindakanIgdId: TindakanIgdId,
            IgdVisitId: IgdVisitId,
            RegId: RegId,
            TarifId: TarifId,
            TarifName: TarifName,
            Qty: Qty,
            Price: Price,
            Subtotal: Qty * Price,
            CreatedDateTime: CrtDate);
}
