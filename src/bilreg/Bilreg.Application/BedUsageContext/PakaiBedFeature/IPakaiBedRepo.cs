using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedRepo :
    ISaveChange<PakaiBedModel>,
    ILoadEntity<PakaiBedModel, IPakaiBed>,
    IDeleteEntity<IPakaiBed>,
    IListData<PakaiBedModel, IRegKey>
{
    MayBe<PakaiBedModel> GetActiveOccupant(IBedKey bedKey);
    Task<List<string>> VoidBedAndRoomChargeAsync(string pakaiBedId, string userId, string alasanBatal, DateTime? voidTime = null);
    Task<List<string>> GetRoomingInBabyRegIdsAsync(string pakaiBedId) => Task.FromResult(new List<string>());
    Task<List<string>> GetRoomingInBabyTrsIdsAsync(string bedId, string? babyRegId = null) => Task.FromResult(new List<string>());
    Task<List<string>> ReleaseRoomingInBabyAsync(string bedId, string? babyRegId, string userId, DateTime dischargeTime) => Task.FromResult(new List<string>());
}
