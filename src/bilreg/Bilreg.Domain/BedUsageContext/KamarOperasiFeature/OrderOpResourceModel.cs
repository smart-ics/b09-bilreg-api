using Ardalis.GuardClauses;

namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public class OrderOpResourceModel
{
    public OrderOpResourceModel(
        string orderOpResourceId,
        string orderOpId,
        OrderOpResourceTypeEnum resourceType,
        string itemName,
        int jumlah,
        string satuan,
        string keterangan)
    {
        OrderOpResourceId = orderOpResourceId;
        OrderOpId = orderOpId;
        ResourceType = resourceType;
        ItemName = itemName;
        Jumlah = jumlah;
        Satuan = satuan;
        Keterangan = keterangan;
    }

    public static OrderOpResourceModel Create(
        string orderOpResourceId,
        string orderOpId,
        OrderOpResourceTypeEnum resourceType,
        string itemName,
        int jumlah,
        string satuan,
        string keterangan)
    {
        Guard.Against.NullOrWhiteSpace(itemName, nameof(itemName));
        Guard.Against.NegativeOrZero(jumlah, nameof(jumlah));
        Guard.Against.NullOrWhiteSpace(satuan, nameof(satuan));

        var id = string.IsNullOrWhiteSpace(orderOpResourceId) ? Ulid.NewUlid().ToString() : orderOpResourceId;
        return new OrderOpResourceModel(
            id,
            orderOpId ?? string.Empty,
            resourceType,
            itemName,
            jumlah,
            satuan,
            keterangan ?? string.Empty);
    }

    public string OrderOpResourceId { get; private set; }
    public string OrderOpId { get; private set; }
    public OrderOpResourceTypeEnum ResourceType { get; private set; }
    public string ItemName { get; private set; }
    public int Jumlah { get; private set; }
    public string Satuan { get; private set; }
    public string Keterangan { get; private set; }
}
