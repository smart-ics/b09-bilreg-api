using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature;

internal static class TataRekeningApplicationMapper
{
    public static TataRekeningSummaryDto ToSummaryDto(TataRekeningModel model) =>
        new(
            model.RegId,
            model.Status,
            model.FinancialVerificationStatus,
            model.IsFinancialResponsibilityAllocated,
            model.SettlementInitiated);

    public static TrsBillSummaryDto ToBillSummaryDto(TrsBillType bill) =>
        new(
            bill.TrsBillingId,
            bill.Reg.RegId,
            bill.ModulGroup,
            bill.Nilai.Total,
            bill.FinancialTotal);

    public static PaymentProjectionDto ToProjectionDto(TataRekeningPaymentType payment) =>
        new(
            payment.Payment.PaymentId,
            payment.Payment.PaymentName,
            payment.Payment.IsTipeJaminan,
            payment.NilaiJasa,
            payment.NilaiObat);

    public static MergeRequestSummaryDto ToMergeRequestDto(MergeRequestModel request) =>
        new(
            request.MergeRequestId,
            request.SourceRegId,
            request.TargetRegId,
            request.Status);

    public static TataRekeningPaymentType ToPaymentType(PaymentAllocationInputDto input) =>
        new(
            new PaymentType(input.PaymentId, input.PaymentName, input.IsTipeJaminan),
            input.NilaiJasa,
            input.NilaiObat,
            new CoaType(input.CoaId, input.CoaName));

    public static PaymentType? ToSubsidyPayer(FinancialAdjustmentInputDto input)
    {
        if (string.IsNullOrWhiteSpace(input.SubsidyPaymentId))
            return null;

        return new PaymentType(
            input.SubsidyPaymentId,
            input.SubsidyPaymentName ?? input.SubsidyPaymentId,
            false);
    }
}
