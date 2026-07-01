using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public record OpCaseStateHistDto(string OrderOpId, int NoUrut, int OpCaseState, DateTime StateTimestamp)
{
    public static OpCaseStateHistDto FromModel(string orderOpId, OpCaseStateHistType model)
    {
        var result = new OpCaseStateHistDto(orderOpId, model.NoUrut, (int)model.OpCaseState, model.StateTimestamp);
        return result;
    }

    public OpCaseStateHistType ToModel()
    {
        var result = new OpCaseStateHistType(NoUrut, (OpCaseStateEnum)OpCaseState, StateTimestamp);
        return result;
    }
}