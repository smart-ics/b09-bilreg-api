using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler <see cref="RoundingPolicyGetQuery"/>: membaca konfigurasi pembulatan aktif dan
/// memproyeksikannya menjadi DTO.
/// </summary>
public class RoundingPolicyGetHandler : IRequestHandler<RoundingPolicyGetQuery, RoundingPolicyGetResponse>
{
    private readonly IRoundingPolicyRepo _roundingPolicyRepo;

    public RoundingPolicyGetHandler(IRoundingPolicyRepo roundingPolicyRepo)
    {
        _roundingPolicyRepo = roundingPolicyRepo;
    }

    public Task<RoundingPolicyGetResponse> Handle(
        RoundingPolicyGetQuery request,
        CancellationToken cancellationToken)
    {
        var policy = _roundingPolicyRepo.GetPolicy();

        return Task.FromResult(new RoundingPolicyGetResponse(
            policy.NilaiPembulatan,
            policy.JenisPembulatan));
    }
}
