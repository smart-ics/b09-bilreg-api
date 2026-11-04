namespace Bilreg.Application.BedUsageContext.PakaiBedFeature;

public interface IPakaiBedDal
{
    Task<List<string>> VoidBedAndRoomChargeAsync(string pakaiBedId, string userId, string alasanBatal, DateTime? voidTime = null);
}
