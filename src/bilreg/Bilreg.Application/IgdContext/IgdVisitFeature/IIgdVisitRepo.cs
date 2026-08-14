using Bilreg.Domain.IgdContext.IgdVisitFeature;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.IgdContext.IgdVisitFeature;

public interface IIgdVisitRepo :
    ISaveChange<IgdVisitModel>,
    ILoadEntity<IgdVisitModel, IIgdVisitKey>,
    IDeleteEntity<IIgdVisitKey>,
    IListData<IgdVisitView, Periode>
{
    IEnumerable<IgdVisitView> ListAktif();
    MayBe<IgdVisitView> GetByRegId(string regId);
}

public record IgdVisitView(
    string IgdVisitId,
    DateTime DaftarDateTime,
    string VisitorName,
    string Gender,
    string DokterId,
    string DokterName,
    bool HasTriage,
    string TriageLevel,
    string TriageColor,
    DateTime LastTriageAt,
    DateTime NextReTriageAt,
    string AdministrativeState,
    string RegId,
    string BedIgdId,
    string BedIgdName);
