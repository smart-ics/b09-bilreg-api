namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedDal
{
    Task<List<string>> VoidBedAndRoomChargeAsync(string pakaiBedId, string userId, string alasanBatal, DateTime? voidTime = null);
    Task<List<string>> GetRoomingInBabyRegIdsAsync(string pakaiBedId) => Task.FromResult(new List<string>());
    Task<List<string>> VoidRoomingInBabyAsync(string motherPakaiBedId, string userId, string alasanBatal, DateTime? voidTime = null) => Task.FromResult(new List<string>());
    Task<List<string>> GetRoomingInBabyTrsIdsAsync(string bedId, string? babyRegId = null) => Task.FromResult(new List<string>());
    Task<List<string>> ReleaseRoomingInBabyAsync(string bedId, string? babyRegId, string userId, DateTime dischargeTime) => Task.FromResult(new List<string>());
}
