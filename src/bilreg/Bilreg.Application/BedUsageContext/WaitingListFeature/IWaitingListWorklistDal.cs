namespace Bilreg.Application.BedUsageContext.WaitingListFeature;

public interface IWaitingListWorklistDal
{
    IEnumerable<WaitingListWorklistView> ListActiveWorklist(string bangsalId);
}

public record WaitingListWorklistView(
    string WaitingListId,
    string RegId,
    int WaitingListStatus,
    int Priority,
    string PasienId,
    string PasienName,
    string Gender,
    string TglLahir,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    int ArrivalSourceType,
    string ReferringUnitId,
    string ReferringUnitName,
    string ReferringDoctorId,
    string ReferringDoctorName,
    int ClinicalUrgency,
    string SpecialRequirements,
    string PlannedBedId,
    string PlannedBedName,
    string PreviousWaitListId,
    string CrtUser,
    DateTime CrtDate);
