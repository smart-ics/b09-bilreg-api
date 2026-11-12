using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler <see cref="CalculateItemPriceQuery"/>: mendelegasikan kalkulasi ke domain service
/// <see cref="IItemPricingCalculator"/> dan mengembalikan DTO kalkulasi harga terurai lengkap.
/// <see cref="HnaNotFoundException"/> dibiarkan merambat (konsisten dengan konvensi not-found rumah).
/// </summary>
public class CalculateItemPriceHandler : IRequestHandler<CalculateItemPriceQuery, ItemPriceCalculationResultDto>
{
    private readonly IItemPricingCalculator _itemPricingCalculator;

    public CalculateItemPriceHandler(IItemPricingCalculator itemPricingCalculator)
    {
        _itemPricingCalculator = itemPricingCalculator;
    }

    public Task<ItemPriceCalculationResultDto> Handle(
        CalculateItemPriceQuery request,
        CancellationToken cancellationToken) =>
        _itemPricingCalculator.CalculateAsync(
            request.BarangId,
            request.TipeHargaId,
            request.TransactionDate,
            request.Qty,
            request.SatuanId ?? string.Empty,
            cancellationToken);
}
