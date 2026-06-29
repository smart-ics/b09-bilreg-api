using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.PaymentContext.PasienBalanceFeature;

public interface IPasienBalanceRebuilder
{
    void RebuildPatient(IPasienKey pasien);

    void RebuildAll();

    PasienBalanceValidationResult ValidatePatient(IPasienKey pasien);
}
