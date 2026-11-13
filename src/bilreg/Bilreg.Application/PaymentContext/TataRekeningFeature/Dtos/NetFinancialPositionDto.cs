namespace Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;

public record NetFinancialPositionDto(
    decimal GrossBilling,
    decimal EstimatedPayerCoverage,
    decimal ClassDifferential,
    decimal TotalPatientDeposit,
    decimal TotalCashierPaid,
    decimal NetPayableBalance
);