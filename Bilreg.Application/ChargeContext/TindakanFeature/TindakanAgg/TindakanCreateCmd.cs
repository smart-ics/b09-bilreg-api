using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;

public record TindakanCreateCmd(
    string RegId, string LayananId, string TipeTarifId,
    string OrderTdkId, string KelasId, TindakanTarifCreate Tarif) 
    : IRequest<TindakanCreateRespose>, IRegKey, ILayananKey, ITipeTarifKey, IOrderTdkKey, IKelasKey;

public record TindakanTarifCreate(string TarifId, IEnumerable<TindakanKomponenCreate> Komponen) : ITarifKey;
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
    public TindakanCreateHandler(IRegRepo regRepo,
        ILayananRepo layananRepo,
        ITipeTarifRepo tipeTarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IPpaRepo ppaRepo,
        IOrderTdkRepo orderTdkRepo,
        ITindakanRepo tindakanRepo)
    {
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _ppaRepo = ppaRepo;
        _orderTdkRepo = orderTdkRepo;
        _tindakanRepo = tindakanRepo;
    }

    public Task<TindakanCreateRespose> Handle(TindakanCreateCmd request, CancellationToken cancellationToken)
    {

        var reg = _regRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {request.RegId} not found")
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
                onNone: () => throw new KeyNotFoundException($"Order tindakan {request.OrderTdkId} not found")
            );
        //var nilaiTarif = _nilaiTarifRepo.LoadEntity()

        throw new NotImplementedException();
    }
}