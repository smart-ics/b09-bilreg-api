using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.WaitingListFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

public sealed record ActiveLocationResult(
    KelasDkType KelasDk,
    BangsalReff Bangsal);

public interface IAdmissionCurrentLocationResolver
{
    ActiveLocationResult Resolve(string regId, KelasDkType fallbackKelasDk, BangsalReff fallbackBangsal);
}

public sealed class AdmissionCurrentLocationResolver : IAdmissionCurrentLocationResolver
{
    private static readonly DateTime ActiveKeluarTimestamp = new(3000, 1, 1);

    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IKelasRepo _kelasRepo;

    public AdmissionCurrentLocationResolver(
        IPakaiBedRepo pakaiBedRepo,
        IWaitingListRepo waitingListRepo,
        IBedRepo bedRepo,
        IKelasRepo kelasRepo)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _waitingListRepo = waitingListRepo;
        _bedRepo = bedRepo;
        _kelasRepo = kelasRepo;
    }

    public ActiveLocationResult Resolve(
        string regId,
        KelasDkType fallbackKelasDk,
        BangsalReff fallbackBangsal)
    {
        var fromPakaiBed = TryResolveFromPakaiBed(regId);
        if (fromPakaiBed is not null)
            return fromPakaiBed;

        var fromWaitingList = TryResolveFromWaitingList(regId);
        if (fromWaitingList is not null)
            return fromWaitingList;

        return new ActiveLocationResult(fallbackKelasDk, fallbackBangsal);
    }

    private ActiveLocationResult? TryResolveFromPakaiBed(string regId)
    {
        var activePakaiBed = (_pakaiBedRepo.ListData(RegModel.Key(regId)) ?? [])
            .Where(pb => !pb.AuditTrail.IsVoided
                && pb.Periode.Keluar.Timestamp == ActiveKeluarTimestamp)
            .OrderByDescending(pb => pb.Periode.Masuk.Timestamp)
            .FirstOrDefault();

        if (activePakaiBed is null)
            return null;

        var bangsal = _bedRepo.LoadEntity(BedType.Key(activePakaiBed.Bed.BedId))
            .Match<BangsalReff?>(bed => bed.Bangsal, () => null);
        var kelasDk = _kelasRepo.LoadEntity(KelasType.Key(activePakaiBed.Kelas.KelasId))
            .Match<KelasDkType?>(kelas => kelas.KelasDk, () => null);

        return bangsal is not null && kelasDk is not null
            ? new ActiveLocationResult(kelasDk, bangsal)
            : null;
    }

    private ActiveLocationResult? TryResolveFromWaitingList(string regId)
    {
        return _waitingListRepo.LoadActiveByRegId(regId)
            .Match<ActiveLocationResult?>(
                wl => _kelasRepo.LoadEntity(KelasType.Key(wl.KelasRawat.KelasId))
                    .Match<ActiveLocationResult?>(
                        kelas => new ActiveLocationResult(kelas.KelasDk, wl.Bangsal),
                        () => null),
                () => null);
    }
}
