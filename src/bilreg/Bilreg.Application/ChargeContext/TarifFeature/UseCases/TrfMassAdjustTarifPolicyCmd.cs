using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.TarifFeature;
using MediatR;

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

    public TrfMassAdjustTarifPolicyHandler(ITarifPolicyRepo tarifPolicyRepo) =>
        _tarifPolicyRepo = tarifPolicyRepo;

    public Task Handle(TrfMassAdjustTarifPolicyCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);

        if (!string.Equals(request.Scope, "ALL", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException($"Scope '{request.Scope}' tidak didukung; gunakan ALL.");

        if (!string.Equals(request.AdjustmentType, "PERCENTAGE", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException(
                $"AdjustmentType '{request.AdjustmentType}' tidak didukung; gunakan PERCENTAGE.");

        var policy = TrfTarifPolicySupport.LoadPolicy(_tarifPolicyRepo, request);
        var updated = policy.MassAdjust(request.Value, request.UserId);
        _tarifPolicyRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
