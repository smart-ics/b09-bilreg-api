//TODO: Refactor AntrianMap Model

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
    private readonly IDeleteBookingWorkflow _deleteBookingWorkflow;

    public BookingDeleteFromHidokHandler(IBookingRepo bookingRepo,
        IDeleteBookingWorkflow deleteBookingWorkflow)
    {
        _bookingRepo = bookingRepo;
        _deleteBookingWorkflow = deleteBookingWorkflow;
    }

    public Task Handle(BookingDeleteFromHidokCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.BookingHidokId);
        var booking = _bookingRepo.LoadEntity(request.BookingHidokId)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {request.BookingHidokId} not found")
            );
        _deleteBookingWorkflow.Execute(booking);


        return Task.CompletedTask;
    }
}
