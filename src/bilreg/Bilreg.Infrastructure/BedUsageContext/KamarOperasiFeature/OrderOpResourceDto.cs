using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OrderOpResourceDto(
    string OrderOpResourceId,
    string OrderOpId,
    int ResourceType,
    string ItemName,
    int Jumlah,
    string Satuan,
    string Keterangan)
{
    public static OrderOpResourceDto FromModel(OrderOpResourceModel model)
        => new(
            model.OrderOpResourceId,
            model.OrderOpId,
            (int)model.ResourceType,
            model.ItemName,
            model.Jumlah,
            model.Satuan,
            model.Keterangan);

    public OrderOpResourceModel ToModel()
        => new(
            OrderOpResourceId,
            OrderOpId,
            (OrderOpResourceTypeEnum)ResourceType,
            ItemName,
            Jumlah,
            Satuan,
            Keterangan);
}
