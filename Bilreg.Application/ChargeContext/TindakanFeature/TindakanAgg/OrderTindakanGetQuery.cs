using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record OrderTindakanGetQuery(string OrderId) : IRequest<OrderTindakanGetResponse>, IOrderTindakanKey;

public record OrderTindakanGetResponse(
    string OrderId,
    string OrderDate,
    PasienReff Pasien,
    RegReff Reg,
    PpaReff DokterOrder,
    LayananReff Layanan,
    int StatusOrder,
    string StatusOrderString,
    TarifReff Tindakan);

public class OrderTindakanGetHandler : IRequestHandler<OrderTindakanGetQuery,  OrderTindakanGetResponse>
{
    private readonly IOrderTindakanRepo _repo;

    public OrderTindakanGetHandler(IOrderTindakanRepo repo)
    {
        _repo = repo;
    }

    public Task<OrderTindakanGetResponse> Handle(OrderTindakanGetQuery request, CancellationToken cancellationToken)
    {
        var order = _repo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Order {request.OrderId} not found")
            );
        var result = new OrderTindakanGetResponse(order.OrderId, order.OrderDate.ToString("yyyy-MM-dd HH:mm:ss"),
            order.Pasien, order.Reg, order.DokterOrder, order.Layanan, (int)order.StatusOrder, order.StatusOrder.ToString(),
            order.Tindakan);
        return Task.FromResult(result);
    }
}
