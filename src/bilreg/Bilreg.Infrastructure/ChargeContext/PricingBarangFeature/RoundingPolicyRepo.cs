using Bilreg.Application.ChargeContext.PricingBarangFeature;
using Bilreg.Domain.ChargeContext.PricingBarangFeature;

namespace Bilreg.Infrastructure.ChargeContext.PricingBarangFeature;

public class RoundingPolicyRepo : IRoundingPolicyRepo
{
    private readonly IRoundingPolicyDal _roundingPolicyDal;

    public RoundingPolicyRepo(IRoundingPolicyDal roundingPolicyDal)
    {
        _roundingPolicyDal = roundingPolicyDal;
    }

    public RoundingPolicyModel GetPolicy() => _roundingPolicyDal.GetPolicy().ToModel();

    public void SavePolicy(RoundingPolicyModel model) =>
        _roundingPolicyDal.SavePolicy(RoundingPolicyDto.FromModel(model));
}
