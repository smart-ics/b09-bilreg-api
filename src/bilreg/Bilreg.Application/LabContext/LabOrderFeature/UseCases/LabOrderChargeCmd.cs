using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderChargeCmd(string OrderId, string UserId)
    : IRequest<LabOrderChargeResponse>, ILabOrderKey;

public record LabOrderChargeResponse(
    bool Success,
    string? BillingTindakanId,
    string? BillingLastError);

public class LabOrderChargeHandler : IRequestHandler<LabOrderChargeCmd, LabOrderChargeResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly IRegRepo _regRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IAddBillAppService _addBillAppService;
    private readonly ILabBillingIntegration _labBillingIntegration;
    private readonly ITglJamProvider _tglJamProvider;
    
    public LabOrderChargeHandler(ILabOrderRepo labOrderRepo, IRegRepo regRepo, ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo, IKomponenRepo komponenRepo, IJaminanRepo jaminanRepo,
        IAddBillAppService addBillAppService, ILabBillingIntegration labBillingIntegration, ITglJamProvider tglJamProvider)
    {
        _labOrderRepo = labOrderRepo;
        _regRepo = regRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _jaminanRepo = jaminanRepo;
        _addBillAppService = addBillAppService;
        _labBillingIntegration = labBillingIntegration;
        _tglJamProvider = tglJamProvider;
    }

    public Task<LabOrderChargeResponse> Handle(LabOrderChargeCmd request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var order = _labOrderRepo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");
        var occurredAt = _tglJamProvider.Now;
        order.Charge(request.UserId);

        // BUILD
        var regKey = RegModel.Key(order.Patient.RegId);
        var reg = LoadReg(regKey);

        var jaminanKey = reg.TipeJaminan.TipeJaminanId[..3];
        var jaminan = LoadJaminan(JaminanType.Key(jaminanKey));

        order.Items.ForEach(item =>
        {
            var tarifKey = TarifType.Key(item.TarifId);
            var tarif = LoadTarif(tarifKey);
            var tipeTarifKey = ResolveTipe(reg.JenisReg, jaminan);
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifKey, reg.Kelas);
            var nilaiTarif = LoadNilaiTarif(nilaiTarifKey);
            var listKomp = ListKomponenTarif(nilaiTarif.ListKomponen.Select(x => x.Komponen));
            _ = _addBillAppService.FromLabOrderItem(order, item, reg, jaminan, tarif, nilaiTarif, listKomp, occurredAt);
        });

        var success = false;
        try
        {
            var tarifLines = order.Items
                .Select(x => new LabBillingTarifLine(x.TarifId, x.TarifCode, x.TarifName))
                .ToList();
            var tindakanId = _labBillingIntegration.CreateTindakan(
                new LabBillingChargeRequest(request.OrderId, request.UserId, tarifLines));
            order.MarkCharged(tindakanId, request.UserId, occurredAt);
            success = true;
        }
        catch (LabBillingChargeException ex)
        {
            order.RecordBillingError(ex.Message, request.UserId, occurredAt);
        }

        LabOrderChargeResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderChargeResponse(
                success,
                string.IsNullOrWhiteSpace(order.BillingTindakanId) ? null : order.BillingTindakanId,
                string.IsNullOrWhiteSpace(order.BillingLastError) ? null : order.BillingLastError);
        }

        return Task.FromResult(response);
    }

    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {key.RegId} not found")
            );

        return !result.IsAktif
            ? throw new ArgumentException($"Register '{key.RegId}' tidak aktif")
            : result;
    }

    private JaminanType LoadJaminan(IJaminanKey jaminan)
    {
        var jmn = _jaminanRepo.LoadEntity(jaminan)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Jaminan invalid")
            );
        return jmn;
    }
    
    private TarifType LoadTarif(ITarifKey tarifKey)
    {
        var tarif = _tarifRepo.LoadEntity(tarifKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tarif invalid")
            );
        return tarif;
    }

    private NilaiTarifType LoadNilaiTarif(INilaiTarifCompositKey nilaiTarifKey)
    {
        var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiTarifKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Nilai Tarif invalid")
            );
        return nilaiTarif;
    }

    private static TipeTarifReff ResolveTipe(JenisRegEnum jenisReg, JaminanType jaminan)
    {
        return jenisReg switch
        {
            JenisRegEnum.RegJalan or JenisRegEnum.Darurat
                => new TipeTarifReff(jaminan.TipeTarif.Rajal.TipeTarifId, jaminan.TipeTarif.Rajal.TipeTarifName),

            JenisRegEnum.RegInap
                => new TipeTarifReff(jaminan.TipeTarif.Ranap.TipeTarifId, jaminan.TipeTarif.Ranap.TipeTarifName),

            _ => new TipeTarifReff("-", "-")
        };
    }

    private List<KomponenType> ListKomponenTarif(IEnumerable<IKomponenKey> listKey)
    {
        var result = _komponenRepo
            .ListData(listKey)?.ToList() ?? [];
        return result;
    }
}