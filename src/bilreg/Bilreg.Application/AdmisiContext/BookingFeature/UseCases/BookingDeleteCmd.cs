 using Ardalis.GuardClauses;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

 namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingDeleteCmd(string BookingId, string UserId, string VoidReason,
    string ClientIpAddress, string UserAgent) : IRequest, IBookingKey;

public class BookingDeleteHandler : IRequestHandler<BookingDeleteCmd>
{
    private readonly IDeleteBookingWorkflow _deleteBookingWorkflow;
    private readonly IDashboardEmrRemoveBookingService _dashboardEmrRemoveSvc;
    private readonly IAuditRepo _auditRepo;
    public readonly IBookingRepo _bookingRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public BookingDeleteHandler(IDeleteBookingWorkflow deleteBookingWorkflow,
        IDashboardEmrRemoveBookingService dashboardEmrRemoveSvc,
        IAuditRepo auditRepo,
        IBookingRepo bookingRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _deleteBookingWorkflow = deleteBookingWorkflow;
        _dashboardEmrRemoveSvc = dashboardEmrRemoveSvc;
        _auditRepo = auditRepo;
        _bookingRepo = bookingRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(BookingDeleteCmd request, CancellationToken cancellationToken)
    {

        Guard.Against.NullOrWhiteSpace(request.BookingId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.VoidReason);
        var booking = _bookingRepo.LoadEntity(request)
            .GetValueOrDefault(BookingModel.Default);
        var snapshotJson = AuditLogSnapshotJson.Serialize(booking);
        
        _deleteBookingWorkflow.Execute(request);

        var removeBooking = new RemoveBookingCmd(request.BookingId);
        _dashboardEmrRemoveSvc.Execute(removeBooking);

        var audit = CreateAudit(snapshotJson, request, _tglJamProvider.Now);
        _auditRepo.SaveChanges(audit);

        return Task.CompletedTask;
    }
    private static AuditLog CreateAudit(string snapShotJson, BookingDeleteCmd cmd, DateTime occurredAt)
    {
        var result = AuditLog.Create(
            cmd.UserId,
            occurredAt,
            actionType: "DELETE",
            entityName: nameof(BookingModel),
            entityId: cmd.BookingId,
            reason: cmd.VoidReason,
            originalDataJson: snapShotJson,
            correlationId: cmd.BookingId,
            clientIpAddress: cmd.ClientIpAddress,
            userAgent: cmd.UserAgent
            );
        return result;
    }

}
