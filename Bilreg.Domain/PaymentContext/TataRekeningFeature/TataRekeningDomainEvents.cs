namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public record BillClosed(string RegId, DateTime ClosedAt);

public record FinancialVerified(string RegId, string PetugasVerif, DateTime VerifiedAt);

public record FinancialResponsibilityAllocated(string RegId, DateTime AllocatedAt);

public record FinancialResponsibilityFinalized(string RegId, string PetugasVerif, DateTime FinalizationDate);

public record FinalizationCancelled(string RegId, DateTime CancelledAt);

public record BillingReopened(string RegId, DateTime ReopenedAt);

public record SettlementInitiated(string RegId, string PetugasVerif, DateTime InitiatedAt);
