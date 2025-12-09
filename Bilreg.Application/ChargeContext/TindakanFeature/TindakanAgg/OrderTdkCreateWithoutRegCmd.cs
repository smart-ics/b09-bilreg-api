using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record OrderTdkCreateWithoutRegCmd(
    string PasienId, string LayananId, string DokterId, 
    string TarifId, string TarifnName, string UserId) : IRequest<OrderTdkCreateWithoutRegResponse>,
    IPasienKey, ILayananKey, ITarifKey;

public record OrderTdkCreateWithoutRegResponse(string OrderTdkId);

public class OrderTdkCreateWithoutRegHandler : IRequestHandler<OrderTdkCreateWithoutRegCmd, OrderTdkCreateWithoutRegResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly ILayananRepo _lynRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IOrderTdkRepo _orderTdkRepo;
    public OrderTdkCreateWithoutRegHandler(IPasienRepo pasienRepo,
        ILayananRepo lynRepo,
        IPpaRepo ppaRepo,
        ITarifRepo tarifRepo,
        IOrderTdkRepo orderTdkRepo)
    {
        _pasienRepo = pasienRepo;
        _lynRepo = lynRepo;
        _ppaRepo = ppaRepo;
        _tarifRepo = tarifRepo;
        _orderTdkRepo = orderTdkRepo;
    }

    public Task<OrderTdkCreateWithoutRegResponse> Handle(OrderTdkCreateWithoutRegCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        var pasien = _pasienRepo.LoadEntity(request)
            .Match
            (
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien {request.PasienId} not found")
            );
        var layanan = _lynRepo.LoadEntity(request)
            .Match
            (
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan {request.LayananId} not found")
            );
        var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId))
            .Match
            (
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterId} not found")
            );

        // BUILD
        var tarif = _tarifRepo.LoadEntity(request).GetValueOrDefault();
        var freeTextOrder = request.TarifnName;
        OrderTdkModel order;
        if (tarif is not null)
            order = OrderTdkModel.Create(pasien, ppa, layanan, tarif, request.UserId);
        else
            order = OrderTdkModel.Create(pasien, ppa, layanan, freeTextOrder, request.UserId);

        // WRITE
        _orderTdkRepo.SaveChanges(order);

        // RESPONSE
        return Task.FromResult(new OrderTdkCreateWithoutRegResponse(order.OrderTdkId));


        throw new NotImplementedException();
    }
}