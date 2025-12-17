using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public interface IDeleteBookingWorkflow
{
    Task Execute(IBookingKey bookKey);
}
public sealed class DeleteBookingWorkflow : IDeleteBookingWorkflow
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPasienTrackerRepo _pasienTrackerRepo;

    public DeleteBookingWorkflow(
        IBookingRepo bookingRepo,
        IPpaRepo ppaRepo,
        IAntrianRepo antrianRepo,
        IPasienTrackerRepo pasienTrackerRepo)
    {
        _bookingRepo = bookingRepo;
        _ppaRepo = ppaRepo;
        _antrianRepo = antrianRepo;
        _pasienTrackerRepo = pasienTrackerRepo;
    }

    public Task Execute(IBookingKey key)
    {
        var booking = LoadBookingOrExit(key);
        if (booking is null)
            return Task.CompletedTask;

        EnsureBookingNotRegistered(booking);

        var dokter = LoadDokterOrThrow(booking);
        var antrianView = LoadAntrianHeaderOrExit(booking, dokter);
        if (antrianView is null)
            antrianView = new AntrianHeaderView("-", "", new DateOnly(3000, 1, 1), new TimeOnly(0, 0), "");

        var antrian = _antrianRepo.LoadEntity(antrianView).Value;
        var entry = antrian.ListEntry
            .FirstOrDefault(x => x.NoUrut == booking.NoAntrian);

        using var trans = TransHelper.NewScope();

        _bookingRepo.DeleteEntity(booking);

        if (entry is not null)
        {
            antrian.RemoveEntry(entry.NoUrut);
            _antrianRepo.SaveChanges(antrian);

            _pasienTrackerRepo.DeleteEntity(
                PasienTrackerModel.Key(entry.Tracker.PasienTrackerId));
        }

        trans.Complete();
        return Task.CompletedTask;
    }

    #region Helper
    private BookingModel? LoadBookingOrExit(IBookingKey key)
    {
        var opt = _bookingRepo.LoadEntity(key);
        return opt.HasValue ? opt.Value : null;
    }

    private static void EnsureBookingNotRegistered(BookingModel booking)
    {
        if (booking.Reg.RegId.Trim() != "-")
            throw new KeyNotFoundException(
                $"Booking sudah registrasi {booking.Reg.RegId}, tidak boleh delete");
    }

    private PpaType LoadDokterOrThrow(BookingModel booking)
    {
        var key = PpaType.Key(booking.Dokter.PpaId);
        return _ppaRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Dokter {key.PpaId} not found")
            );
    }

    private AntrianHeaderView? LoadAntrianHeaderOrExit(
        BookingModel booking,
        PpaType dokter)
    {
        var tag = AntrianModel.GenSequenceTag(
            booking.TglBerobat, dokter);

        return _antrianRepo
            .ListData(booking.TglBerobat)
            .FirstOrDefault(x => x.SequenceTag == tag);
    }
    #endregion

}
