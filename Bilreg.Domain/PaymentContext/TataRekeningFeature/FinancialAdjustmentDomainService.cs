using Bilreg.Domain.PaymentContext.TrsBillFeature;

namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed class FinancialAdjustmentDomainService : IFinancialAdjustmentDomainService
{
    private readonly IProjectionRegenerationDomainService _projectionService;

    public FinancialAdjustmentDomainService(IProjectionRegenerationDomainService projectionService)
    {
        _projectionService = projectionService ?? throw new ArgumentNullException(nameof(projectionService));
    }

    public FinancialAdjustmentResult Apply(
        TataRekeningModel tataRekening,
        FinancialAdjustmentRequest request,
        DateTime appliedAt)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        ArgumentNullException.ThrowIfNull(request);

        ValidatePreconditions(tataRekening);

        if (request.RequiresChargeSourceChange)
        {
            return new FinancialAdjustmentResult(
                RequiresReopen: true,
                Type: request.Type,
                TrsBillingId: request.TrsBillingId);
        }

        var hadAllocation = tataRekening.IsFinancialResponsibilityAllocated;

        TrsBillFinancialAdjustmentRecord? applied = request.Type switch
        {
            FinancialAdjustmentTypeEnum.ManualCharge => ApplyManualCharge(tataRekening, request),
            FinancialAdjustmentTypeEnum.BillingCorrection or FinancialAdjustmentTypeEnum.MergeBillingCorrection
                => ApplyBillAdjustment(tataRekening, request, appliedAt),
            FinancialAdjustmentTypeEnum.Waive => ApplyBillAdjustment(tataRekening, request, appliedAt),
            FinancialAdjustmentTypeEnum.Subsidy => ApplyBillAdjustment(tataRekening, request, appliedAt),
            _ => throw new InvalidOperationException($"Financial Adjustment type '{request.Type}' tidak dikenali.")
        };

        if (hadAllocation)
            _projectionService.ClearProjection(tataRekening);

        tataRekening.ResetFinancialVerification();

        return new FinancialAdjustmentResult(
            RequiresReopen: false,
            Type: request.Type,
            TrsBillingId: request.TrsBillingId,
            AppliedAdjustment: applied);
    }

    private static void ValidatePreconditions(TataRekeningModel tataRekening)
    {
        if (tataRekening.Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException(
                "Financial Adjustment hanya dapat dilakukan saat Billing berstatus CLOSED.");

        if (tataRekening.Status is TataRekeningStatusEnum.Finalized or TataRekeningStatusEnum.Lunas)
            throw new InvalidOperationException(
                "Financial Adjustment tidak dapat dilakukan pada billing FINALIZED atau LUNAS.");

        if (tataRekening.FinancialVerificationStatus != FinancialVerificationStatusEnum.RequiresAdjustment)
            throw new InvalidOperationException(
                "Financial Adjustment hanya dapat dilakukan setelah Financial Verification menandakan RequiresAdjustment.");
    }

    private static TrsBillFinancialAdjustmentRecord? ApplyManualCharge(
        TataRekeningModel tataRekening,
        FinancialAdjustmentRequest request)
    {
        if (request.ManualChargeBill is null)
            throw new InvalidOperationException(
                "Manual Charge memerlukan bill yang akan ditambahkan ke Billing Set.");

        tataRekening.AcceptManualChargeBill(request.ManualChargeBill);
        return null;
    }

    private static TrsBillFinancialAdjustmentRecord ApplyBillAdjustment(
        TataRekeningModel tataRekening,
        FinancialAdjustmentRequest request,
        DateTime appliedAt)
    {
        if (string.IsNullOrWhiteSpace(request.TrsBillingId))
            throw new InvalidOperationException(
                $"Financial Adjustment '{request.Type}' memerlukan TrsBillingId.");

        var bill = tataRekening.GetBillById(request.TrsBillingId);

        return bill.ApplyFinancialAdjustment(
            request.Type,
            request.Amount,
            request.Reason,
            appliedAt,
            request.SubsidyPayer);
    }
}
