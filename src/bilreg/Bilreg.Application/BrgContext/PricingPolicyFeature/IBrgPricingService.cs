using Bilreg.Domain.BrgContext.PricingPolicyFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;

namespace Bilreg.Application.BrgContext.PricingPolicyFeature;

public record TipeBrgPricingConfig(
    TipeBrgReff TipeBrg,
    decimal DefaultProfit,
    decimal DefaultBiayaPerBarang,
    decimal DefaultTax,
    decimal DefaultDiskon
);

public record BrgBatchPricingData(
    string BrgId,
    bool HasBarangTipe,
    decimal HargaJual,
    decimal Profit,
    decimal Biaya,
    decimal Tax,
    decimal Diskon,
    decimal HnaGen,
    decimal HnaAkhir,
    decimal HppStok
)
{
    public decimal EffectiveHna => HnaGen > 0 ? HnaGen : (HnaAkhir > 0 ? HnaAkhir : HppStok);
}

public interface IBrgHargaJualDal
{
    TipeBrgPricingConfig ResolveTipeBrg(string tipeJaminanId);
    IReadOnlyDictionary<string, BrgBatchPricingData> GetBatchPricingData(IEnumerable<string> brgIds, string tipeBrgId, string layananId = "");
}

public record BrgPriceCalcResult(
    TipeBrgReff TipeBrg,
    NilaiItemType Nilai
);

public interface IBrgPricingService
{
    BrgPriceCalcResult CalculateItemPrice(string brgId, decimal qty, string tipeJaminanId, string layananId = "");
    IReadOnlyDictionary<string, decimal> GetUnitPrices(IEnumerable<string> brgIds, string tipeJaminanId, string layananId = "");
}
