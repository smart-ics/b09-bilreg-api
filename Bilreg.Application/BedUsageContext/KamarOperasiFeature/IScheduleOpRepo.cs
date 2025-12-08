using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IScheduleOpRepo :
    ISaveChange<ScheduleOpModel>,
    ILoadEntity<ScheduleOpModel, IScheduleOpKey>,
    IDeleteEntity<IScheduleOpKey>,
    IListData<ScheduleOpView, DateTime>
{
}

public record ScheduleOpView(string ScheduleOpId, 
    PasienReff Pasien,
    string NamaOperasi, UrgencyLevelEnum Urgency,
    DateTime TglOp, int Durasi, 
    PpaReff TeamLead, 
    KamarReff Kamar);