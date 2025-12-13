using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPpaRepo _ppaRepo;
    public BookingDeleteHandler(IBookingRepo bookingRepo,
        IAntrianRepo antrianRepo,
        IPpaRepo ppaRepo)
    {
        _bookingRepo = bookingRepo;
        _antrianRepo = antrianRepo;
        _ppaRepo = ppaRepo;
    }

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {
        
        Guard.Against.NullOrWhiteSpace(request.BookingId);

        var booking = _bookingRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingId} not found")
            );

        // cek Dokter
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
            new AntrianHeaderView("-", "", new DateOnly(3000,1,1), new TimeOnly(0,0), "");
        var antrian = _antrianRepo.LoadEntity(antrianView).Value;
        
        antrian.RemoveEntry(booking.NoAntrian);
        
        _bookingRepo.DeleteEntity(booking);
        _antrianRepo.SaveChanges(antrian);

        return Task.CompletedTask;
    }
}
