using Ardalis.GuardClauses;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteFromHidokCmd(string BookingHidokId) : IRequest;

public class BookingDeleteFromHidokHandler : IRequestHandler<BookingDeleteFromHidokCmd>
{
    private readonly IBookingRepo _bookingRepo;

    public BookingDeleteFromHidokHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task Handle(BookingDeleteFromHidokCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BookingHidokId);
        var booking = _bookingRepo.LoadEntity(request.BookingHidokId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingHidokId} not found")
            );

        _bookingRepo.DeleteEntity(booking);
        return Task.CompletedTask;
    }
}
