using Bilreg.Domain.PaymentContext.TataRekeningFeature;

namespace Bilreg.Test.PaymentContext.TataRekeningFeature;

internal static class TataRekeningDomainTestHelper
{
    public static readonly DateTime DefaultVerificationDate = new(2026, 6, 16, 9, 0, 0);
    public static readonly DateTime DefaultFinalizationDate = new(2026, 6, 16, 10, 0, 0);
    public const string DefaultPetugasVerif = "verif-01";

    public static void CloseVerifyAllocateFinalize(
        TataRekeningModel tataRekening,
        IEnumerable<TataRekeningPaymentType> listPayment,
        string petugasVerif = DefaultPetugasVerif,
        DateTime? verificationDate = null,
        DateTime? finalizationDate = null)
    {
        tataRekening.Close();
        tataRekening.CompleteFinancialVerification(petugasVerif, verificationDate ?? DefaultVerificationDate);
        tataRekening.AllocateFinancialResponsibility(listPayment);
        tataRekening.FinalizeFinancialResponsibility(petugasVerif, finalizationDate ?? DefaultFinalizationDate);
    }

    public static void VerifyAndAllocate(
        TataRekeningModel tataRekening,
        IEnumerable<TataRekeningPaymentType> listPayment,
        string petugasVerif = DefaultPetugasVerif,
        DateTime? verificationDate = null)
    {
        tataRekening.CompleteFinancialVerification(petugasVerif, verificationDate ?? DefaultVerificationDate);
        tataRekening.AllocateFinancialResponsibility(listPayment);
    }
}
