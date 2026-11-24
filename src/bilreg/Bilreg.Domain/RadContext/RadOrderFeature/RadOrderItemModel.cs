namespace Bilreg.Domain.RadContext.RadOrderFeature;

public record RadOrderItemModel(
    string OrderId,
    int ItemNo,
    string TarifId,
    string TarifName,
    RadModalityEnum Modality,
    RadUrgencyEnum Urgency = RadUrgencyEnum.Rutin)
{
    public bool IsCito => Urgency == RadUrgencyEnum.Cito;

    public static RadOrderItemModel Create(
        string orderId,
        int itemNo,
        string tarifId,
        string tarifName,
        RadModalityEnum modality,
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => new(
            orderId ?? string.Empty,
            itemNo,
            tarifId ?? string.Empty,
            tarifName ?? string.Empty,
            modality,
            urgency);

    public static RadOrderItemModel Create(
        string tarifId,
        string tarifName,
        RadModalityEnum modality,
        RadUrgencyEnum urgency = RadUrgencyEnum.Rutin)
        => new(
            string.Empty,
            0,
            tarifId ?? string.Empty,
            tarifName ?? string.Empty,
            modality,
            urgency);
}
