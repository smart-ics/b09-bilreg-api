using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public interface IPolisRepo :
    ISaveChange<PolisModel>,
    ILoadEntity<PolisModel, IPolisKey>,
    IDeleteEntity<IPolisKey>,
    IListData<PolisView, IPasienKey>
{
}

public record PolisView(
    string PolisId, string NoPolis, string AtasName, 
    PasienReff Pasien, TipeJaminanReff TipeJaminan,
    DateOnly TglExpired) : IPolisKey;