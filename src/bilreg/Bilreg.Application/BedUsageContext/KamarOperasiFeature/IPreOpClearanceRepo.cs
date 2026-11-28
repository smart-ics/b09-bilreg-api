using Nuna.Lib.PatternHelper;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IPreOpClearanceRepo
{
    MayBe<PreOpClearanceModel> GetLatestByScheduleOpId(string scheduleOpId);
    int CountByScheduleOpId(string scheduleOpId);
    void SaveChanges(PreOpClearanceModel model);
}
