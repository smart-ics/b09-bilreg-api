using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.Integration;

public class WardAccommodationGateway : IWardAccommodationGateway
{
    private readonly IKelasRepo _kelasRepo;
    private readonly IKelasDkRepo _kelasDkRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IBangsalByKelasDkDal _bangsalByKelasDkDal;

    public WardAccommodationGateway(
        IKelasRepo kelasRepo,
        IKelasDkRepo kelasDkRepo,
        IBangsalRepo bangsalRepo,
        IBangsalByKelasDkDal bangsalByKelasDkDal)
    {
        _kelasRepo = kelasRepo;
        _kelasDkRepo = kelasDkRepo;
        _bangsalRepo = bangsalRepo;
        _bangsalByKelasDkDal = bangsalByKelasDkDal;
    }

    public KelasReff ResolveKelas(string kelasId)
    {
        Guard.Against.NullOrWhiteSpace(kelasId);
        return _kelasRepo.LoadEntity(KelasType.Key(kelasId))
            .GetValueOrThrow($"Kelas '{kelasId}' tidak ditemukan.")
            .ToReff();
    }

    public KelasDkType ResolveKelasDk(string kelasDkId)
    {
        Guard.Against.NullOrWhiteSpace(kelasDkId);
        return _kelasDkRepo.LoadEntity(KelasDkType.Key(kelasDkId))
            .GetValueOrThrow($"Care Class '{kelasDkId}' tidak ditemukan.");
    }

    public BangsalReff ResolveBangsal(string bangsalId)
    {
        Guard.Against.NullOrWhiteSpace(bangsalId);
        return _bangsalRepo.LoadEntity(BangsalType.Key(bangsalId))
            .GetValueOrThrow($"Bangsal '{bangsalId}' tidak ditemukan.")
            .ToReff();
    }

    public BangsalReff ResolveBangsalForCareClass(string bangsalId, string kelasDkId)
    {
        Guard.Against.NullOrWhiteSpace(bangsalId);
        Guard.Against.NullOrWhiteSpace(kelasDkId);

        ResolveKelasDk(kelasDkId);

        var eligible = ListEligibleBangsal(kelasDkId);
        var match = eligible.FirstOrDefault(b => b.BangsalId == bangsalId);
        if (match is null)
            throw new InvalidOperationException(
                $"Bangsal '{bangsalId}' tidak memenuhi syarat untuk Care Class '{kelasDkId}'.");

        return match;
    }

    public IReadOnlyList<BangsalReff> ListEligibleBangsal(string kelasDkId)
    {
        Guard.Against.NullOrWhiteSpace(kelasDkId);
        return _bangsalByKelasDkDal.ListByKelasDkId(kelasDkId).ToList();
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
