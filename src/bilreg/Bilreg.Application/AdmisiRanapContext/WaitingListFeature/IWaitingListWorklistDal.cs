namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature;

public interface IWaitingListWorklistDal
{
    IEnumerable<WaitingListWorklistView> List(WaitingListWorklistFilter filter);
}

public record WaitingListWorklistFilter(string? BangsalId = null, int? WaitingListStatus = null);

public record WaitingListWorklistView(
    string WaitingListId,
    string RegId,
    int WaitingListStatus,
    int Priority,
    string PasienId,
    string PasienName,
    string Gender,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    DateTime CrtDate);
