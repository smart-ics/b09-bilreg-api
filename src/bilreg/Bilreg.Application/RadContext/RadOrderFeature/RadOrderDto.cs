using Bilreg.Domain.RadContext.RadOrderFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Application.RadContext.RadOrderFeature;

public record RadOrderDto(
    string OrderId,
    string EmrOrderId,
    string OrderNo,
    int OrderSource,
    string Modality,
    int RadOrderStatus,
    int Urgency,
    string RegId,
    string PatientId,
    string PatientName,
    DateTime BirthDate,
    string Gender,
    int AgeAtOrder,
    string DokterPengorderId,
    string DokterPengorderName,
    string LayananPengorderId,
    string LayananPengorderName,
    string ClinicalNote,
    string CancelledReason,
    DateTime CancelledDate,
    string CancelledUserId,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate,
    string BillingTindakanId = "",
    string BillingLastError = "") : IRadOrderKey
{
    public RadOrderDto() : this(
        string.Empty, string.Empty, string.Empty, 0, string.Empty, 1, 0,
        string.Empty, string.Empty, string.Empty, new DateTime(3000, 1, 1), string.Empty, 0,
        string.Empty, string.Empty, string.Empty, string.Empty, string.Empty,
        string.Empty, new DateTime(3000, 1, 1), string.Empty,
        string.Empty, new DateTime(3000, 1, 1), string.Empty, new DateTime(3000, 1, 1),
        string.Empty, new DateTime(3000, 1, 1),
        string.Empty, string.Empty)
    {
    }

    public static RadOrderDto FromModel(RadOrderModel model) => new(
        model.OrderId,
        model.EmrOrderId,
        model.OrderNo,
        (int)model.OrderSource,
        model.Modality.ToString(),
        (int)model.RadOrderStatus,
        (int)model.Urgency,
        model.Patient.RegId,
        model.Patient.PatientId,
        model.Patient.PatientName,
        model.Patient.BirthDate,
        model.Patient.Gender,
        model.Patient.AgeAtOrder,
        model.DokterPengorderId,
        model.DokterPengorderName,
        model.LayananPengorderId,
        model.LayananPengorderName,
        model.ClinicalNote,
        model.CancelledReason,
        model.CancelledDate,
        model.CancelledUserId,
        model.AuditTrail.Created.UserId,
        model.AuditTrail.Created.Timestamp,
        model.AuditTrail.Modified.UserId,
        model.AuditTrail.Modified.Timestamp,
        model.AuditTrail.Voided.UserId,
        model.AuditTrail.Voided.Timestamp,
        model.BillingTindakanId,
        model.BillingLastError);

    public RadOrderModel ToModel(IEnumerable<RadOrderItemModel>? items = null)
    {
        var patient = new PatientSnapshotType(
            RegId,
            PatientId,
            PatientName,
            BirthDate,
            Gender,
            AgeAtOrder);

        var orderSource = Enum.IsDefined(typeof(RadOrderSourceEnum), OrderSource)
            ? (RadOrderSourceEnum)OrderSource
            : RadOrderSourceEnum.Internal;

        var modality = Enum.TryParse<RadModalityEnum>(Modality, ignoreCase: true, out var m)
            ? m
            : RadModalityEnum.CR;

        var status = Enum.IsDefined(typeof(RadOrderStatusEnum), RadOrderStatus)
            ? (RadOrderStatusEnum)RadOrderStatus
            : RadOrderStatusEnum.Requested;

        var urgency = Enum.IsDefined(typeof(RadUrgencyEnum), Urgency)
            ? (RadUrgencyEnum)Urgency
            : RadUrgencyEnum.Rutin;

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        if (!string.IsNullOrWhiteSpace(VodUser) && VodDate.Date != new DateTime(3000, 1, 1))
        {
            auditTrail.Batal(VodUser, VodDate);
        }

        return new RadOrderModel(
            OrderId,
            EmrOrderId,
            OrderNo,
            orderSource,
            modality,
            status,
            urgency,
            patient,
            RegId,
            DokterPengorderId,
            DokterPengorderName,
            LayananPengorderId,
            LayananPengorderName,
            ClinicalNote,
            CancelledReason,
            CancelledDate,
            CancelledUserId,
            auditTrail,
            items,
            BillingTindakanId,
            BillingLastError);
    }
}
