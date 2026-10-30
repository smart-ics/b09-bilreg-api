namespace Bilreg.Domain.LabContext.LabExternalFeature;

public record LabExternalReferralInfoType(
    LabReferralTypeEnum ReferralType,
    string ReferralDoctor,
    string ReferralOrigin,
    string ClinicalNotes)
{
    public static LabExternalReferralInfoType Default => new(
        LabReferralTypeEnum.Aps,
        string.Empty,
        string.Empty,
        string.Empty);
}
