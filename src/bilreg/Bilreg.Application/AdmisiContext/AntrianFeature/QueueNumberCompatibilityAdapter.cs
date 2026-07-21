using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

/// <summary>
/// Anti-corruption adapter between legacy AntrianMap allocation and canonical Queue Session entries (F-13).
/// </summary>
public sealed class QueueNumberCompatibilityAdapter : IQueueNumberCompatibilityAdapter
{
    private readonly IAntrianMapWithBookingResolver _bookingResolver;
    private readonly IAntrianMapWithRegResolver _regResolver;
    private readonly QueueNumberCompatibilityOptions _options;

    public QueueNumberCompatibilityAdapter(
        IAntrianMapWithBookingResolver bookingResolver,
        IAntrianMapWithRegResolver regResolver,
        IOptions<QueueNumberCompatibilityOptions> options)
    {
        _bookingResolver = bookingResolver;
        _regResolver = regResolver;
        _options = options.Value;
    }

    public Result<ReservedQueueNumber> ReserveForBooking(
        JadwalPraktekType jadwal,
        DateOnly tgl,
        BookingModel booking,
        PasienModel pasien)
    {
        EnsureLegacyMapAuthority();
        var result = _bookingResolver.Resolve(jadwal, tgl, booking, pasien);
        return ToReserved(result);
    }

    public Result<ReservedQueueNumber> ReserveForRegistration(
        JadwalPraktekType jadwal,
        DateOnly tgl,
        RegModel reg)
    {
        EnsureLegacyMapAuthority();
        var result = _regResolver.Resolve(jadwal, tgl, reg);
        return ToReserved(result);
    }

    public AntrianEntryModel ProjectIntoQueueSession(
        AntrianModel antrian,
        ReservedQueueNumber reserved,
        PasienTrackerModel tracker,
        string reffId,
        string reffDesc,
        DateTime createdAt) =>
        antrian.AddEntry(reserved.NoUrut, tracker, reffId, reffDesc, createdAt);

    public AntrianEntryModel AcceptExternalNumber(
        AntrianModel antrian,
        int noUrut,
        PasienTrackerModel tracker,
        string reffId,
        string reffDesc,
        DateTime createdAt) =>
        // Hidok exception: queue session only; legacy map is not updated.
        antrian.AddEntry(noUrut, tracker, reffId, reffDesc, createdAt);

    public void Release(AntrianMapModel map, int noUrut)
    {
        if (map.AntrianMapId == "-" || map.JadwalId == "-")
            return;

        map.VoidSlot(noUrut);
    }

    public bool ProjectSourceReffForRegistration(AntrianMapModel map, int noUrut, RegModel reg)
    {
        if (map.AntrianMapId == "-" || map.JadwalId == "-")
            return false;

        var detil = map.ListMap.FirstOrDefault(x => x.NoUrut == noUrut);
        if (detil is null || detil.IsFreeSlot())
            return false;

        map.SetDataPasien(noUrut, reg);
        return true;
    }

    private void EnsureLegacyMapAuthority()
    {
        if (_options.Authority == QueueNumberAuthority.QueueSession)
            throw new NotSupportedException(
                "QueueSession number authority cutover is not yet enabled. " +
                "Set QueueNumber:Authority to LegacyMap until parity is proven.");
    }

    private static Result<ReservedQueueNumber> ToReserved(
        Result<(AntrianMapModel, AntrianMapDetilModel)> result)
    {
        var (map, detil) = result.Value;
        return Result<ReservedQueueNumber>.Success(ReservedQueueNumber.FromMap(map, detil));
    }
}
