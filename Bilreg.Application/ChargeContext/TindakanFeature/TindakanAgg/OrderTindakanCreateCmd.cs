using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using System;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record OrderTindakanCreateCmd(string PasienId, string RegId, string LayananId,
    string DokterId, string TindakanId, string TindakanName, string UserId) : IRequest<OrderTindakanCreateResponse>, 
    IPasienKey, IRegKey, ILayananKey;

public record OrderTindakanCreateResponse(string OrderId);

public class OrderTindakanCreatehandler : IRequestHandler<OrderTindakanCreateCmd, OrderTindakanCreateResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly IRegRepo _regRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILayananRepo _lynRepo;
    //private readonly ITarifRepo _tarifRepo;
    private readonly IOrderTindakanRepo _orderTdkRepo;
    public OrderTindakanCreatehandler(IPasienRepo pasienRepo,
        IRegRepo regRepo,
        IPpaRepo ppaRepo,
        ILayananRepo lynRepo,
        //ITarifRepo tarifRepo,
        IOrderTindakanRepo orderTdkRepo)
    {
        _pasienRepo = pasienRepo;
        _regRepo = regRepo;
        _ppaRepo = ppaRepo;
        _lynRepo = lynRepo;
        //_tarifRepo = tarifRepo;
        _orderTdkRepo = orderTdkRepo;
    }

    public Task<OrderTindakanCreateResponse> Handle(OrderTindakanCreateCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.PasienId, nameof(request.PasienId));
        Guard.Against.Null(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));

        if (string.IsNullOrWhiteSpace(request.TindakanId) &&
            string.IsNullOrWhiteSpace(request.TindakanName))
        {
            throw new ArgumentException("tindakan wajib diisi");
        }

        var pasien = _pasienRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien {request.PasienId} not found")
            );
        var reg = _regRepo.LoadEntity(request)
            .Match
            (
                onSome: x => x,
                onNone : () => RegModel.Default
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
        var tarif = new TarifReff(request.TindakanId, request.TindakanName);
        //var tarif = request.TindakanId == string.Empty ? TarifType.Default with { TarifName = request.TindakanName } 
        //    : GetTarif(TarifType.Key(request.TindakanId!));

        // BUILD
        var auditOrderTdk = AuditTrailType.Create(request.UserId, DateTime.Now);
        var order = OrderTindakanModel.Create(auditOrderTdk, pasien.ToReff(), reg.ToReff(), ppa.ToReff(), 
            layanan.ToReff(), tarif);
       
        // WRITE
        _orderTdkRepo.SaveChanges(order);
        
        // RESPONSE
        return Task.FromResult(new OrderTindakanCreateResponse(order.OrderId));
    }

    //private TarifType GetTarif(ITarifKey tarifKey)
    //{
    //    var tarif = _tarifRepo.LoadEntity(tarifKey)
    //        .Match(
    //            onSome: x => x,
    //            onNone: () => throw new KeyNotFoundException($"Tarif {tarifKey.TarifId} not found")
    //        );
    //    return tarif;
    //}
}
