using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IScheduleOpRepo :
    ISaveChange<ScheduleOpModel>,
    ILoadEntity<ScheduleOpModel, IScheduleOpKey>,
    IDeleteEntity<IScheduleOpKey>,
    IListData<ScheduleOpView, DateTime>,
    IListData<ScheduleOpView, IPasienKey>
{
    IEnumerable<ScheduleOpView> ListActiveSchedulesInRoom(string kamarId, DateTime date);
    IEnumerable<ScheduleOpView> ListActiveSchedulesForDoctor(string ppaId, DateTime date);
    IEnumerable<ScheduleOpView> ListActiveSchedulesForPpaInTeam(string ppaId, DateTime date);
}

public record ScheduleOpView(string ScheduleOpId,
    PasienReff Pasien,
    OrderOpReff OrderOp,
    UrgencyLevelEnum Urgency,
    DateTime TglOp, int Durasi,
    PpaReff TeamLead,
    KamarReff Kamar,
    OpCaseStateEnum Status,
    bool IsVoid,
    int TurnoverBuffer = 30,
    string ProfesiId = "",
    string ProfesiName = "");
