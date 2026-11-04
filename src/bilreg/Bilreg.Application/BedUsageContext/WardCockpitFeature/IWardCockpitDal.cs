namespace Bilreg.Application.BedUsageContext.WardCockpitFeature;

public interface IWardCockpitDal
{
    Task<WardCockpitOverviewDto?> GetOverviewAsync(string bangsalId, DateTime? currentTime = null, CancellationToken cancellationToken = default);
    WardCockpitOverviewDto? GetOverview(string bangsalId, DateTime? currentTime = null);
    Task<WardCockpitBedDetailDto?> GetBedDetailAsync(string bedId, DateTime? currentTime = null, CancellationToken cancellationToken = default);
    WardCockpitBedDetailDto? GetBedDetail(string bedId, DateTime? currentTime = null);
    Task<IEnumerable<WardCockpitBangsalListItemDto>> ListActiveBangsalAsync(CancellationToken cancellationToken = default);
    Task<string?> SaveRoomingInAsync(
        string motherPakaiBedId,
        string babyRegId,
        string bedId,
        string bangsalId,
        string kamarId,
        string regId,
        string userId,
        DateTime occurredAt,
        string? catatan = null,
        CancellationToken cancellationToken = default);
    Task<IEnumerable<WardCockpitBabyLookupDto>> LookupBabyAsync(string keyword, CancellationToken cancellationToken = default);
}

public record WardCockpitBangsalListItemDto(string BangsalId, string BangsalName, string KelasName);
public record WardCockpitBabyLookupDto(string RegId, string MrNo, string NamaPasien, string Gender, string? TglLahir);
