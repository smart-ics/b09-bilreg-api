using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using JetBrains.Annotations;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record TindakanCreateCmd(
    string RegId, string LayananId, string TipeTarifId,
    string OrderTdkId, string KelasId, int JenisTindakan, 
    string TarifId, IEnumerable<TindakanKomponenCreate> Komponen, string UserId) 
    : IRequest<TindakanCreateRespose>, 
    IRegKey, ILayananKey, IOrderTdkKey, INilaiTarifCompositKey;

public record TindakanKomponenCreate (string KomponenId, string PpaId, int qty);

public record TindakanCreateRespose(string TindakanId);

public class TindakanCreateHandler : IRequestHandler<TindakanCreateCmd, TindakanCreateRespose>
{
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IOrderTdkRepo _orderTdkRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly ITarifRepo _tarifRepo;
    public TindakanCreateHandler(IRegRepo regRepo,
        ILayananRepo layananRepo,
        ITipeTarifRepo tipeTarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IPpaRepo ppaRepo,
        IOrderTdkRepo orderTdkRepo,
        ITindakanRepo tindakanRepo,
        IPasienRepo pasienRepo,
        ITarifRepo tarifRepo)
    {
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _ppaRepo = ppaRepo;
        _orderTdkRepo = orderTdkRepo;
        _tindakanRepo = tindakanRepo;
        _pasienRepo = pasienRepo;
        _tarifRepo = tarifRepo;
    }

    public Task<TindakanCreateRespose> Handle(TindakanCreateCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.LayananId);
        Guard.Against.NullOrWhiteSpace(request.TarifId);
        Guard.Against.NullOrWhiteSpace(request.TipeTarifId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);

        var reg = _regRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {request.RegId} not found")
            );
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(reg.Pasien.PasienId))
            .Match(
                onSome: x => x,
                onNone : () => throw new KeyNotFoundException($"pasien {reg.Pasien.PasienId} not found")
            );
        var layanan = _layananRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan {request.LayananId} not found")
            );
        var tipeTarif = _tipeTarifRepo.LoadEntity(TipeTarifType.Key(request.TipeTarifId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tipe Tarif {request.TipeTarifId} not found")
            );
        var orderTdk = _orderTdkRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => OrderTdkModel.Default
            );
        var tarif = _tarifRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tarif {request.TarifId} not found")
            );
        var nilaiTarif = _nilaiTarifRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Nilai Tarif {request.TarifId} not found")
            );
        
        var tdkTarif = BuildTindakanTarif(request, tarif, nilaiTarif);

        var tindakan = TindakanModel.Create((JenisTindakanEnum)request.JenisTindakan, orderTdk,
            pasien, reg, layanan, tipeTarif, tdkTarif, request.UserId);
        

        if(orderTdk.OrderTdkId != "-")
        {
            orderTdk.Execute(request.UserId);
            _orderTdkRepo.SaveChanges(orderTdk);
        }
        
        _tindakanRepo.SaveChanges(tindakan);


        return Task.FromResult(new TindakanCreateRespose(tindakan.TindakanId));
    }

    public TindakanTarifModel BuildTindakanTarif(
    TindakanCreateCmd request,
    TarifType tarif,
    NilaiTarifType nilaiTarif)
    {
        var tindakanTarif = new TindakanTarifModel(tarif, []);
        request.Komponen
            .Select(d =>
            {
                var kompo = nilaiTarif.ListKomponen
                    .FirstOrDefault(x => x.Komponen.KomponenId == d.KomponenId)
                    ?? NilaiTarifKomponenType.Default;

                var komponen = new KomponenType(
                    kompo.Komponen.KomponenId, kompo.Komponen.KomponenName,
                    GroupKomponenType.Default, []);

                var ppa = string.IsNullOrWhiteSpace(d.PpaId)
                    ? PpaType.Default
                    : GetPpa(PpaType.Key(d.PpaId));

                return new
                {
                    Komponen = komponen,
                    Ppa = ppa,
                    Qty = d.qty,
                    Nilai = kompo.Nilai
                };
            })
            .ToList()
            .ForEach(x =>
            {
                tindakanTarif.SetKomponen(x.Komponen, x.Ppa, x.Qty, x.Nilai);
            });

        return tindakanTarif;
    }

    private PpaType GetPpa(IPpaKey ppakey)
    {
        return _ppaRepo.LoadEntity(ppakey)
            .Match(
                onSome: x => x,
                onNone: () => PpaType.Default);
    }

}