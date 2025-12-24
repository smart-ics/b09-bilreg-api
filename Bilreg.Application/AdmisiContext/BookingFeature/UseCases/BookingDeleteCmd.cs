using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IDeleteBookingWorkflow _deleteBookingWorkflow;

    public BookingDeleteHandler(IDeleteBookingWorkflow deleteBookingWorkflow)
    {
        _deleteBookingWorkflow = deleteBookingWorkflow;
    }

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {
        
        Guard.Against.NullOrWhiteSpace(request.BookingId);
        _deleteBookingWorkflow.Execute(request);
        
        return Task.CompletedTask;
    }

    
}
