using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BedUsageContext.RoomRateFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BedUsageContext.RoomChargeFeature;
using Bilreg.Domain.BedUsageContext.RoomRateFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.RoomChargeFeature.UseCases;

public record RoomChargeCreateCmd(string PakaiBedId, string UserId)
    : IRequest<RoomChargeCreateResponse>, IPakaiBed;

public record RoomChargeCreateResponse(string RoomChargeId);

public class RoomChargeCreateHandler : IRequestHandler<RoomChargeCreateCmd, RoomChargeCreateResponse>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IBedRepo _bedRepo;
    private readonly IRoomRateRepo _roomRateRepo;
    private readonly IRoomChargeRepo _roomChargeRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public RoomChargeCreateHandler(
        IPakaiBedRepo pakaiBedRepo,
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        IBedRepo bedRepo,
        IRoomRateRepo roomRateRepo,
        IRoomChargeRepo roomChargeRepo,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _bedRepo = bedRepo;
        _roomRateRepo = roomRateRepo;
        _roomChargeRepo = roomChargeRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<RoomChargeCreateResponse> Handle(RoomChargeCreateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var pakaiBed = LoadAndValidatePakaiBed(request);
        var reg = LoadAndValidateRegister(RegModel.Key(pakaiBed.Reg.RegId));
        var layanan = LoadAndValidateLyn(pakaiBed.Layanan);
        var bed = LoadAndValidateBed(BedType.Key(pakaiBed.Bed.BedId));

        var occurredAt = _tglJamProvider.Now;
        var existingCharges = _roomChargeRepo.ListData(request);
        if (existingCharges.Any(x => x.TimeCharge.Date == occurredAt.Date))
            throw new InvalidOperationException(
                $"Tagihan untuk PakaiBed '{request.PakaiBedId}' pada tanggal {occurredAt:yyyy-MM-dd} sudah ada");

        var roomRate = _roomRateRepo.LoadEntity(KamarType.Key(bed.Kamar.KamarId))
            .GetValueOrThrow($"RoomRate kamar '{bed.Kamar.KamarId}' tidak ditemukan");

        var listKomponen = SelectKomponen(roomRate, pakaiBed, occurredAt)
            .Select(x => new RoomChargeKomponenModel(
                x.Komponen.KomponenId,
                x.Komponen.KomponenName,
                x.Nilai,
                0,
                x.Nilai));

        var roomCharge = RoomChargeModel.Create(pakaiBed, reg, layanan, bed,
            occurredAt, request.UserId, listKomponen);
        
        using var trans = TransHelper.NewScope();
        _roomChargeRepo.SaveChanges(roomCharge);
        trans.Complete();

        return Task.FromResult(new RoomChargeCreateResponse(roomCharge.RoomChargeId));
    }

    #region PRIVATE-HELPER

    private PakaiBedModel LoadAndValidatePakaiBed(IPakaiBed key)
    {
        var pakaiBed = _pakaiBedRepo.LoadEntity(key)
            .GetValueOrThrow($"PakaiBed '{key.PakaiBedId}' tidak ditemukan");
        
        if (pakaiBed.Periode.Keluar != AuditInfoType.Default)
            throw new InvalidOperationException($"PakaiBed '{key.PakaiBedId}' sudah selesai, tidak dapat ditagih");
        return pakaiBed;
    }

    private RegModel LoadAndValidateRegister(IRegKey key) =>
        _regRepo.LoadEntity(key)
            .GetValueOrThrow($"Register '{key.RegId}' tidak ditemukan");

    private LayananType LoadAndValidateLyn(ILayananKey key) =>
        _layananRepo.LoadEntity(key)
            .GetValueOrThrow($"Layanan '{key.LayananId}' tidak ditemukan");

    private BedType LoadAndValidateBed(IBedKey key) =>
        _bedRepo.LoadEntity(key)
            .GetValueOrThrow($"Bed '{key.BedId}' tidak ditemukan");
    private static IEnumerable<RoomRateKomponenType> SelectKomponen(
        IRoomRate<IRoomRateDetail> roomRate, PakaiBedModel pakaiBed, DateTime occurredAt)
    {
        var tipeKamarId = pakaiBed.TipeKamar.TipeKamarId;
        var kelasId = pakaiBed.Kelas.KelasId;
        var hariKe = (occurredAt.Date - pakaiBed.Periode.Masuk.Timestamp.Date).Days + 1;

        return roomRate switch
        {
            RoomRateRegulerType reguler => SelectReguler(reguler, tipeKamarId),
            RoomRateFloatingType floating => SelectFloating(floating, kelasId),
            RoomRateDailyType daily => SelectDaily(daily, hariKe),
            _ => throw new InvalidOperationException("Jenis RoomRate tidak dikenal")
        };
    }

    private static IEnumerable<RoomRateKomponenType> SelectReguler(
        RoomRateRegulerType reguler, string tipeKamarId)
    {
        var detail = reguler.ListTipe
            .FirstOrDefault(x => x.TipeKamar.TipeKamarId == tipeKamarId)
            ?? throw new InvalidOperationException($"RoomRate reguler tidak menemukan tipe kamar '{tipeKamarId}'");
        return detail.ListKomponen;
    }

    private static IEnumerable<RoomRateKomponenType> SelectFloating(
        RoomRateFloatingType floating, string kelasId)
    {
        var detail = floating.ListTipe
            .FirstOrDefault(x => x.Kelas.KelasId == kelasId)
            ?? throw new InvalidOperationException($"RoomRate floating tidak menemukan kelas '{kelasId}'");
        return detail.ListKomponen;
    }

    private static IEnumerable<RoomRateKomponenType> SelectDaily(
        RoomRateDailyType daily, int hariKe)
    {
        var detail = daily.ListTipe
            .Where(x => x.HariKe <= hariKe)
            .OrderByDescending(x => x.HariKe)
            .FirstOrDefault()
            ?? throw new InvalidOperationException($"RoomRate daily tidak menemukan hari ke-{hariKe}");
        return detail.ListKomponen;
    }
    #endregion
}