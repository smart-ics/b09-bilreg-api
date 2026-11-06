using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;

namespace Bilreg.Application.BrgContext.PricingPolicyFeature;

public class BrgPricingService : IBrgPricingService
{
    private readonly IBrgHargaJualDal _dal;

    public BrgPricingService(IBrgHargaJualDal dal)
    {
        _dal = dal ?? throw new ArgumentNullException(nameof(dal));
    }

    public BrgPriceCalcResult CalculateItemPrice(string brgId, decimal qty, string tipeJaminanId, string layananId = "")
    {
        if (string.IsNullOrWhiteSpace(brgId))
            throw new ArgumentException("Barang ID cannot be empty.", nameof(brgId));
        if (qty < 0)
            throw new ArgumentOutOfRangeException(nameof(qty), "Qty must be greater than or equal to zero.");

        var cleanBrgId = brgId.Trim();
        var tipeBrgConfig = _dal.ResolveTipeBrg(tipeJaminanId);
        var pricingMap = _dal.GetBatchPricingData(new[] { cleanBrgId }, tipeBrgConfig.TipeBrg.TipeBrgId, layananId);
        pricingMap.TryGetValue(cleanBrgId, out var pricing);

        var (harga, diskonProsen, embalase, taxProsen) = ResolveItemParameters(pricing, tipeBrgConfig);

        var diskon = Math.Round((qty * harga) * (diskonProsen / 100m), 2);
        var subTotal = (qty * harga) - diskon + embalase;
        var tax = Math.Round(subTotal * (taxProsen / 100m), 0);
        var total = subTotal + tax;

        var nilai = new NilaiItemType(
            harga: harga,
            diskon: diskon,
            embalase: embalase,
            subTotal: subTotal,
            taxProsen: taxProsen,
            tax: tax,
            fee: 0m,
            bulat: 0m,
            nilaiKlaim: 0m,
            total: total);

        return new BrgPriceCalcResult(tipeBrgConfig.TipeBrg, nilai);
    }

    public IReadOnlyDictionary<string, decimal> GetUnitPrices(IEnumerable<string> brgIds, string tipeJaminanId, string layananId = "")
    {
        if (brgIds == null)
            return new Dictionary<string, decimal>();

        var idList = brgIds
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (idList.Count == 0)
            return new Dictionary<string, decimal>();

        var tipeBrgConfig = _dal.ResolveTipeBrg(tipeJaminanId);
        var pricingMap = _dal.GetBatchPricingData(idList, tipeBrgConfig.TipeBrg.TipeBrgId, layananId);

        var result = new Dictionary<string, decimal>(StringComparer.OrdinalIgnoreCase);
        foreach (var id in idList)
        {
            pricingMap.TryGetValue(id, out var pricing);
            var (harga, _, _, _) = ResolveItemParameters(pricing, tipeBrgConfig);
            result[id] = harga;
        }

        return result;
    }

    private static (decimal Harga, decimal DiskonProsen, decimal Embalase, decimal TaxProsen) ResolveItemParameters(
        BrgBatchPricingData? pricing,
        TipeBrgPricingConfig tipeBrgConfig)
    {
        decimal harga;
        decimal diskonProsen;
        decimal embalase;
        decimal taxProsen;

        if (pricing != null && pricing.HasBarangTipe)
        {
            diskonProsen = pricing.Diskon > 0 ? pricing.Diskon : tipeBrgConfig.DefaultDiskon;
            embalase = pricing.Biaya > 0 ? pricing.Biaya : tipeBrgConfig.DefaultBiayaPerBarang;
            taxProsen = pricing.Tax > 0 ? pricing.Tax : tipeBrgConfig.DefaultTax;

            if (pricing.HargaJual > 0)
            {
                harga = pricing.HargaJual;
            }
            else
            {
                var profit = pricing.Profit > 0 ? pricing.Profit : tipeBrgConfig.DefaultProfit;
                var hna = pricing.EffectiveHna;
                harga = Math.Round(hna * (1m + profit / 100m), 2);
            }
        }
        else
        {
            diskonProsen = tipeBrgConfig.DefaultDiskon;
            embalase = tipeBrgConfig.DefaultBiayaPerBarang;
            taxProsen = tipeBrgConfig.DefaultTax;

            var profit = tipeBrgConfig.DefaultProfit;
            var hna = pricing?.EffectiveHna ?? 0m;
            harga = Math.Round(hna * (1m + profit / 100m), 2);
        }

        return (harga, diskonProsen, embalase, taxProsen);
    }
}
