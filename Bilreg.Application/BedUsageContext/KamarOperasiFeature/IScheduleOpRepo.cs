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
}

public record ScheduleOpView(string ScheduleOpId,
    PasienReff Pasien,
    OrderOpReff OrderOp,
    UrgencyLevelEnum Urgency,
    DateTime TglOp, int Durasi,
    PpaReff TeamLead,
    KamarReff Kamar,
    bool IsVoid);