using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;

namespace Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;

public sealed class RegistrationCancellationEligibilityRepo : IRegistrationCancellationEligibilityRepo
{
    private readonly IRegistrationCancellationEligibilityDal _dal;

    public RegistrationCancellationEligibilityRepo(IRegistrationCancellationEligibilityDal dal) => _dal = dal;

    public bool HasBillingItems(string regId)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(regId);
        return _dal.HasBillingItems(regId);
    }
}
