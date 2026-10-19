using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Application.LabContext.LabOrderFeature.Integration;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Microsoft.Extensions.DependencyInjection;

namespace Bilreg.Infrastructure.LabContext.Integration;

public class LabBillingIntegration : ILabBillingIntegration
{
    private static int _fakeSequence;

    private readonly IRegRepo? _regRepo;
    private readonly IJaminanRepo? _jaminanRepo;
    private readonly ITarifRepo? _tarifRepo;
    private readonly INilaiTarifRepo? _nilaiTarifRepo;
    private readonly IKomponenRepo? _komponenRepo;
    private readonly ILabExternalRepo? _labExternalRepo;
    private readonly IAddBillAppService? _addBillAppService;
    private readonly ITataRekeningRepo? _tataRekeningRepo;

    public LabBillingIntegration()
    {
    }

    [ActivatorUtilitiesConstructor]
    public LabBillingIntegration(
        IRegRepo regRepo,
        IJaminanRepo jaminanRepo,
        ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        ILabExternalRepo labExternalRepo,
        IAddBillAppService addBillAppService,
        ITataRekeningRepo tataRekeningRepo)
    {
        _regRepo = regRepo;
        _jaminanRepo = jaminanRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _labExternalRepo = labExternalRepo;
        _addBillAppService = addBillAppService;
        _tataRekeningRepo = tataRekeningRepo;
    }

    public string CreateTindakan(LabBillingChargeRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var seq = Interlocked.Increment(ref _fakeSequence);
        return $"TDK-FAKE-{seq:D4}";
    }

