namespace Bilreg.Application.PaymentContext.KasirFeature;

public interface IKasirSupervisorAuthDal
{
    bool ValidateSupervisor(string userId, string password);
}
