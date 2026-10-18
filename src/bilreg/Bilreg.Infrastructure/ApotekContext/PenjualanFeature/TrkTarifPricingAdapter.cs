using Bilreg.Application.ApotekContext.PenjualanFeature;
using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ApotekContext.PenjualanFeature;

/// <summary>
/// Adapter yang mengimplementasikan ITrkTarifPricingPort berkolaborasi dengan domain Tata Rekening
/// (ChargeContext.PricingBarangFeature) untuk evaluasi harga jual obat/BMHP, margin, plafon HET,
/// biaya jasa tuslah /r dan embalase /R, serta pembulatan nominal total tagihan.
/// </summary>
public class TrkTarifPricingAdapter : ITrkTarifPricingPort
{
    private readonly IItemPricingCalculator _pricingCalculator;

    public TrkTarifPricingAdapter(IItemPricingCalculator pricingCalculator)
    {
        _pricingCalculator = pricingCalculator;
    }

    public async Task<TrkTarifItemPricingResult> CalculateItemPriceAsync(
        TrkTarifItemPricingRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var tipeHargaId = ResolveTipeHargaId(request.TipeJaminanId);
        var qtyInt = (int)Math.Max(1, Math.Round(request.Qty, MidpointRounding.AwayFromZero));

        var calcResult = await _pricingCalculator.CalculateAsync(
            request.BrgId,
            tipeHargaId,
            request.TransactionDate,
            qtyInt,
            request.SatuanId,
            cancellationToken);

        return new TrkTarifItemPricingResult(
            calcResult.BarangId,
            calcResult.TipeHargaId,
            calcResult.HargaJualFinal,
            calcResult.DiskonDefaultPersen,
            calcResult.BiayaPerObat_r,
            calcResult.BiayaPerResep_R,
            calcResult.HnaApplied,
            calcResult.MarginPersenApplied,
            calcResult.IsFixedPriceApplied,
            calcResult.IsHetCapped,
            calcResult.HetNominal);
    }

    public TrkTarifItemPricingResult CalculateItemPrice(TrkTarifItemPricingRequest request)
    {
        return CalculateItemPriceAsync(request).GetAwaiter().GetResult();
    }

    public TrkTarifRoundingResult CalculateRounding(
        decimal totalBeforeRounding,
        int roundingIncrement = 100,
        int roundingMode = 1)
    {
        var mode = roundingMode == 2 ? RoundingModeEnum.KeAtas : RoundingModeEnum.Normal;
        var result = _pricingCalculator.CalculateRounding(totalBeforeRounding, roundingIncrement, mode);

        return new TrkTarifRoundingResult(
            result.TotalBeforeRounding,
            result.TotalAfterRounding,
            result.RoundingDifference);
    }

    public static string ResolveTipeHargaId(string tipeJaminanId)
    {
        if (string.IsNullOrWhiteSpace(tipeJaminanId) || tipeJaminanId == "00000")
            return "UM";
        if (tipeJaminanId.StartsWith("JKN", StringComparison.OrdinalIgnoreCase))
            return "JM";
        if (tipeJaminanId.Length == 2)
            return tipeJaminanId.ToUpperInvariant();
        return "UM";
    }
}
