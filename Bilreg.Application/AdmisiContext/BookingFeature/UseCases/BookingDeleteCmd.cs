// TODO: Refactor AntrianMap Model

// using Ardalis.GuardClauses;
// using Bilreg.Domain.AdmisiContext.BookingFeature;
// using MediatR;
//
// namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;
//
// public record BookingDeleteCmd(string BookingId) : IRequest, IBookingKey;
//
// public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
// {
//     private readonly IDeleteBookingWorkflow _deleteBookingWorkflow;
//     private readonly IDashboardEmrRemoveBookingService _dashboardEmrRemoveSvc;
//     public BookingDeleteHandler(IDeleteBookingWorkflow deleteBookingWorkflow, 
//         IDashboardEmrRemoveBookingService dashboardEmrRemoveSvc)
//     {
//         _deleteBookingWorkflow = deleteBookingWorkflow;
//         _dashboardEmrRemoveSvc = dashboardEmrRemoveSvc;
//     }
//
//     public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
//     {
//         
//         Guard.Against.NullOrWhiteSpace(request.BookingId);
//         _deleteBookingWorkflow.Execute(request);
//         
//         var removeBooking = new RemoveBookingCmd(request.BookingId);
//         _dashboardEmrRemoveSvc.Execute(removeBooking);
//
//         return Task.CompletedTask;
//     }
//
//     
// }
