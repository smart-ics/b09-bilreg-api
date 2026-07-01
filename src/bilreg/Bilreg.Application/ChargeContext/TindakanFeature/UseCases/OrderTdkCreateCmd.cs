using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record OrderTdkCreateCmd(string RegId, string LayananId,
    string DokterId, string TarifId, string TarifName, string UserId) : IRequest<OrderTdkCreateResponse>,
    IRegKey, ILayananKey, ITarifKey;

public record OrderTdkCreateResponse(string OrderTdkId);

public class OrderTdkCreatehandler : IRequestHandler<OrderTdkCreateCmd, OrderTdkCreateResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _lynRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IOrderTdkRepo _orderTdkRepo;
    public OrderTdkCreatehandler(IRegRepo regRepo,
        IPpaRepo ppaRepo,
        ILayananRepo lynRepo,
        ITarifRepo tarifRepo,
        IOrderTdkRepo orderTdkRepo)
    {
        _regRepo = regRepo;
        _ppaRepo = ppaRepo;
        _lynRepo = lynRepo;
        _tarifRepo = tarifRepo;
        _orderTdkRepo = orderTdkRepo;
    }

    public Task<OrderTdkCreateResponse> Handle(OrderTdkCreateCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.Null(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        if (string.IsNullOrWhiteSpace(request.TarifId) &&
            string.IsNullOrWhiteSpace(request.TarifName))
        {
            throw new ArgumentException("tindakan wajib diisi");
        }

        var reg = _regRepo.LoadEntity(request)
            .Match
            (
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {request.RegId} not found")
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
        var freeTextOrder = request.TarifName;
        OrderTdkModel order;
        if (tarif is not null)
            order = OrderTdkModel.Create(reg, ppa, layanan, tarif, request.UserId);
        else
            order = OrderTdkModel.Create(reg, ppa, layanan, freeTextOrder, request.UserId);
        
        // WRITE
        _orderTdkRepo.SaveChanges(order);

        // RESPONSE
        return Task.FromResult(new OrderTdkCreateResponse(order.OrderTdkId));
    }
}
