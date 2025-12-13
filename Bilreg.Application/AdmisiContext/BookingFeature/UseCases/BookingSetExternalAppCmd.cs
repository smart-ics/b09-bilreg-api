using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingSetExternalAppCmd(string BookingExtId, string CheckinQr) : IRequest;

public class BookingSetExternalAppHandler : IRequestHandler<BookingSetExternalAppCmd>
{
    private readonly IBookingRepo _bookingRepo;

    public BookingSetExternalAppHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task Handle(BookingSetExternalAppCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BookingExtId);
        Guard.Against.NullOrWhiteSpace(request.CheckinQr);

        var booking = _bookingRepo.LoadEntity(request.BookingExtId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingExtId} not found")
            );
        var extApp = booking.ExtAppReff with { CheckInQr = request.CheckinQr };
        booking.SetExtApp(extApp);

        _bookingRepo.SaveChanges(booking);
        return Task.CompletedTask;
    }
}
