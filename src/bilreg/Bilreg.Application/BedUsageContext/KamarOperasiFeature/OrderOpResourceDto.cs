using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public class OrderOpResourceDto
{
    public string OrderOpResourceId { get; set; } = string.Empty;
    public string Id { get => OrderOpResourceId; set => OrderOpResourceId = value; }

    public string OrderOpId { get; set; } = string.Empty;
    public string ScheduleOpId { get; set; } = string.Empty;
    public int ResourceType { get; set; } = 1;
    public string Category { get; set; } = string.Empty;

    public string ItemName { get; set; } = string.Empty;
    public string Name { get => ItemName; set => ItemName = value; }

    public int Jumlah { get; set; } = 1;
    public int Qty { get => Jumlah; set => Jumlah = value; }

    public string Satuan { get; set; } = string.Empty;
    public string Unit { get => Satuan; set => Satuan = value; }

    public string Keterangan { get; set; } = string.Empty;
    public string Notes { get => Keterangan; set => Keterangan = value; }

    public string CrtUser { get; set; } = string.Empty;
    public DateTime CrtDate { get; set; } = new(3000, 1, 1);
    public string UpdUser { get; set; } = string.Empty;
    public DateTime UpdDate { get; set; } = new(3000, 1, 1);
    public string VodUser { get; set; } = string.Empty;
    public DateTime VodDate { get; set; } = new(3000, 1, 1);

    public OrderOpResourceDto() { }

    public OrderOpResourceDto(
        string orderOpResourceId,
        string orderOpId,
        int resourceType,
        string itemName,
        int jumlah,
        string satuan,
        string keterangan)
        : this(orderOpResourceId, orderOpId, string.Empty, resourceType, MapResourceTypeToCategory(resourceType), itemName, jumlah, satuan, keterangan)
    {
    }

    public OrderOpResourceDto(
        string orderOpResourceId,
        string orderOpId,
        string scheduleOpId,
        int resourceType,
        string category,
        string itemName,
        int jumlah,
        string satuan,
        string keterangan,
        string crtUser = "",
        DateTime? crtDate = null,
        string updUser = "",
        DateTime? updDate = null,
        string vodUser = "",
        DateTime? vodDate = null)
    {
        OrderOpResourceId = orderOpResourceId;
        OrderOpId = orderOpId;
        ScheduleOpId = scheduleOpId;
        ResourceType = resourceType;
        Category = string.IsNullOrWhiteSpace(category) ? MapResourceTypeToCategory(resourceType) : category;
        ItemName = itemName;
        Jumlah = jumlah;
        Satuan = satuan;
        Keterangan = keterangan;
        CrtUser = crtUser;
        CrtDate = crtDate ?? new DateTime(3000, 1, 1);
        UpdUser = updUser;
        UpdDate = updDate ?? new DateTime(3000, 1, 1);
        VodUser = vodUser;
        VodDate = vodDate ?? new DateTime(3000, 1, 1);
    }

    public static int MapCategoryToResourceType(string category) =>
        category?.Trim().ToUpperInvariant() switch
        {
            "BLOOD" or "DARAH" => 1,
            "IMPLANT" or "IMPLAN" => 2,
            "SPECIAL_EQUIPMENT" or "ALATKHUSUS" or "ALAT_KHUSUS" or "SPECIAL EQUIPMENT" => 3,
            _ => 1
        };

    public static string MapResourceTypeToCategory(int resourceType, string existingCategory = "")
    {
        if (!string.IsNullOrWhiteSpace(existingCategory))
            return existingCategory.Trim().ToUpperInvariant();
        return resourceType switch
        {
            1 => "BLOOD",
            2 => "IMPLANT",
            3 => "SPECIAL_EQUIPMENT",
            _ => "OTHER"
        };
    }

    public static OrderOpResourceDto Create(
        string id,
        string orderOpId,
        string scheduleOpId,
        string category,
        string name,
        int qty,
        string unit,
        string notes,
        string userId,
        DateTime? occurredAt = null)
    {
        var timestamp = occurredAt ?? DateTime.Now;
        var rType = MapCategoryToResourceType(category);
        var normCategory = MapResourceTypeToCategory(rType, category);

        return new OrderOpResourceDto(
            orderOpResourceId: id,
            orderOpId: orderOpId,
            scheduleOpId: scheduleOpId,
            resourceType: rType,
            category: normCategory,
            itemName: name,
            jumlah: qty,
            satuan: unit,
            keterangan: notes,
            crtUser: userId,
            crtDate: timestamp,
            updUser: userId,
            updDate: timestamp,
            vodUser: string.Empty,
            vodDate: new DateTime(3000, 1, 1)
        );
    }

    public static OrderOpResourceDto FromModel(OrderOpResourceModel model)
        => new(
            orderOpResourceId: model.OrderOpResourceId,
            orderOpId: model.OrderOpId,
            scheduleOpId: string.Empty,
            resourceType: (int)model.ResourceType,
            category: MapResourceTypeToCategory((int)model.ResourceType),
            itemName: model.ItemName,
            jumlah: model.Jumlah,
            satuan: model.Satuan,
            keterangan: model.Keterangan);

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
