using Bilreg.Domain.ChargeContext.PricingBarangFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.PricingBarangFeature.UseCases;

/// <summary>
/// Handler command pemutakhiran konfigurasi pembulatan kasir. Memvalidasi kelipatan pembulatan
/// lebih besar dari nol dan menyimpannya ke <c>tz_parameter_sistem</c> melalui
/// <see cref="IRoundingPolicyRepo"/>.
/// </summary>
public class RoundingPolicyUpdateHandler
    : IRequestHandler<RoundingPolicyUpdateCommand, RoundingPolicyUpdateResponse>
{
    private readonly IRoundingPolicyRepo _roundingPolicyRepo;

    public RoundingPolicyUpdateHandler(IRoundingPolicyRepo roundingPolicyRepo)
    {
        _roundingPolicyRepo = roundingPolicyRepo;
    }

    public Task<RoundingPolicyUpdateResponse> Handle(
        RoundingPolicyUpdateCommand request,
        CancellationToken cancellationToken)
    {
        if (request.NilaiPembulatan <= 0)
        {
            throw new ArgumentException(
                "Nilai kelipatan pembulatan harus lebih besar dari 0 " +
                $"(diterima: {request.NilaiPembulatan}).",
                nameof(request.NilaiPembulatan));
        }

        var model = new RoundingPolicyModel(request.NilaiPembulatan, request.JenisPembulatan);
        _roundingPolicyRepo.SavePolicy(model);

        return Task.FromResult(new RoundingPolicyUpdateResponse(
            model.NilaiPembulatan,
            model.JenisPembulatan));
    }
}
