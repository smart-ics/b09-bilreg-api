using Bilreg.Domain.RadContext.RadOrderFeature;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public record RadOrderItemDto(
    string OrderId,
    int ItemNo,
    string TarifId,
    string TarifName,
    string Modality,
    int Urgency) : IRadOrderKey
{
    public RadOrderItemDto() : this(string.Empty, 0, string.Empty, string.Empty, string.Empty, 0)
    {
    }

    public static RadOrderItemDto FromModel(string orderId, RadOrderItemModel model) => new(
        orderId,
        model.ItemNo,
        model.TarifId,
        model.TarifName,
        model.Modality.ToString(),
        (int)model.Urgency);

    public RadOrderItemModel ToModel()
    {
        var modality = Enum.TryParse<RadModalityEnum>(Modality, ignoreCase: true, out var m) ? m : RadModalityEnum.CR;
        var urgency = Enum.IsDefined(typeof(RadUrgencyEnum), Urgency) ? (RadUrgencyEnum)Urgency : RadUrgencyEnum.Rutin;
        return new RadOrderItemModel(OrderId, ItemNo, TarifId, TarifName, modality, urgency);
    }
}
