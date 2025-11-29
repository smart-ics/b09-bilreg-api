namespace Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

public record OpCaseStateHistType(
    int NoUrut, 
    OpCaseStateEnum OpCaseState, 
    DateTime StateTimestamp)
{
    public static OpCaseStateHistType Default 
        => new OpCaseStateHistType(0, OpCaseStateEnum.Requested, new DateTime(3000,1,1));
};