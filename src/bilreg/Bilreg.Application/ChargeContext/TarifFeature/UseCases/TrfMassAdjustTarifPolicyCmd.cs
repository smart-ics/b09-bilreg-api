using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfMassAdjustTarifPolicyCmd(
    string TarifPolicyId,
    string Scope,
    string AdjustmentType,
    decimal Value,
    string UserId) : IRequest, ITarifPolicyKey;

public class TrfMassAdjustTarifPolicyHandler : IRequestHandler<TrfMassAdjustTarifPolicyCmd>
{
    private readonly ITarifPolicyRepo _tarifPolicyRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TrfMassAdjustTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo, ITglJamProvider? tglJamProvider = null)
    {
        _tarifPolicyRepo = tarifPolicyRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(TrfMassAdjustTarifPolicyCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        if (!string.Equals(request.Scope, "ALL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Scope '{request.Scope}' tidak didukung; gunakan ALL.");

        if (!string.Equals(request.AdjustmentType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"AdjustmentType '{request.AdjustmentType}' tidak didukung; gunakan PERCENTAGE.");

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var updated = policy.MassAdjust(request.Value, request.UserId, _tglJamProvider.Now);
        _tarifPolicyRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
