using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Nuna.Lib.DataAccessHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature;

public interface INilaiTarifRepo :
    ISaveChange<NilaiTarifType>,
    ILoadEntity<NilaiTarifType, ITarifKey>,
    IDeleteEntity<ITarifKey>
{
    IEnumerable<NilaiTarifView> ListData(ILayananKey layanan, 
        ITipeTarifKey tipeTarif, IKelasKey kelas);
}

public record NilaiTarifView(string TarifId, string TarifName, decimal Nilai);