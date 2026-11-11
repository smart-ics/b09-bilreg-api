using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public class OrderOpResourceRepo : IOrderOpResourceRepo, IScheduleOpResourceRepo
{
    private readonly IOrderOpResourceDal _dal;

    public OrderOpResourceRepo(IOrderOpResourceDal dal)
    {
        _dal = dal;
    }

    public IEnumerable<OrderOpResourceDto> ListData(string orderOpId)
    {
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        return _dal.ListData(orderOpId);
    }

    public OrderOpResourceDto? GetData(string resourceId, string orderOpId)
    {
        Guard.Against.NullOrWhiteSpace(resourceId, nameof(resourceId));
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        return _dal.GetData(resourceId, orderOpId);
    }

    public void Insert(OrderOpResourceDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(dto.OrderOpId, nameof(dto.OrderOpId));
        ValidateResourceAttributes(dto);

        if (string.IsNullOrWhiteSpace(dto.OrderOpResourceId) || dto.OrderOpResourceId.StartsWith("res-"))
        {
            dto.OrderOpResourceId = Ulid.NewUlid().ToString();
        }

        _dal.Insert(dto);
    }

    public void Update(OrderOpResourceDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(dto.OrderOpResourceId, nameof(dto.OrderOpResourceId));
        Guard.Against.NullOrWhiteSpace(dto.OrderOpId, nameof(dto.OrderOpId));
        ValidateResourceAttributes(dto);

        _dal.Update(dto);
    }

    public void Delete(string resourceId, string orderOpId)
    {
        Guard.Against.NullOrWhiteSpace(resourceId, nameof(resourceId));
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        _dal.Delete(resourceId, orderOpId);
    }

    public void DeleteAll(string orderOpId)
    {
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        _dal.DeleteAll(orderOpId);
    }

    public List<OrderOpResourceDto> Reconcile(
        string orderOpId, 
        string scheduleOpId, 
        IEnumerable<OrderOpResourceDto> targetCollection, 
        string userId)
    {
        Guard.Against.NullOrWhiteSpace(orderOpId, nameof(orderOpId));
        var targetList = targetCollection?.ToList() ?? new List<OrderOpResourceDto>();

        // 1. Validate each target item
        foreach (var item in targetList)
        {
            ValidateResourceAttributes(item);

            // Ownership check: if item specifies an OrderOpId, it MUST match the target orderOpId
            if (!string.IsNullOrWhiteSpace(item.OrderOpId) && item.OrderOpId != orderOpId)
            {
                throw new InvalidOperationException(
                    $"Resource '{item.Id}' owner mismatch: specifies OrderOpId '{item.OrderOpId}' but target is '{orderOpId}'.");
            }
        }

        // 2. Fetch existing persisted items for this OrderOpId
        var existingList = _dal.ListData(orderOpId).ToList();
        var existingMap = existingList.ToDictionary(x => x.OrderOpResourceId, x => x);

        // 3. Check for cross-order ID conflicts for items with established IDs
        foreach (var item in targetList)
        {
            var rawId = item.OrderOpResourceId;
            if (!string.IsNullOrWhiteSpace(rawId) && !rawId.StartsWith("res-") && !existingMap.ContainsKey(rawId))
            {
                var other = _dal.GetByIdAnyOwner(rawId);
                if (other != null && other.OrderOpId != orderOpId)
                {
                    throw new InvalidOperationException(
                        $"Resource '{rawId}' belongs to another OrderOpId ('{other.OrderOpId}'). Cross-order mutation is prohibited.");
                }
            }
        }

        // 4. Determine items to keep and items to add/update
        var keptIds = new HashSet<string>();
        var now = DateTime.Now;

        foreach (var item in targetList)
        {
            var rawId = item.OrderOpResourceId;
            bool isExisting = !string.IsNullOrWhiteSpace(rawId) && existingMap.ContainsKey(rawId);

            if (isExisting)
            {
                // Update existing item with stable ID
                var dto = new OrderOpResourceDto(
                    orderOpResourceId: rawId,
                    orderOpId: orderOpId,
                    scheduleOpId: !string.IsNullOrWhiteSpace(scheduleOpId) ? scheduleOpId : item.ScheduleOpId,
                    resourceType: OrderOpResourceDto.MapCategoryToResourceType(item.Category),
                    category: OrderOpResourceDto.MapResourceTypeToCategory(OrderOpResourceDto.MapCategoryToResourceType(item.Category), item.Category),
                    itemName: item.Name,
                    jumlah: item.Qty,
                    satuan: item.Unit,
                    keterangan: item.Notes,
                    updUser: userId,
                    updDate: now
                );
                _dal.Update(dto);
                keptIds.Add(rawId);
            }
            else
            {
                // New item: generate server-generated stable ID if needed
                var newId = (!string.IsNullOrWhiteSpace(rawId) && !rawId.StartsWith("res-")) 
                    ? rawId 
                    : Ulid.NewUlid().ToString();

                var dto = OrderOpResourceDto.Create(
                    id: newId,
                    orderOpId: orderOpId,
                    scheduleOpId: !string.IsNullOrWhiteSpace(scheduleOpId) ? scheduleOpId : item.ScheduleOpId,
                    category: item.Category,
                    name: item.Name,
                    qty: item.Qty,
                    unit: item.Unit,
                    notes: item.Notes,
                    userId: userId,
                    occurredAt: now
                );
                _dal.Insert(dto);
                keptIds.Add(newId);
            }
        }

        // 5. Delete items missing from target collection
        _dal.DeleteMissingFromTarget(orderOpId, keptIds);

        // 6. Return refreshed persisted collection
        return _dal.ListData(orderOpId).ToList();
    }

    private static void ValidateResourceAttributes(OrderOpResourceDto dto)
    {
        Guard.Against.Null(dto, nameof(dto));
        Guard.Against.NullOrWhiteSpace(dto.Name, nameof(dto.Name), "Nama resource tidak boleh kosong.");
        if (dto.Name.Length > 100)
            throw new ArgumentException("Nama resource tidak boleh melebihi 100 karakter.", nameof(dto.Name));

        if (dto.Qty <= 0)
            throw new ArgumentException("Jumlah resource harus lebih besar dari 0.", nameof(dto.Qty));

        if (!string.IsNullOrWhiteSpace(dto.Unit) && dto.Unit.Length > 20)
            throw new ArgumentException("Satuan resource tidak boleh melebihi 20 karakter.", nameof(dto.Unit));

        if (!string.IsNullOrWhiteSpace(dto.Notes) && dto.Notes.Length > 255)
            throw new ArgumentException("Catatan resource tidak boleh melebihi 255 karakter.", nameof(dto.Notes));

        var category = dto.Category?.Trim().ToUpperInvariant() ?? string.Empty;
        if (category is not ("BLOOD" or "DARAH" or "IMPLANT" or "IMPLAN" or "SPECIAL_EQUIPMENT" or "ALATKHUSUS" or "ALAT_KHUSUS" or "SPECIAL EQUIPMENT"))
        {
            throw new ArgumentException($"Kategori resource '{dto.Category}' tidak valid. Kategori yang diizinkan: BLOOD, IMPLANT, SPECIAL_EQUIPMENT.", nameof(dto.Category));
        }
    }
}
