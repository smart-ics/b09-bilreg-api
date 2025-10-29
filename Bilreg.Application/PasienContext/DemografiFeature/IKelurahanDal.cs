using Bilreg.Domain.PasienContext.DemografiFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.PasienContext.DemografiFeature;


public interface IKelurahanRepo :
    ISaveChange<KelurahanType>,
    ILoadEntity<KelurahanType, IKelurahanKey>,
    IDeleteEntity<IKelurahanKey>,
    IListData<KelurahanView>,
    IListData<KelurahanView, string>;

public record KelurahanView(
    string KelurahanId, string KelurahanName, string KecamatanName,
    string KabupatanName, string PropinsiName);

