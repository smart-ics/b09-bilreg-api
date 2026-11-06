using Ardalis.GuardClauses;
using Bilreg.Application.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderCreateExternalPatientCmd(
    string UserId,
    string? EmrOrderId,
    string? RegId,
    string? PatientId,
    string PatientName,
    string? BirthDateYmd,
    string? Gender,
    List<LabOrderTarifItemInput> Items,
    string? RegExternalId = null,
    string? ExternalMrNo = null,
    string DokterPerujukId = "",
    string DokterPerujukName = "",
    string LayananPerujukId = "",
    string LayananPerujukName = "",
    string ClinicalNote = "",
    int Urgency = 0,
    bool AllowDuplicateConfirmation = false)
    : IRequest<LabOrderCreateResponse>
{
    public string EffectiveRegId => !string.IsNullOrWhiteSpace(RegExternalId) ? RegExternalId : (RegId ?? string.Empty);
    public string EffectivePatientId => !string.IsNullOrWhiteSpace(ExternalMrNo) ? ExternalMrNo : (PatientId ?? string.Empty);
}

public record LabOrderCreateResponse(
    string OrderId,
    string OrderNo,
    List<CreatedOrderInfo>? CreatedOrders = null);

public class LabOrderCreateExternalPatientHandler
    : IRequestHandler<LabOrderCreateExternalPatientCmd, LabOrderCreateResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ISequencer _sequencer;
    private readonly ILabTestResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly ILabExternalRepo _labExternalRepo;
    private readonly ILabOrderDal _labOrderDal;

    public LabOrderCreateExternalPatientHandler(
        ILabOrderRepo labOrderRepo,
        ISequencer sequencer,
        ILabTestResolutionService resolutionService,
        ITglJamProvider tglJamProvider,
        ILabExternalRepo labExternalRepo,
        ILabOrderDal labOrderDal)
    {
        _labOrderRepo = labOrderRepo;
        _sequencer = sequencer;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
        _labExternalRepo = labExternalRepo;
        _labOrderDal = labOrderDal;
    }

    public Task<LabOrderCreateResponse> Handle(
        LabOrderCreateExternalPatientCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrWhiteSpace(request.PatientName, nameof(request.PatientName));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var regId = request.EffectiveRegId;
        Guard.Against.NullOrWhiteSpace(regId, nameof(request.RegExternalId));

        // 0. Idempotensi jika EmrOrderId diisi
        if (!string.IsNullOrWhiteSpace(request.EmrOrderId))
        {
            var existingOrder = _labOrderRepo.LoadByEmrOrderId(request.EmrOrderId)
                .Match(
                    onSome: x => x,
                    onNone: () => null as LabOrderModel
                );

            if (existingOrder is not null)
            {
                var createdOrders = new List<CreatedOrderInfo>
                {
                    new(existingOrder.OrderId, existingOrder.OrderNo, (int)existingOrder.ServiceType, existingOrder.EmrOrderId)
                };
                return Task.FromResult(new LabOrderCreateResponse(
                    existingOrder.OrderId,
                    existingOrder.OrderNo,
                    createdOrders));
            }

            var existingSplitOrders = _labOrderRepo.ListByEmrOrderIdPrefix(request.EmrOrderId)?.ToList() ?? [];
            if (existingSplitOrders.Count > 0)
            {
                var primary = existingSplitOrders.First();
                var createdOrders = existingSplitOrders
                    .Select(o => new CreatedOrderInfo(o.OrderId, o.OrderNo, (int)o.ServiceType, o.EmrOrderId))
                    .ToList();
                return Task.FromResult(new LabOrderCreateResponse(
                    primary.OrderId,
                    primary.OrderNo,
                    createdOrders));
            }
        }

        // 1. Validasi keaktifan RegExternalId ke LAB-EXTERNAL
        var externalReg = _labExternalRepo.LoadEntity(LabExternalModel.Key(regId))
            .Match(
                onSome: x => x,
                onNone: () => null as LabExternalModel
            );

        if (externalReg is null)
            throw new InvalidOperationException($"Registrasi eksternal '{regId}' tidak ditemukan.");

        if (externalReg.Status != LabExternalStatusEnum.Active)
            throw new InvalidOperationException($"Registrasi eksternal '{regId}' tidak aktif (status {externalReg.Status}).");

        var patientId = request.EffectivePatientId;
        if (string.IsNullOrWhiteSpace(patientId))
            patientId = externalReg.ExternalMrNo;

        // 2. Auto-populasi data perujuk dan catatan klinis dari ReferralInfo (atau default APS bila kosong)
        var dokterPerujukName = !string.IsNullOrWhiteSpace(request.DokterPerujukName)
            ? request.DokterPerujukName
            : (!string.IsNullOrWhiteSpace(externalReg.ReferralInfo?.ReferralDoctor)
                ? externalReg.ReferralInfo.ReferralDoctor
                : "APS");

        var layananPerujukName = !string.IsNullOrWhiteSpace(request.LayananPerujukName)
            ? request.LayananPerujukName
            : (!string.IsNullOrWhiteSpace(externalReg.ReferralInfo?.ReferralOrigin)
                ? externalReg.ReferralInfo.ReferralOrigin
                : "APS");

        var clinicalNote = !string.IsNullOrWhiteSpace(request.ClinicalNote)
            ? request.ClinicalNote
            : (!string.IsNullOrWhiteSpace(externalReg.ReferralInfo?.ClinicalNotes)
                ? externalReg.ReferralInfo.ClinicalNotes
                : "APS");

        var audit = new AuditInfoType(request.UserId, _tglJamProvider.Now);
        var snapshot = LabOrderCreateHelper.BuildSnapshot(
            regId,
            patientId,
            request.PatientName,
            request.BirthDateYmd ?? "",
            request.Gender ?? "",
            audit.Timestamp);
        var lines = LabOrderCreateHelper.MapResolvedLines(_resolutionService.ResolveByTarifItems(request.Items));

        // 3. Deteksi duplikasi 24 jam menggunakan _labOrderDal.ListActiveTestsByPatientWithinHours
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

        var groupedLines = lines
            .GroupBy(x => x.Item.ServiceType)
            .OrderBy(g => g.Key)
            .ToList();

        var urgency = (LabOrderUrgencyEnum)request.Urgency;
        LabOrderCreateResponse response;

        using (var trans = TransHelper.NewScope())
        {
            if (groupedLines.Count == 1)
            {
                var singleGroup = groupedLines[0];
                var orderNo = LabOrderCreateHelper.NextOrderNo(_sequencer);
                var order = LabOrderModel.CreateExternal(
                    request.EmrOrderId,
                    snapshot,
                    singleGroup.ToList(),
                    orderNo,
                    audit,
                    request.DokterPerujukId,
                    dokterPerujukName,
                    request.LayananPerujukId,
                    layananPerujukName,
                    clinicalNote,
                    urgency,
                    singleGroup.Key);

                _labOrderRepo.SaveChanges(order);
                trans.Complete();

                var createdOrders = new List<CreatedOrderInfo>
                {
                    new(order.OrderId, order.OrderNo, (int)order.ServiceType, order.EmrOrderId)
                };
                response = new LabOrderCreateResponse(order.OrderId, order.OrderNo, createdOrders);
            }
            else
            {
                var createdOrders = new List<LabOrderModel>();
                foreach (var group in groupedLines)
                {
                    var suffix = LabOrderCreateHelper.GetServiceTypeSuffix(group.Key);
                    var subEmrOrderId = string.IsNullOrWhiteSpace(request.EmrOrderId)
                        ? string.Empty
                        : $"{request.EmrOrderId}-{suffix}";
                    var subOrderNo = LabOrderCreateHelper.NextOrderNo(_sequencer);
                    var subOrder = LabOrderModel.CreateExternal(
                        subEmrOrderId,
                        snapshot,
                        group.ToList(),
                        subOrderNo,
                        audit,
                        request.DokterPerujukId,
                        dokterPerujukName,
                        request.LayananPerujukId,
                        layananPerujukName,
                        clinicalNote,
                        urgency,
                        group.Key);

                    _labOrderRepo.SaveChanges(subOrder);
                    createdOrders.Add(subOrder);
                }

                trans.Complete();

                var primary = createdOrders.First();
                var createdOrderInfos = createdOrders
                    .Select(o => new CreatedOrderInfo(o.OrderId, o.OrderNo, (int)o.ServiceType, o.EmrOrderId))
                    .ToList();
                response = new LabOrderCreateResponse(primary.OrderId, primary.OrderNo, createdOrderInfos);
            }
        }

        return Task.FromResult(response);
    }
}
