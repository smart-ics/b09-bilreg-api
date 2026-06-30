using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Infrastructure.PaymentContext.TataRekeningFeature;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal static class TataRekeningDtoTestHelper
{
    private static readonly DateTime EmptyDate = new(3000, 1, 1);

    public static BilrgTataRekeningDto BuildHeaderDto(
        string regId = "RG-TATA-01",
        TataRekeningStatusEnum status = TataRekeningStatusEnum.Closed,
        int version = 1) =>
        new(
            regId,
            (int)status,
            "-",
            EmptyDate,
            (int)FinancialVerificationStatusEnum.NotVerified,
            string.Empty,
            EmptyDate,
            false,
            false,
            version);
}
