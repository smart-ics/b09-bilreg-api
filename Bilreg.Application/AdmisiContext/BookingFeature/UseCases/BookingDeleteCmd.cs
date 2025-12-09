using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IBookingRepo _bookingRepo;

    public BookingDeleteHandler(IBookingRepo bookingRepo)
    {
        _bookingRepo = bookingRepo;
    }

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {
        
        Guard.Against.NullOrWhiteSpace(request.BookingId);
        var booking = _bookingRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingId} not found")
            );

        _bookingRepo.DeleteEntity(booking);
        return Task.CompletedTask;
    }
}
