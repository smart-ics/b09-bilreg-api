using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public interface IKarcisRepo :
    ISaveChange<KarcisType>,
    IDelete<IKarcisKey>,
    ILoadEntity<KarcisType, IKarcisKey>,
    IListData<KarcisView, IInstalasiDkKey>,
    IListData<KarcisLayananView, ILayananKey>

{
}

public record KarcisView(
    string KarcisId, string KarcisName,
    InstalasiDkType InstalasiDk,
    TarifReff DefaultTarif,
    decimal Nilai);

public record KarcisLayananView(
    string KarcisId, string KarcisName,
    LayananReff Layanan,
    TarifReff DefaultTarif,
    decimal Nilai);