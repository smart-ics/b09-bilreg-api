using Bilreg.Application.IgdContext.IgdVisitFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;

namespace Bilreg.Infrastructure.IgdContext.IgdVisitFeature;

public record IgdVisitDto(
    string IgdVisitId, DateTime DaftarDateTime,

    string VisitorName, string VisitorGender, DateTime VisitorTglLahir, string VisitorKontak,

    string DokterId, string DokterName,

    bool HasTriage, string TriageMethod, string TriageLevel, string TriageColor, DateTime LastTriageAt, DateTime NextReTriageAt,

    string AdministrativeState,
    string RegId, string PasienId, string PasienName,

    string RedirectRajalId, DateTime RedirectDateTime, string RedirectReason,

    string BedIgdId,

    string DischargeUser, DateTime DischargeDateTime,

    string CrtUser, DateTime CrtDate,
    string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate)
{
    public static IgdVisitDto FromModel(IgdVisitModel model)
    {
        var visitorTglLahir = model.Visitor.TglLahir.ToDateTime(TimeOnly.MinValue);
        return new IgdVisitDto(
            IgdVisitId: model.IgdVisitId,
            DaftarDateTime: model.DaftarDateTime,

            VisitorName: model.Visitor.VisitorName,
            VisitorGender: model.Visitor.Gender,
            VisitorTglLahir: visitorTglLahir,
            VisitorKontak: model.Visitor.Kontak,

            DokterId: model.Dokter.PpaId,
            DokterName: model.Dokter.PpaName,

            HasTriage: model.HasTriage,
            TriageMethod: model.HasTriage ? model.TriageMethod.ToCode() : "",
            TriageLevel: model.HasTriage ? model.Triage.Level.ToCode() : "",
            TriageColor: model.HasTriage ? model.TriageColor.ToCode() : "",
            LastTriageAt: model.LastTriageAt,
            NextReTriageAt: model.NextReTriageAt,

            AdministrativeState: model.AdministrativeState.ToCode(),
            RegId: model.Reg.RegId,
            PasienId: model.Reg.PasienId,
            PasienName: model.Reg.PasienName,

            RedirectRajalId: model.Redirection.RedirectRajalId,
            RedirectDateTime: model.Redirection.RedirectDateTime,
            RedirectReason: model.Redirection.Reason,

            BedIgdId: model.BedId,

            DischargeUser: model.DischargeAudit.UserId,
            DischargeDateTime: model.DischargeAudit.Timestamp,

            CrtUser: model.AuditTrail.Created.UserId, CrtDate: model.AuditTrail.Created.Timestamp,
            UpdUser: model.AuditTrail.Modified.UserId, UpdDate: model.AuditTrail.Modified.Timestamp,
            VodUser: model.AuditTrail.Voided.UserId, VodDate: model.AuditTrail.Voided.Timestamp);
    }

    public IgdVisitModel ToModel(
        IEnumerable<IgdVisitTriageType> listTriage,
        IEnumerable<IgdVisitEventType> listEvent)
    {
        var visitor = new VisitorType(
            VisitorName: VisitorName,
            Gender: VisitorGender,
            TglLahir: DateOnly.FromDateTime(VisitorTglLahir),
            Kontak: VisitorKontak);

        var dokter = string.IsNullOrEmpty(DokterId)
            ? PpaType.Default.ToReff()
            : new PpaReff(DokterId, DokterName);

        var triageLatest = HasTriage
            ? new IgdVisitTriageType(
                NoTriage: 0,
                Method: TriageMethod.ToTriageMethodEnum(),
                Level: TriageLevel.ToTriageLevelEnum(),
                Color: TriageColor.ToTriageColorEnum(),
                AirwaysScore: 0,
                BreathingScore: 0,
                BloodCirculationScore: 0,
                GcsEyeScore: 0,
                GcsMotorScore: 0,
                GcsVoiceScore: 0,
                IsManualOverrideBlack: false,
                OverrideByUserId: "-",
                OverrideReason: "-",
                OverrideDateTime: new DateTime(3000, 1, 1),
                AssessmentDateTime: new DateTime(3000, 1, 1),
                AssessorUserId: "-",
                Notes: "-")
            : IgdVisitTriageType.Default;

        var reg = string.IsNullOrEmpty(RegId)
            ? RegModel.Default.ToReff()
            : new RegReff(RegId, PasienId, PasienName);

        var redirection = string.IsNullOrEmpty(RedirectRajalId)
            ? RedirectionType.Default
            : new RedirectionType(RedirectRajalId, RedirectDateTime, RedirectReason);

        var auditTrail = new AuditTrailType(
            new AuditInfoType(CrtUser, CrtDate),
            new AuditInfoType(UpdUser, UpdDate),
            new AuditInfoType(VodUser, VodDate));
        if (VodUser.Length > 0 && VodDate != new DateTime(3000, 1, 1))
            auditTrail.Batal(VodUser, VodDate);

        var dischargeAudit = new AuditInfoType(DischargeUser, DischargeDateTime);

        var bedId = string.IsNullOrEmpty(BedIgdId) ? "-" : BedIgdId;

        var visit = new IgdVisitModel(
            igdVisitId: IgdVisitId,
            daftarDateTime: DaftarDateTime,
            visitor: visitor,
            dokter: dokter,
            hasTriage: HasTriage,
            triage: triageLatest,
            administrativeState: AdministrativeState.ToAdministrativeStateEnum(),
            reg: reg,
            redirection: redirection,
            bedId: bedId,
            triageMethod: TriageMethod.ToTriageMethodEnum(),
            triageColor: TriageColor.ToTriageColorEnum(),
            lastTriageAt: LastTriageAt,
            nextReTriageAt: NextReTriageAt,
            auditTrail: auditTrail,
            dischargeAudit: dischargeAudit,
            listTriage: [],
            listEvent: []);

        visit.AttachTriages(listTriage);
        visit.AttachEvents(listEvent);
        return visit;
    }

    public IgdVisitView ToView()
        => new(
            IgdVisitId: IgdVisitId,
            DaftarDateTime: DaftarDateTime,
            VisitorName: VisitorName,
            Gender: VisitorGender,
            DokterId: DokterId,
            DokterName: DokterName,
            HasTriage: HasTriage,
            TriageLevel: TriageLevel,
            TriageColor: TriageColor,
            LastTriageAt: LastTriageAt,
            NextReTriageAt: NextReTriageAt,
            AdministrativeState: AdministrativeState,
            RegId: RegId,
            BedIgdId: BedIgdId,
            BedIgdName: "");
}
