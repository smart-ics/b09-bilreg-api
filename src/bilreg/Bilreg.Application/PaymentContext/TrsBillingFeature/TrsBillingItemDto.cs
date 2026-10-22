using Bilreg.Domain.AdmisiContext.RegFeature;

namespace Bilreg.Application.PaymentContext.TrsBillingFeature;

// ReSharper disable InconsistentNaming

public record TrsBillingItemDto(
    string BillingId, string ItemName, string CategoryCode, string CategoryName,
    string SourceRegId, string SourceRegType, string ServiceUnitId, string ServiceUnitName,
    string BedCode, DateTime TransactionDate, decimal Qty, decimal Tarif, decimal Diskon, decimal Total,
    string? PhysicalClass, string? PayerClass, string? BillingClass, decimal? DifferentialAmount
);