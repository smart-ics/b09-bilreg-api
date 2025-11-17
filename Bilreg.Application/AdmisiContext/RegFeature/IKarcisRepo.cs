
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BillContext.TindakanFeature;
using Nuna.Lib.DataAccessHelper;

public interface IKarcisRepo :
    ISaveChange<KarcisType>,
    IDelete<IKarcisKey>,
    ILoadEntity<KarcisType, IKarcisKey>,
    IListData<KarcisView, IInstalasiDkKey>
{
}

public record KarcisView(
    string KarcisId, string KarcisName,
    InstalasiDkType InstalasiDk,
    TarifReff DefaultTarif,
    decimal Nilai);