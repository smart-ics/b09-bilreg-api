using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record OrderTindakanGetQuery(string OrderTdkId) : IRequest<OrderTindakanGetResponse>, IOrderTdkKey;

public record OrderTindakanGetResponse(
    string OrderId,
    string OrderDate,
    PasienReff Pasien,
    RegReff Reg,
    PpaReff DokterOrder,
    LayananReff Layanan,
    TarifReff Tarif,
    string FreeTextOrder,
    int StatusOrder,
    string StatusOrderString);

public class OrderTindakanGetHandler : IRequestHandler<OrderTindakanGetQuery,  OrderTindakanGetResponse>
{
    private readonly IOrderTdkRepo _repo;

    public OrderTindakanGetHandler(IOrderTdkRepo repo)
    {
        _repo = repo;
    }

    public Task<OrderTindakanGetResponse> Handle(OrderTindakanGetQuery request, CancellationToken cancellationToken)
    {
        var order = _repo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Order {request.OrderTdkId} not found")
            );
        var result = new OrderTindakanGetResponse(order.OrderTdkId, order.OrderTdkDate.ToString("yyyy-MM-dd HH:mm:ss"),
            order.Pasien, order.Reg, order.DokterOrder, order.Layanan, order.Tarif, order.FreeTextOrder,
            (int)order.StatusOrder, order.StatusOrder.ToString());
        return Task.FromResult(result);
    }
}
