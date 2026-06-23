using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingResolvePasienIdCmd(string BookingId, string PasienId): IRequest;

public class BookingResolvePasienIdHandler : IRequestHandler<BookingResolvePasienIdCmd>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienRepo _pasienRepo;

    public BookingResolvePasienIdHandler(IBookingRepo bookingRepo, IPasienRepo pasienRepo)
    {
        _bookingRepo = bookingRepo;
        _pasienRepo = pasienRepo;
    }

    public Task Handle(BookingResolvePasienIdCmd request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
        var booking = _bookingRepo.LoadEntity(BookingModel.Key(request.BookingId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking id {request.BookingId} not found")
            );
        booking.ResolvePasienId(pasien);
        _bookingRepo.SaveChanges(booking);
        return Task.CompletedTask;
    }
}