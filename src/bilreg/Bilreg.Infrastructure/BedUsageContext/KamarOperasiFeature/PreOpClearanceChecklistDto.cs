using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record PreOpClearanceChecklistDto(
    string ClearanceId,
    int NoUrut,
    string ChecklistKey,
    int Status,
    string Notes)
{
    public static PreOpClearanceChecklistDto FromModel(
        string clearanceId,
        int noUrut,
        PreOpChecklistItemType item)
    {
        return new PreOpClearanceChecklistDto(
            clearanceId,
            noUrut,
            item.ChecklistKey,
            (int)item.Status,
            item.Notes
        );
    }

    public PreOpChecklistItemType ToModel()
    {
        return new PreOpChecklistItemType(
            ChecklistKey,
            (ChecklistItemStatusEnum)Status,
            Notes
        );
    }
}
