using Ardalis.GuardClauses;
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
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

public record OrderTdkCreateWithoutRegCmd(
    string PasienId, string LayananId, string DokterId, 
    string TarifId, string TarifName, string UserId) : IRequest<OrderTdkCreateWithoutRegResponse>,
    IPasienKey, ILayananKey, ITarifKey;

public record OrderTdkCreateWithoutRegResponse(string OrderTdkId);

public class OrderTdkCreateWithoutRegHandler : IRequestHandler<OrderTdkCreateWithoutRegCmd, OrderTdkCreateWithoutRegResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly ILayananRepo _lynRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly IOrderTdkRepo _orderTdkRepo;
    private readonly ITglJamProvider _tglJamProvider;
    public OrderTdkCreateWithoutRegHandler(IPasienRepo pasienRepo,
        ILayananRepo lynRepo,
        IPpaRepo ppaRepo,
        ITarifRepo tarifRepo,
        IOrderTdkRepo orderTdkRepo,
        ITglJamProvider tglJamProvider)
    {
        _pasienRepo = pasienRepo;
        _lynRepo = lynRepo;
        _ppaRepo = ppaRepo;
        _tarifRepo = tarifRepo;
        _orderTdkRepo = orderTdkRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<OrderTdkCreateWithoutRegResponse> Handle(OrderTdkCreateWithoutRegCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.Null(request.PasienId, nameof(request.PasienId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        if (string.IsNullOrWhiteSpace(request.TarifId) &&
            string.IsNullOrWhiteSpace(request.TarifName))
        {
            throw new ArgumentException("tindakan wajib diisi");
        }

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
        var freeTextOrder = request.TarifName;
        var occurredAt = _tglJamProvider.Now;
        OrderTdkModel order;
        if (tarif is not null)
            order = OrderTdkModel.Create(pasien, ppa, layanan, tarif, request.UserId, occurredAt);
        else
            order = OrderTdkModel.Create(pasien, ppa, layanan, freeTextOrder, request.UserId, occurredAt);

        // WRITE
        _orderTdkRepo.SaveChanges(order);

        // RESPONSE
        return Task.FromResult(new OrderTdkCreateWithoutRegResponse(order.OrderTdkId));


        throw new NotImplementedException();
    }
}
