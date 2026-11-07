using Bilreg.Domain.AdmisiRanapContext.InpatientOrderFeature;
using Bilreg.Domain.AdmisiRanapContext.OpnameRequestFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.InpatientOrderFeature.UseCases;

public record AdmListOpnameRequestQry(OpnameRequestStatusEnum? Status = null)
    : IRequest<AdmListOpnameRequestResponse>;

public record AdmListOpnameRequestResponse(IReadOnlyList<AdmOpnameRequestListItem> Items);

public record AdmOpnameRequestListItem(
    string OpnameRequestId,
    OpnameRequestStatusEnum OpnameRequestStatus,
    string PasienId,
    string PasienName,
    string DokterId,
    string DokterName,
    DateTime CrtDate);

public class AdmListOpnameRequestHandler : IRequestHandler<AdmListOpnameRequestQry, AdmListOpnameRequestResponse>
{
    private readonly IInpatientOrderRepo _orderRepo;

    public AdmListOpnameRequestHandler(IInpatientOrderRepo orderRepo) =>
        _orderRepo = orderRepo;

    public Task<AdmListOpnameRequestResponse> Handle(
        AdmListOpnameRequestQry request,
        CancellationToken cancellationToken)
    {
        InpatientOrderStatusEnum? statusFilter = request.Status switch
        {
            OpnameRequestStatusEnum.Requested => InpatientOrderStatusEnum.Requested,
            OpnameRequestStatusEnum.Fulfilled => InpatientOrderStatusEnum.Fulfilled,
            OpnameRequestStatusEnum.Cancelled => InpatientOrderStatusEnum.Cancelled,
            _ => null
        };

        var items = _orderRepo
            .ListData(new InpatientOrderListFilter(Status: statusFilter))
            .Where(x => x.OrderSource is OrderSourceEnum.EmrRawatJalan or OrderSourceEnum.EmrIgd)
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmListOpnameRequestResponse(items));
    }

    private static AdmOpnameRequestListItem Map(InpatientOrderModel order)
    {
        var opnameStatus = order.OrderStatus switch
        {
            InpatientOrderStatusEnum.Fulfilled => OpnameRequestStatusEnum.Fulfilled,
            InpatientOrderStatusEnum.Cancelled => OpnameRequestStatusEnum.Cancelled,
            _ => OpnameRequestStatusEnum.Requested
        };

        return new AdmOpnameRequestListItem(
            order.InpatientOrderId,
            opnameStatus,
            order.Pasien.PasienId,
            order.Pasien.PasienName,
            order.Dokter.PpaId,
            order.Dokter.PpaName,
            order.AuditTrail.Created.Timestamp);
    }
}
