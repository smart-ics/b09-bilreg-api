using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.Integration;

public class WardAccommodationGateway : IWardAccommodationGateway
{
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public WardAccommodationGateway(IKelasRepo kelasRepo, IBangsalRepo bangsalRepo)
    {
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public KelasReff ResolveKelas(string kelasId)
    {
        Guard.Against.NullOrWhiteSpace(kelasId);
        return _kelasRepo.LoadEntity(KelasType.Key(kelasId))
            .GetValueOrThrow($"Kelas '{kelasId}' tidak ditemukan.")
            .ToReff();
    }

    public BangsalReff ResolveBangsal(string bangsalId)
    {
        Guard.Against.NullOrWhiteSpace(bangsalId);
        return _bangsalRepo.LoadEntity(BangsalType.Key(bangsalId))
            .GetValueOrThrow($"Bangsal '{bangsalId}' tidak ditemukan.")
            .ToReff();
    }

    /// <summary>
    /// V1: Ward consumes Waiting List via persistence and GET api/admisi-ranap/waiting-list (ADR-004).
    /// Future HTTP/push notification can be wired here without changing application handlers.
    /// </summary>
    public void NotifyHandOver(WardAccommodationHandOver handOver)
    {
        Guard.Against.Null(handOver);
    }
}
