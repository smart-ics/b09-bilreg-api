using Ardalis.GuardClauses;
using Bilreg.Domain.LabContext.LabOrderFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.LabContext.LabOrderFeature.UseCases;

public record LabOrderGetQuery(string OrderId) : IRequest<LabOrderGetResponse>, ILabOrderKey;

public record LabOrderGetResponse(
    string OrderId,
    string EmrOrderId,
    string OrderNo,
    int OrderSource,
    int LabOrderStatus,
    int FinancialClearance,
    int OwareStatus,
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder,
    string ExecutionRegId,
    string DeferredReason,
    DateTime DeferredUntil,
    string BillingTindakanId,
    string BillingLastError,
    DateTime CollectedDate,
    string CollectedUserId,
    string CollectionNote,
    DateTime FinancialClearanceDate,
    string FinancialClearanceUserId,
    string FinancialClearanceReason,
    DateTime ReleasedDate,
    string ReleasedUserId,
    string ReleaseNote,
    string CancelledReason,
    DateTime CancelledDate,
    string CancelledUserId,
    string TerminationReason,
    DateTime TerminationDate,
    string TerminationUserId,
    bool IsVoided,
    IEnumerable<LabOrderItemResponse> Items,
    IEnumerable<LabOrderItemComponentResponse> ItemComponents);

public record LabOrderItemResponse(
    int ItemNo,
    string TestDefinitionId,
    string LabTestCode,
    string LabTestName,
    string TarifId,
    string TarifCode,
    string TarifName,
    int TubeType,
    string SpecimenType,
    int RequiredTubeCount);

public record LabOrderItemComponentResponse(
    int ItemNo,
    int ComponentNo,
    string ComponentId,
    string ComponentCode,
    string ComponentName,
    int ResultType,
    string Unit,
    string ReferenceRangeText,
    int SequenceNo,
    bool IsMandatory);

public class LabOrderGetHandler : IRequestHandler<LabOrderGetQuery, LabOrderGetResponse>
{
    private readonly ILabOrderRepo _repo;

    public LabOrderGetHandler(ILabOrderRepo repo)
    {
        _repo = repo;
    }

    public Task<LabOrderGetResponse> Handle(LabOrderGetQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.OrderId, nameof(request.OrderId));

        var order = _repo.LoadEntity(request).GetValueOrThrow($"LabOrder '{request.OrderId}' not found");

        var items = order.Items.Select(x => new LabOrderItemResponse(
            x.ItemNo,
            x.TestDefinitionId,
            x.LabTestCode,
            x.LabTestName,
            x.TarifId,
            x.TarifCode,
            x.TarifName,
            (int)x.TubeType,
            x.SpecimenType,
            x.RequiredTubeCount)).ToList();

        var components = order.ItemComponents.Select(x => new LabOrderItemComponentResponse(
            x.ItemNo,
            x.ComponentNo,
            x.ComponentId,
            x.ComponentCode,
            x.ComponentName,
            x.ResultType,
            x.Unit,
            x.ReferenceRangeText,
            x.SequenceNo,
            x.IsMandatory)).ToList();

        var response = new LabOrderGetResponse(
            OrderId: order.OrderId,
            EmrOrderId: order.EmrOrderId,
            OrderNo: order.OrderNo,
            OrderSource: (int)order.OrderSource,
            LabOrderStatus: (int)order.LabOrderStatus,
            FinancialClearance: (int)order.FinancialClearance,
            OwareStatus: (int)order.OwareStatus,
            RegId: order.Patient.RegId,
            PatientId: order.Patient.PatientId,
            PatientName: order.Patient.PatientName,
            BirthDate: order.Patient.BirthDate,
            Gender: order.Patient.Gender,
            AgeAtOrder: order.Patient.AgeAtOrder,
            ExecutionRegId: order.ExecutionRegId,
            DeferredReason: order.DeferredInfo.Reason,
            DeferredUntil: order.DeferredInfo.Until,
            BillingTindakanId: order.BillingTindakanId,
            BillingLastError: order.BillingLastError,
            CollectedDate: order.CollectionInfo.CollectedDate,
            CollectedUserId: order.CollectionInfo.CollectedUserId,
            CollectionNote: order.CollectionInfo.CollectionNote,
            FinancialClearanceDate: order.FinancialClearanceDate,
            FinancialClearanceUserId: order.FinancialClearanceUserId,
            FinancialClearanceReason: order.FinancialClearanceReason,
            ReleasedDate: order.ReleasedDate,
            ReleasedUserId: order.ReleasedUserId,
            ReleaseNote: order.ReleaseNote,
            CancelledReason: order.CancelledReason,
            CancelledDate: order.CancelledDate,
            CancelledUserId: order.CancelledUserId,
            TerminationReason: order.TerminationReason,
            TerminationDate: order.TerminationDate,
            TerminationUserId: order.TerminationUserId,
            IsVoided: order.AuditTrail.IsVoided,
            Items: items,
            ItemComponents: components);

        return Task.FromResult(response);
    }
}
