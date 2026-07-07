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

    BangsalReff ResolveBangsal(string bangsalId);

    void NotifyHandOver(WardAccommodationHandOver handOver);
}
