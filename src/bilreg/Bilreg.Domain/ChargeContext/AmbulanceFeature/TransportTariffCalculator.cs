namespace Bilreg.Domain.ChargeContext.AmbulanceFeature;

public record CalculatedTariffItem(
    string DetilTarifId,
    decimal TarifMaster,
    bool IsTetap,
    decimal TarifFinal);

public record TransportTariffCalculationResult(
    decimal KonstantaEfektif,
    decimal Abonement,
    IReadOnlyList<CalculatedTariffItem> Items,
    decimal Total);

public static class TransportTariffCalculator
{
    public static TransportTariffCalculationResult Calculate(
        AmbulanceModel ambulance,
        TujuanTransportModel tujuan)
    {
        ArgumentNullException.ThrowIfNull(ambulance);
        ArgumentNullException.ThrowIfNull(tujuan);

        var overrideTujuan = ambulance.ListTujuan
            .FirstOrDefault(x => x.TujuanTransportId == tujuan.TujuanTransportId && x.Konstanta > 0);
        var konstantaEfektif = overrideTujuan?.Konstanta ?? tujuan.Konstanta;

        var totalTarifVariabel = ambulance.ListKomponen
            .Where(k => !k.IsTetap)
            .Sum(k => k.NilaiTarif);

        var items = new List<CalculatedTariffItem>(ambulance.ListKomponen.Count);
        foreach (var k in ambulance.ListKomponen)
        {
            decimal tarifFinal;
            if (k.IsTetap)
            {
                tarifFinal = k.NilaiTarif;
            }
            else
            {
                var varPart = k.NilaiTarif * konstantaEfektif;
                if (ambulance.Abonement > 0 && totalTarifVariabel > 0)
                {
                    var abonemenPart = Math.Round(
                        (k.NilaiTarif / totalTarifVariabel) * ambulance.Abonement,
                        0,
                        MidpointRounding.AwayFromZero);
                    tarifFinal = abonemenPart + varPart;
                }
                else
                {
                    tarifFinal = varPart;
                }
            }

            items.Add(new CalculatedTariffItem(k.KomponenId, k.NilaiTarif, k.IsTetap, tarifFinal));
        }

        var total = items.Sum(x => x.TarifFinal);
        return new TransportTariffCalculationResult(konstantaEfektif, ambulance.Abonement, items, total);
    }
}
