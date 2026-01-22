using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.BedUsageContext.KamarOperasiFeature;

public interface IStartOpRepo :
    ISaveChange<StartOpModel>,
    ILoadEntity<StartOpModel, IStartOpKey>,
    IDeleteEntity<IStartOpKey>,
    IListData<StartOpView, DateTime>,
    IListData<StartOpView, IPasienKey>
{
}

public record StartOpView(
    string StartOpId, string ScheduleOpId, string OrderOpId, DateTime StartOpTime,
    string RegId, string PasienId, string PasienName, bool IsVoid);
