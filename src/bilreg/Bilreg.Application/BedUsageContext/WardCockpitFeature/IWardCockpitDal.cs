namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public interface IWardCockpitDal
{
    Task<WardCockpitOverviewDto?> GetOverviewAsync(string bangsalId, CancellationToken cancellationToken = default);
    WardCockpitOverviewDto? GetOverview(string bangsalId);
    Task<WardCockpitBedDetailDto?> GetBedDetailAsync(string bedId, CancellationToken cancellationToken = default);
    WardCockpitBedDetailDto? GetBedDetail(string bedId);
}
