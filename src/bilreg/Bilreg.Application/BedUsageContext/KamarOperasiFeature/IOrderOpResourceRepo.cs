namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IOrderOpResourceRepo
{
    IEnumerable<OrderOpResourceDto> ListData(string orderOpId);
    OrderOpResourceDto? GetData(string resourceId, string orderOpId);
    void Insert(OrderOpResourceDto dto);
    void Update(OrderOpResourceDto dto);
    void Delete(string resourceId, string orderOpId);
    void DeleteAll(string orderOpId);
    List<OrderOpResourceDto> Reconcile(string orderOpId, string scheduleOpId, IEnumerable<OrderOpResourceDto> targetCollection, string userId);
}

// Alias interface for ScheduleOpResourceRepo
public interface IScheduleOpResourceRepo : IOrderOpResourceRepo
{
}
