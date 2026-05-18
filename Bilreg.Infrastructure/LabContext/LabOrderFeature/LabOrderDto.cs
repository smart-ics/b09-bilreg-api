using Bilreg.Domain.LabContext.LabOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.LabContext.LabOrderFeature;

public record LabOrderDto(
    string OrderId,
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
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static LabOrderDto FromModel(LabOrderModel model)
        => new(
            OrderId: model.OrderId,
            OrderNo: model.OrderNo,
            OrderSource: (int)model.OrderSource,
            LabOrderStatus: (int)model.LabOrderStatus,
            FinancialClearance: (int)model.FinancialClearance,
            OwareStatus: (int)model.OwareStatus,
            RegId: model.Patient.RegId,
            PatientId: model.Patient.PatientId,
            PatientName: model.Patient.PatientName,
            BirthDate: model.Patient.BirthDate,
            Gender: model.Patient.Gender,
            AgeAtOrder: model.Patient.AgeAtOrder,
            ExecutionRegId: model.ExecutionRegId,
            DeferredReason: model.DeferredInfo.Reason,
            DeferredUntil: model.DeferredInfo.Until,
            BillingTindakanId: model.BillingTindakanId,
            BillingLastError: model.BillingLastError,
            CollectedDate: model.CollectionInfo.CollectedDate,
            CollectedUserId: model.CollectionInfo.CollectedUserId,
            CollectionNote: model.CollectionInfo.CollectionNote,
            FinancialClearanceDate: model.FinancialClearanceDate,
            FinancialClearanceUserId: model.FinancialClearanceUserId,
            FinancialClearanceReason: model.FinancialClearanceReason,
            ReleasedDate: model.ReleasedDate,
            ReleasedUserId: model.ReleasedUserId,
            ReleaseNote: model.ReleaseNote,
            CancelledReason: model.CancelledReason,
            CancelledDate: model.CancelledDate,
            CancelledUserId: model.CancelledUserId,
            TerminationReason: model.TerminationReason,
            TerminationDate: model.TerminationDate,
            TerminationUserId: model.TerminationUserId,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabOrderModel ToModel(IEnumerable<LabOrderItemModel> items)
    {
        var patient = new PatientSnapshotType(
            RegId,
            PatientId,
            PatientName,
            BirthDate,
            Gender,
            AgeAtOrder);

        var deferredInfo = new DeferredInfoType(DeferredReason, DeferredUntil);
        var collectionInfo = new CollectionInfoType(CollectedDate, CollectedUserId, CollectionNote);

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        if (VodUser.Length > 0 && VodDate != EmptyDate)
            auditTrail.Batal(VodUser, VodDate);

        return LabOrderModel.Load(
            OrderId,
            OrderNo,
            (LabOrderSourceEnum)OrderSource,
            (LabOrderStatusEnum)LabOrderStatus,
            (FinancialClearanceEnum)FinancialClearance,
            (OwareStatusEnum)OwareStatus,
            patient,
            ExecutionRegId,
            deferredInfo,
            BillingTindakanId,
            BillingLastError,
            collectionInfo,
            FinancialClearanceDate,
            FinancialClearanceUserId,
            FinancialClearanceReason,
            ReleasedDate,
            ReleasedUserId,
            ReleaseNote,
            CancelledReason,
            CancelledDate,
            CancelledUserId,
            TerminationReason,
            TerminationDate,
            TerminationUserId,
            auditTrail,
            items);
    }
}