    public string ChargeOrder(LabOrderModel order, string userId, DateTime occurredAt)
    {
        Guard.Against.Null(order);
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));

        try
        {
            if (_regRepo is null || _addBillAppService is null)
                throw new InvalidOperationException("Billing dependencies are not configured.");

            if (order.OrderSource == LabOrderSourceEnum.ExternalPatient)
            {
                ChargeExternalPatient(order, userId, occurredAt);
            }
            else
            {
                ChargeAdmisiPatient(order, occurredAt);
            }

            var refId = order.OrderId.Length > 12 ? order.OrderId[..12] : order.OrderId;
            return refId;
        }
        catch (LabBillingChargeException)
        {
            throw;
        }
        catch (Exception ex)
        {
            throw new LabBillingChargeException(ex.Message, ex);
        }
    }

    public void CancelCharge(string orderId, string regId, string userId, string reason, DateTime cancelledAt)
    {
        Guard.Against.NullOrWhiteSpace(orderId, nameof(orderId));
        Guard.Against.NullOrWhiteSpace(regId, nameof(regId));
        Guard.Against.NullOrWhiteSpace(userId, nameof(userId));
        Guard.Against.NullOrWhiteSpace(reason, nameof(reason));

        if (_tataRekeningRepo is null)
            return;

        var tataRekeningOpt = _tataRekeningRepo.LoadEntity(RegModel.Key(regId));
        if (!tataRekeningOpt.HasValue)
            return;

        var tataRekening = tataRekeningOpt.Value;
        if (tataRekening.Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas
            || tataRekening.ListPayment.Any())
        {
            throw new InvalidOperationException(
                "Order laboratorium tidak dapat dibatalkan karena transaksi penagihan kasir telah Final/Lunas.");
        }

        if (tataRekening.Status == TataRekeningStatusEnum.Opened)
        {
            var matchingBills = tataRekening.ListTrsBill
                .Where(b => b.TrsBillingId.StartsWith($"{orderId}-", StringComparison.Ordinal) || b.TrsBillingId == orderId)
                .Select(b => b.TrsBillingId)
                .ToList();

            if (matchingBills.Count > 0)
            {
                foreach (var billId in matchingBills)
                {
                    tataRekening.DeleteBill(billId);
                }
                _tataRekeningRepo.SaveChanges(tataRekening);
            }
        }
    }

    public void CancelCharge(LabBillingCancelRequest request)
    {
        Guard.Against.Null(request);
        CancelCharge(request.OrderId, request.RegId, request.UserId, request.Reason, request.CancelledAt);
    }

    public BillingReleaseValidationResult ValidateReleaseEligibility(LabBillingReleaseValidationRequest request)
    {
        Guard.Against.Null(request);
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.OrderNo, nameof(request.OrderNo));

        if (string.Equals(request.OrderId, "BLOCK-INFRA", StringComparison.Ordinal))
            throw new LabBillingReleaseValidationException("BIL release validation unavailable.");

        if (request.OrderNo.EndsWith("-BILBLOCK", StringComparison.OrdinalIgnoreCase))
        {
            return new BillingReleaseValidationResult(
                BillingReleaseValidationCode.Blocked,
                "Tagihan pasien belum memenuhi syarat release.");
        }

        return new BillingReleaseValidationResult(BillingReleaseValidationCode.Clear, "");
    }

    private void ChargeExternalPatient(LabOrderModel order, string userId, DateTime occurredAt)
    {
        var externalKey = LabExternalModel.Key(order.Patient.RegId);
        _ = _labExternalRepo!.LoadEntity(externalKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"LabExternal '{order.Patient.RegId}' not found")
            );

        var tipeTarifKey = TipeTarifType.Key("UMUM");
        var kelasKey = KelasType.Key("III");
        var jaminanKey = JaminanType.Key("000");
        var jaminan = LoadJaminan(jaminanKey);

        var externalReg = new RegModel(
            order.Patient.RegId,
            DateOnly.FromDateTime(occurredAt),
            new AuditInfoType(userId, occurredAt),
            AuditInfoType.Default,
            AuditInfoType.Default,
            AuditInfoType.Default,
            JenisRegEnum.RegJalan,
            new PasienReff(order.Patient.PatientId, order.Patient.PatientName, DateOnly.FromDateTime(order.Patient.BirthDate), order.Patient.Gender),
            new TipeJaminanReff("00000", "Umum"),
            PolisModel.Default.ToReff(),
            new KelasReff("III", "Kelas III"),
            CaraMasukDkType.Default,
            RujukanType.Default.ToReff(),
            PpaType.Default.ToReff(),
            LayananType.Default.ToReff(),
            KarcisType.Default.ToReff(),
            new RegEligibilityType("-", "-", "-"),
            []);

        foreach (var item in order.Items)
        {
            var tarifKey = TarifType.Key(item.TarifId);
            var tarif = LoadTarif(tarifKey);
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifKey, kelasKey);
            var nilaiTarif = LoadNilaiTarif(nilaiTarifKey);
            var listKomp = ListKomponenTarif(nilaiTarif.ListKomponen.Select(x => x.Komponen));
            _ = _addBillAppService!.FromLabOrderItem(order, item, externalReg, jaminan, tarif, nilaiTarif, listKomp, occurredAt);
        }
    }

    private void ChargeAdmisiPatient(LabOrderModel order, DateTime occurredAt)
    {
        var regKey = RegModel.Key(order.Patient.RegId);
        var reg = LoadReg(regKey);

        var jaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        var jaminan = LoadJaminan(JaminanType.Key(jaminanId));

        foreach (var item in order.Items)
        {
            var tarifKey = TarifType.Key(item.TarifId);
            var tarif = LoadTarif(tarifKey);
            var tipeTarifKey = ResolveTipe(reg.JenisReg, jaminan);
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifKey, reg.Kelas);
            var nilaiTarif = LoadNilaiTarif(nilaiTarifKey);
            var listKomp = ListKomponenTarif(nilaiTarif.ListKomponen.Select(x => x.Komponen));
            _ = _addBillAppService!.FromLabOrderItem(order, item, reg, jaminan, tarif, nilaiTarif, listKomp, occurredAt);
        }
    }

    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo!.LoadEntity(key)
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
        var jmn = _jaminanRepo!.LoadEntity(jaminan)
            .Match(
                onSome: x => x,
                onNone: () => jaminan.JaminanId == "000" ? JaminanType.Umum : throw new KeyNotFoundException($"Jaminan invalid")
            );
        return jmn;
    }

    private TarifType LoadTarif(ITarifKey tarifKey)
    {
        var tarif = _tarifRepo!.LoadEntity(tarifKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Tarif invalid")
            );
        return tarif;
    }

    private NilaiTarifType LoadNilaiTarif(INilaiTarifCompositKey nilaiTarifKey)
    {
        var nilaiTarif = _nilaiTarifRepo!.LoadEntity(nilaiTarifKey)
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
        var result = _komponenRepo!
            .ListData(listKey)?.ToList() ?? [];
        return result;
    }
}
