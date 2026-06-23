using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Bilreg.Domain.IgdContext.TindakanIgdFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.IgdContext.TindakanIgdFeature;

public interface ITindakanIgdRepo :
    ISaveChange<TindakanIgdModel>,
    IDeleteEntity<ITindakanIgdKey>,
    ILoadEntity<TindakanIgdModel, ITindakanIgdKey>,
    IListData<TindakanIgdView, IIgdVisitKey>
{
    bool AnyForVisit(IIgdVisitKey visit);
}

public record TindakanIgdView(
    string TindakanIgdId,
    string IgdVisitId,
    string RegId,
    string ReffId,
    string Descriptions,
    int Qty,
    ActivityTindakanIgd Aktifitas,
    PpaReff Ppa,
    string CrtUserId,
    DateTime CreatedDateTime);
