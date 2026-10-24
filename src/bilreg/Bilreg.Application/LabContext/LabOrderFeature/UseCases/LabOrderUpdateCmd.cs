using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderUpdateCmd(
    string OrderId,
    string UserId,
    List<LabOrderTarifItemInput> Items,
    LabOrderUrgencyEnum Urgency = LabOrderUrgencyEnum.Rutin,
    string ClinicalNote = "",
    bool AllowDuplicateConfirmation = false) : IRequest<LabOrderUpdateResponse>, ILabOrderKey;

public record LabOrderUpdateResponse(string OrderId, int ItemCount, int ComponentCount);

public class LabOrderUpdateHandler : IRequestHandler<LabOrderUpdateCmd, LabOrderUpdateResponse>
{
    private readonly ILabOrderRepo _labOrderRepo;
    private readonly ILabOrderDal? _labOrderDal;
    private readonly ILabTestResolutionService _resolutionService;
    private readonly ITglJamProvider _tglJamProvider;

    public LabOrderUpdateHandler(
        ILabOrderRepo labOrderRepo,
        ILabTestResolutionService resolutionService,
        ITglJamProvider tglJamProvider,
        ILabOrderDal? labOrderDal = null)
    {
        _labOrderRepo = labOrderRepo;
        _resolutionService = resolutionService;
        _tglJamProvider = tglJamProvider;
        _labOrderDal = labOrderDal;
    }

    public Task<LabOrderUpdateResponse> Handle(LabOrderUpdateCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NullOrEmpty(request.Items, nameof(request.Items));

        var order = _labOrderRepo.LoadEntity(request)
            .GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        if (order.LabOrderStatus != LabOrderStatusEnum.Ordered)
            throw new InvalidOperationException(
                $"LabOrder {order.OrderId} berstatus {order.LabOrderStatus}; update hanya diperbolehkan dari Ordered.");

        var resolvedLines = LabOrderCreateHelper.MapResolvedLines(
            _resolutionService.ResolveByTarifItems(request.Items));

        // Deteksi tes duplikat 24 jam (EX-05), mengecualikan order yang sedang diedit
        if (_labOrderDal is not null)
        {
            var activeTests = _labOrderDal.ListActiveTestsByPatientWithinHours(
                order.Patient.PatientId, 24, _tglJamProvider.Now)?.ToList() ?? [];
            var otherOrderActiveTests = activeTests.Where(a => a.OrderId != order.OrderId).ToList();
            if (otherOrderActiveTests.Count > 0)
            {
                var duplicates = otherOrderActiveTests
                    .Where(a => resolvedLines.Any(l =>
                        (!string.IsNullOrWhiteSpace(a.TestDefinitionId) && a.TestDefinitionId == l.Item.TestDefinitionId) ||
                        (!string.IsNullOrWhiteSpace(a.LabTestCode) && a.LabTestCode == l.Item.LabTestCode)))
                    .ToList();

                if (duplicates.Count > 0 && !request.AllowDuplicateConfirmation)
                {
                    var matchingLines = resolvedLines.Where(l => duplicates.Any(a =>
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
        }

        var occurredAt = _tglJamProvider.Now;
        order.UpdateItems(
            resolvedLines,
            request.Urgency,
            request.ClinicalNote,
            request.UserId,
            occurredAt);

        LabOrderUpdateResponse response;
        using (var trans = TransHelper.NewScope())
        {
            _labOrderRepo.SaveChanges(order);
            trans.Complete();
            response = new LabOrderUpdateResponse(
                order.OrderId,
                order.Items.Count,
                order.ItemComponents.Count);
        }

        return Task.FromResult(response);
    }
}
