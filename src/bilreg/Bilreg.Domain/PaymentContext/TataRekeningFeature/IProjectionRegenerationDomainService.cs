namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public interface IProjectionRegenerationDomainService
{
    void ClearProjection(TataRekeningModel tataRekening);

    void RegenerateProjection(
        TataRekeningModel tataRekening,
        IEnumerable<TataRekeningPaymentType> listPayment);
}
