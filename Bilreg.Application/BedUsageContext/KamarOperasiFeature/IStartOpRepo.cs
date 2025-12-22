using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IStartOpRepo :
    ISaveChange<StartOpModel>,
    ILoadEntity<StartOpModel, IStartOpKey>,
    IDeleteEntity<IStartOpKey>,
    IListData<StartOpView, DateTime>
{
}

public record StartOpView(
    string StartOpId, string ScheduleOpId, string OrderOpId, DateTime StartOpTime,
    string RegId, string PasienId, string PasienName);
