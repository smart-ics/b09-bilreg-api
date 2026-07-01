namespace Bilreg.Domain.PaymentContext.TataRekeningFeature;

public sealed class ProjectionRegenerationDomainService : IProjectionRegenerationDomainService
{
    public void ClearProjection(TataRekeningModel tataRekening)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        tataRekening.ClearFinancialProjection();
    }

    public void RegenerateProjection(
        TataRekeningModel tataRekening,
        IEnumerable<TataRekeningPaymentType> listPayment)
    {
        ArgumentNullException.ThrowIfNull(tataRekening);
        ArgumentNullException.ThrowIfNull(listPayment);

        tataRekening.RegenerateFinancialProjection(listPayment);
    }
}
