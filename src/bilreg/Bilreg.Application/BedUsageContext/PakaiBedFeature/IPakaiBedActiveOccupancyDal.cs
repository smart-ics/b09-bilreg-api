namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedActiveOccupancyDal
{
    PakaiBedActiveOccupancyResponse? GetActiveByBed(string bedId);
    IEnumerable<PakaiBedActiveOccupancyResponse> ListActiveByWard(string bangsalId);
}
