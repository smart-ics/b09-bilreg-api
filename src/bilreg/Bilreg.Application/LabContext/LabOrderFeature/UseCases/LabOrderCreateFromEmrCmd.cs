using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCreateFromEmrCmd(
    string UserId,
    string EmrOrderId,
    string RegId,
    string PatientId,
    string PatientName,
    string BirthDateYmd,
    string Gender,
    List<LabOrderTarifItemInput> Items,
    string DokterPerujukId = "",
    string DokterPerujukName = "",
    string LayananPerujukId = "",
    string LayananPerujukName = "",
    string ClinicalNote = "",
    int Urgency = 0,
    bool AllowDuplicateConfirmation = false)
    : IRequest<LabOrderCreateFromEmrResponse>;

public record LabOrderCreateFromEmrResponse(
    string EmrOrderId,
    int LabOrderStatus,
    string OrderNo,
    string OrderId = "");

public class LabOrderCreateFromEmrHandler : IRequestHandler<LabOrderCreateFromEmrCmd, LabOrderCreateFromEmrResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ILabTestResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegRepo _regRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly ILabOrderDal _labOrderDal;

    public LabOrderCreateFromEmrHandler(
        ILabOrderRepo labOrderRepo,
        ISequencer sequencer,
        ILabTestResolutionService resolutionService,
        ITglJamProvider tglJamProvider,
        IRegRepo regRepo,
        IPpaRepo ppaRepo,
        ILabOrderDal labOrderDal)
    {
        _labOrderRepo = labOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
        _regRepo = regRepo;
        _ppaRepo = ppaRepo;
        _labOrderDal = labOrderDal;
    }

    public Task<LabOrderCreateFromEmrResponse> Handle(LabOrderCreateFromEmrCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.EmrOrderId, nameof(request.EmrOrderId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.PatientId, nameof(request.PatientId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        // 1. Validasi status kunjungan aktif ke ADM-REG (EX-01)
        var reg = _regRepo.LoadEntity(RegModel.Key(request.RegId))
            .Match(
                onSome: x => x,
                onNone: () => null as RegModel
            );

        if (reg is null)
            throw new InvalidOperationException($"EX-01: Registrasi kunjungan '{request.RegId}' tidak ditemukan.");

        if (!reg.IsAktif)
            throw new InvalidOperationException($"EX-01: Registrasi kunjungan '{request.RegId}' sudah tidak aktif atau sudah discharge.");

        // 2. Validasi dokter perujuk ke ORG-PPA jika DokterPerujukId diisi
        var dokterPerujukName = request.DokterPerujukName;
        if (!string.IsNullOrWhiteSpace(request.DokterPerujukId))
        {
            var ppa = _ppaRepo.LoadEntity(PpaType.Key(request.DokterPerujukId))
                .Match(
                    onSome: x => x,
                    onNone: () => null as PpaType
                );

            if (ppa is null || ppa.PpaId == "-" || string.IsNullOrWhiteSpace(ppa.PpaId))
                throw new InvalidOperationException($"Dokter perujuk '{request.DokterPerujukId}' tidak ditemukan atau tidak aktif.");

            if (string.IsNullOrWhiteSpace(dokterPerujukName))
                dokterPerujukName = ppa.PpaName;
        }

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var patientId = !string.IsNullOrWhiteSpace(reg?.Pasien?.PasienId) && reg.Pasien.PasienId != "-"
            ? reg.Pasien.PasienId
            : request.PatientId;
        var snapshot = LabOrderCreateHelper.BuildSnapshot(
            request.RegId,
            patientId,
            request.PatientName,
            request.BirthDateYmd,
            request.Gender,
            audit.Timestamp);
        var lines = LabOrderCreateHelper.MapResolvedLines(_resolutionService.ResolveByTarifItems(request.Items));

        // 3. Deteksi tes duplikat 24 jam (EX-05)
        var activeTests = _labOrderDal.ListActiveTestsByPatientWithinHours(patientId, 24, _tglJamProvider.Now)?.ToList() ?? [];
        if (activeTests.Count > 0)
        {
            var duplicates = activeTests
                .Where(a => lines.Any(l =>
                    (!string.IsNullOrWhiteSpace(a.TestDefinitionId) && a.TestDefinitionId == l.Item.TestDefinitionId) ||
                    (!string.IsNullOrWhiteSpace(a.LabTestCode) && a.LabTestCode == l.Item.LabTestCode)))
                .ToList();

            if (duplicates.Count > 0 && !request.AllowDuplicateConfirmation)
            {
                var matchingLines = lines.Where(l => duplicates.Any(a =>
                    (!string.IsNullOrWhiteSpace(a.TestDefinitionId) && a.TestDefinitionId == l.Item.TestDefinitionId) ||
                    (!string.IsNullOrWhiteSpace(a.LabTestCode) && a.LabTestCode == l.Item.LabTestCode))).ToList();
                var dupNames = duplicates.Select(d => d.LabTestName)
                    .Concat(matchingLines.Select(l => l.Item.TarifName))
                    .Where(n => !string.IsNullOrWhiteSpace(n))
                    .Distinct()
                    .ToList();
                var dupOrderNos = duplicates.Select(d => string.IsNullOrWhiteSpace(d.OrderNo) ? d.OrderId : d.OrderNo).Distinct().ToList();
                var namesJoined = string.Join(", ", dupNames);
                var ordersJoined = string.Join(", ", dupOrderNos);
                throw new DuplicateLabOrderWarningException(
                    dupNames,
                    dupOrderNos,
                    $"EX-05: Peringatan duplikasi tes laboratorium dalam 24 jam terakhir untuk pasien ini. Pemeriksaan '{namesJoined}' telah dipesan pada order sebelumnya: {ordersJoined}. Konfirmasikan atau berikan justifikasi medis.");
            }
        }

        var orderNo = LabOrderCreateHelper.NextOrderNo(_sequencer);
        var urgency = (LabOrderUrgencyEnum)request.Urgency;

        var order = LabOrderModel.CreateFromEmr(
            request.EmrOrderId,
            snapshot,
            lines,
            orderNo,
            audit,
            request.DokterPerujukId,
            dokterPerujukName,
            request.LayananPerujukId,
            request.LayananPerujukName,
            request.ClinicalNote,
            urgency);

        LabOrderCreateFromEmrResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderCreateFromEmrResponse(
                order.EmrOrderId,
                (int)order.LabOrderStatus,
                order.OrderNo,
                order.OrderId);
        }

        return Task.FromResult(response);
    }
}
