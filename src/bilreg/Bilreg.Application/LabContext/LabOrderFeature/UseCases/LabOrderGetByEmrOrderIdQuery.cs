using Ardalis.GuardClauses;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderGetByEmrOrderIdQuery(string EmrOrderId) : IRequest<LabOrderEmrStatusResponse>;

public record LabOrderEmrStatusResponse(
    string EmrOrderId,
    int LabOrderStatus,
    int LastBillingReleaseStatus,
    int OwareStatus,
    string OrderNo,
    DateTime CreatedDate,
    string? BillingTindakanId,
    string? BillingLastError,
    string? CancelledReason,
    DateTime? CancelledDate);

public class LabOrderGetByEmrOrderIdHandler : IRequestHandler<LabOrderGetByEmrOrderIdQuery, LabOrderEmrStatusResponse>
{
    private readonly ILabOrderRepo _repo;

    public LabOrderGetByEmrOrderIdHandler(ILabOrderRepo repo)
    {
        _repo = repo;
    }

    public Task<LabOrderEmrStatusResponse> Handle(
        LabOrderGetByEmrOrderIdQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.EmrOrderId, nameof(request.EmrOrderId));

        var order = _repo.LoadByEmrOrderId(request.EmrOrderId)
            .GetValueOrThrow($"LabOrder for EmrOrderId '{request.EmrOrderId}' not found");

        var emptyDate = new DateTime(3000, 1, 1);
        var response = new LabOrderEmrStatusResponse(
            order.EmrOrderId,
            (int)order.LabOrderStatus,
            (int)order.LastBillingReleaseStatus,
            (int)order.OwareStatus,
            order.OrderNo,
            order.AuditTrail.Created.Timestamp,
            string.IsNullOrWhiteSpace(order.BillingTindakanId) ? null : order.BillingTindakanId,
            string.IsNullOrWhiteSpace(order.BillingLastError) ? null : order.BillingLastError,
            string.IsNullOrWhiteSpace(order.CancelledReason) ? null : order.CancelledReason,
            order.CancelledDate == emptyDate ? null : order.CancelledDate);

        return Task.FromResult(response);
    }
}
