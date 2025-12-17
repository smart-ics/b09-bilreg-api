using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteFromHidokCmd(string BookingHidokId) : IRequest;

public class BookingDeleteFromHidokHandler : IRequestHandler<BookingDeleteFromHidokCmd>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IPasienTrackerRepo _paasienTrackerRepo;

    public BookingDeleteFromHidokHandler(IBookingRepo bookingRepo,
        IAntrianRepo antrianRepo,
        IPpaRepo ppaRepo,
        IPasienTrackerRepo paasienTrackerRepo)
    {
        _bookingRepo = bookingRepo;
        _antrianRepo = antrianRepo;
        _ppaRepo = ppaRepo;
        _paasienTrackerRepo = paasienTrackerRepo;
    }

    public Task Handle(BookingDeleteFromHidokCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BookingHidokId);
        var booking = _bookingRepo.LoadEntity(request.BookingHidokId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingHidokId} not found")
            );

        // cek booking sudah reg
        if (booking.Reg.RegId.Trim() != "-")
            throw new KeyNotFoundException($"Booking sudah registrasi {booking.Reg.RegId}, tidak boleh delete");


        // cek dokter
        var dokterKey = PpaType.Key(booking.Dokter.PpaId);
        var dokter = _ppaRepo.LoadEntity(dokterKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {dokterKey.PpaId} not found")
            );
       
        //  ambil data antrian
        var listAntrian = _antrianRepo.ListData(booking.TglBerobat);
        var sequenceTag = AntrianModel.GenSequenceTag(booking.TglBerobat, dokter);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag) ??
            new AntrianHeaderView("-", "", new DateOnly(3000, 1, 1), new TimeOnly(0, 0), "");
        var antrian = _antrianRepo.LoadEntity(antrianView).Value;
        
        var antrianPasien = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == booking.NoAntrian) ?? AntrianEntryModel.Default;
        var pasienTracker = PasienTrackerModel.Key(antrianPasien.Tracker.PasienTrackerId);

        antrian.RemoveEntry(booking.NoAntrian);
        
        using var trans = TransHelper.NewScope();
        _bookingRepo.DeleteEntity(booking);
        _antrianRepo.SaveChanges(antrian);
        _paasienTrackerRepo.DeleteEntity(pasienTracker);
        trans.Complete();   
        return Task.CompletedTask;
    }
}
