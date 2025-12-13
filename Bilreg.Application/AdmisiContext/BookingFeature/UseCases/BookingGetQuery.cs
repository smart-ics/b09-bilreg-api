using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingGetQuery(string BookingId) : IRequest<BookingGetResponse>, IBookingKey;

public record BookingGetResponse(string BookingId, string BookingDate,
    PersonInfoType Person, string PasienId, RegReff Reg,LayananReff Layanan,
    PpaReff Dokter, string TglBerobat, string JamPraktek, int NoAntrian, 
    ExtAppReffType ExtAppReff, CoverageInfoType CoverageInfo);

public class BookingGetHanlder : IRequestHandler<BookingGetQuery, BookingGetResponse>
{
    private readonly IBookingRepo _bookingRepo;

    public BookingGetHanlder(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task<BookingGetResponse> Handle(BookingGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrEmpty(request.BookingId);
        
        var booking = _bookingRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingId} not found")
            );
        var result = new BookingGetResponse(booking.BookingId, booking.BookingDate.ToString("yyyy-MM-dd"),
            booking.Person, booking.PasienId, booking.Reg, booking.Layanan, booking.Dokter, 
            booking.TglBerobat.ToString("yyyy-MM-dd"), booking.JamPraktek.ToString("HH:mm"), booking.NoAntrian,
            booking.ExtAppReff, booking.CoverageInfo);
        
        return Task.FromResult(result);
    }
}
