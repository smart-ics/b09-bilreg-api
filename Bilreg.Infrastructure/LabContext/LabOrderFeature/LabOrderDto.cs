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
    string BillingTindakanId,
    string BillingLastError,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
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
            BillingTindakanId: model.BillingTindakanId,
            BillingLastError: model.BillingLastError,
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

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        if (VodUser.Length > 0 && VodDate != new DateTime(3000, 1, 1))
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
            BillingTindakanId,
            BillingLastError,
            auditTrail,
            items);
    }
}
