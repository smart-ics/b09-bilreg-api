using Bilreg.Application.ChargeContext.RoomChargeFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.RoomChargeFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;

namespace Bilreg.Infrastructure.ChargeContext.RoomChargeFeature;

public class RoomChargeTarifService : IRoomChargeTarifService
{
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;

    public RoomChargeTarifService(ITarifRepo tarifRepo, INilaiTarifRepo nilaiTarifRepo)
    {
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
    }

    public RoomChargeTarifResult GetTarif(string billingClassId, string tipeJaminanId)
    {
        // Try looking up NilaiTarif
        var compositeKey = NilaiTarifType.KeyComposite(
            TarifType.Key(billingClassId),
            TipeTarifType.Key(tipeJaminanId),
            KelasType.Key(billingClassId));

        var nilaiTarifOpt = _nilaiTarifRepo.LoadEntity(compositeKey);
        if (nilaiTarifOpt.HasValue && nilaiTarifOpt.Value.Nilai > 0)
        {
            var nt = nilaiTarifOpt.Value;
            var komponen = nt.ListKomponen.Select(k => RoomChargeKomponenModel.Create(
                k.Komponen.KomponenId,
                k.Komponen.KomponenName,
                "Akomodasi",
                k.Nilai,
                0m,
                "1.1.01"
            )).ToList();

            if (komponen.Count > 0)
            {
                return new RoomChargeTarifResult(nt.Nilai, komponen);
            }
        }

        // Standard 3-pillar breakdown fallback (Kamar, Keperawatan, Gizi)
        var defaultTariff = 350000m;
        var defaultKomponen = new List<RoomChargeKomponenModel>
        {
            RoomChargeKomponenModel.Create("K01", "Sewa Kamar", "Akomodasi", defaultTariff * 0.60m, 0m, "1.1.01"),
            RoomChargeKomponenModel.Create("K02", "Askep Bangsal", "Keperawatan", defaultTariff * 0.25m, 0m, "1.1.02"),
            RoomChargeKomponenModel.Create("K03", "Konsumsi Gizi", "Gizi", defaultTariff * 0.15m, 0m, "1.1.03")
        };

        return new RoomChargeTarifResult(defaultTariff, defaultKomponen);
    }
}
