namespace Bilreg.Domain.PurchaseContext.DeliveryFeature;

/// <summary>
/// MetodePersediaanHPP — legacy unit-cost formula for DO receipt lines.
/// </summary>
public enum HppMethodEnum
{
    Hpp,          // HPP = HargaBeli
    HppDiskon,    // HPP = HargaBeli - Diskon
    HppDiskonTax  // HPP = HargaBeli - Diskon + Tax
}