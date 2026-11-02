using Bilreg.Domain.LabContext.LabExternalFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.LabContext.LabExternalFeature;

public record LabExternalDto(
    string RegExternalId,
    string ExternalMrNo,
    string PatientName,
    string Nik,
    DateTime BirthDate,
    string Gender,
    string Phone,
    string Address,
    int ReferralType,
    string ReferralDoctor,
    string ReferralOrigin,
    string ClinicalNotes,
    string BillingAccountId,
    int Status,
    string CancelReason,
    string CancelledUserId,
    DateTime CancelledDate,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate)
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static LabExternalDto FromModel(LabExternalModel model)
        => new(
            RegExternalId: model.RegExternalId,
            ExternalMrNo: model.ExternalMrNo,
            PatientName: model.Demographics.PatientName,
            Nik: model.Demographics.Nik,
            BirthDate: model.Demographics.BirthDate,
            Gender: model.Demographics.Gender,
            Phone: model.Demographics.Phone,
            Address: model.Demographics.Address,
            ReferralType: (int)model.ReferralInfo.ReferralType,
            ReferralDoctor: model.ReferralInfo.ReferralDoctor,
            ReferralOrigin: model.ReferralInfo.ReferralOrigin,
            ClinicalNotes: model.ReferralInfo.ClinicalNotes,
            BillingAccountId: model.BillingAccountId,
            Status: (int)model.Status,
            CancelReason: model.CancelInfo.CancelReason,
            CancelledUserId: model.CancelInfo.CancelledUserId,
            CancelledDate: model.CancelInfo.CancelledDate,
            CrtUser: model.AuditTrail.Created.UserId,
            CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId,
            UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId,
            VodDate: model.AuditTrail.Voided.Timestamp);

    public LabExternalModel ToModel()
    {
        var demographics = new LabExternalDemographicsType(
            PatientName,
            Nik,
            BirthDate,
            Gender,
            Phone,
            Address);

        var referralInfo = new LabExternalReferralInfoType(
            (LabReferralTypeEnum)ReferralType,
            ReferralDoctor,
            ReferralOrigin,
            ClinicalNotes);

        var cancelInfo = new LabExternalCancelInfoType(
            CancelReason,
            CancelledUserId,
            CancelledDate);

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));

        if (VodUser.Length > 0 && VodDate != EmptyDate)
            auditTrail.Batal(VodUser, VodDate);

        return LabExternalModel.Load(
            RegExternalId,
            ExternalMrNo,
            demographics,
            referralInfo,
            BillingAccountId,
            (LabExternalStatusEnum)Status,
            cancelInfo,
            auditTrail);
    }
}
