namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// Pure unit-cost calculation mirroring legacy MetodePersediaanHPP.
/// Inputs are unit prices (per DO satuan); application layer converts to the smallest unit when needed.
/// </summary>
public static class DeliveryOrderHppCalculator
{
    public static decimal Calculate(HppMethodEnum method, decimal hargaBeli, decimal diskon, decimal tax)
    {
        if (method is < HppMethodEnum.Hpp or > HppMethodEnum.HppDiskonTax)
            throw new ArgumentOutOfRangeException(nameof(method), method, "MetodePersediaanHPP tidak dikenal");
        if (hargaBeli < 0)
            throw new ArgumentOutOfRangeException(nameof(hargaBeli), "HargaBeli tidak boleh negatif");
        if (diskon < 0)
            throw new ArgumentOutOfRangeException(nameof(diskon), "Diskon tidak boleh negatif");
        if (tax < 0)
            throw new ArgumentOutOfRangeException(nameof(tax), "Tax tidak boleh negatif");

        return method switch
        {
            HppMethodEnum.Hpp => hargaBeli,
            HppMethodEnum.HppDiskon => hargaBeli - diskon,
            HppMethodEnum.HppDiskonTax => hargaBeli - diskon + tax,
            _ => throw new ArgumentOutOfRangeException(nameof(method), method, "MetodePersediaanHPP tidak dikenal")
        };
    }
}