using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Application.AdmisiRanapContext.Integration;

public record WardAccommodationHandOver(
    string WaitingListId,
    string RegId,
    string BangsalId,
    string KelasId,
    WaitingListStatusEnum Status);

public interface IWardAccommodationGateway
{
    KelasReff ResolveKelas(string kelasId);

    KelasDkType ResolveKelasDk(string kelasDkId);

    BangsalReff ResolveBangsal(string bangsalId);

    BangsalReff ResolveBangsalForCareClass(string bangsalId, string kelasDkId);

    IReadOnlyList<BangsalReff> ListEligibleBangsal(string kelasDkId);

    void NotifyHandOver(WardAccommodationHandOver handOver);
}
