using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface INilaiTarifRepo :
    ISaveChange<NilaiTarifType>,
    ILoadEntity<NilaiTarifType, INilaiTarifCompositKey>,
    IDeleteEntity<INilaiTarifKey>,
    IListData<NilaiTarifView, ILayananKey, INilaiTarifVariant>
{
}

public record NilaiTarifView(string TarifId, string TarifName, 
    TipeTarifReff TipeTarif, KelasReff Kelas,
    decimal Nilai);