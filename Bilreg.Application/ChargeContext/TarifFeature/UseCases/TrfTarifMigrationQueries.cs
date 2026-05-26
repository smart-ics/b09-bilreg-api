using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.ChargeContext.TarifFeature.UseCases;

public record TrfGetTarifMigrationStatusQry() : IRequest<TarifMigrationStatus>;

public class TrfGetTarifMigrationStatusHandler : IRequestHandler<TrfGetTarifMigrationStatusQry, TarifMigrationStatus>
{
    private readonly ITarifMigrationModeResolver _modeResolver;
    private readonly TarifMigrationOptions _options;
    private readonly TarifOperationalGate _operationalGate;
    private readonly ITarifOperationalStateRepo _operationalStateRepo;
    private readonly ITarifProjectionReadRepo _projectionReadRepo;
    private readonly ITarifPolicyRepo _tarifPolicyRepo;

    public TrfGetTarifMigrationStatusHandler(
        ITarifMigrationModeResolver modeResolver,
        IOptions<TarifMigrationOptions> options,
        TarifOperationalGate operationalGate,
        ITarifOperationalStateRepo operationalStateRepo,
        ITarifProjectionReadRepo projectionReadRepo,
        ITarifPolicyRepo tarifPolicyRepo)
    {
        _modeResolver = modeResolver;
        _options = options.Value;
        _operationalGate = operationalGate;
        _operationalStateRepo = operationalStateRepo;
        _projectionReadRepo = projectionReadRepo;
        _tarifPolicyRepo = tarifPolicyRepo;
    }

    public Task<TarifMigrationStatus> Handle(
        TrfGetTarifMigrationStatusQry request,
        CancellationToken cancellationToken)
    {
        var state = _operationalStateRepo.GetState();
        var (lastPublishAt, lastPublishPolicyId, lastPublishLogId) = _projectionReadRepo.GetLastPublish();
        var baselinePolicies = _tarifPolicyRepo
            .ListData(new TarifPolicyListFilter(Keyword: "BASELINE"))
            .Where(p => p.PolicyNo.StartsWith("BASELINE", StringComparison.OrdinalIgnoreCase))
            .Select(p => p.PolicyNo)
            .Distinct()
            .OrderBy(x => x)
            .ToList();

        var status = new TarifMigrationStatus(
            _modeResolver.GetEffectiveMode(),
            _modeResolver.GetConfigMode(),
            _modeResolver.GetDbOverrideMode(),
            _modeResolver.AllowRoutineImport(),
            _modeResolver.AllowPublish(),
            _options.AllowEmergencyImport,
            _operationalGate.IsOperationInProgress,
            _operationalGate.ActiveOperation,
            _projectionReadRepo.GetProjectionSummary(),
            state.LastImportAt,
            state.LastImportBy,
            lastPublishAt,
            lastPublishPolicyId,
            lastPublishLogId,
            state.LastBaselineAt,
            string.IsNullOrWhiteSpace(state.LastBaselinePolicyId) ? null : state.LastBaselinePolicyId,
            baselinePolicies);

        return Task.FromResult(status);
    }
}

public record TrfCheckTarifProjectionConsistencyQry() : IRequest<TarifProjectionConsistencyReport>;

public class TrfCheckTarifProjectionConsistencyHandler
    : IRequestHandler<TrfCheckTarifProjectionConsistencyQry, TarifProjectionConsistencyReport>
{
    private readonly ITarifProjectionReadRepo _projectionReadRepo;

    public TrfCheckTarifProjectionConsistencyHandler(ITarifProjectionReadRepo projectionReadRepo) =>
        _projectionReadRepo = projectionReadRepo;

    public Task<TarifProjectionConsistencyReport> Handle(
        TrfCheckTarifProjectionConsistencyQry request,
        CancellationToken cancellationToken) =>
        Task.FromResult(_projectionReadRepo.GetConsistencyReport());
}

public record TrfSetTarifMigrationModeCmd(
    TarifMigrationMode? Mode,
    string UserId) : IRequest;

public class TrfSetTarifMigrationModeHandler : IRequestHandler<TrfSetTarifMigrationModeCmd>
{
    private readonly ITarifOperationalStateRepo _operationalStateRepo;

    public TrfSetTarifMigrationModeHandler(ITarifOperationalStateRepo operationalStateRepo) =>
        _operationalStateRepo = operationalStateRepo;

    public Task Handle(TrfSetTarifMigrationModeCmd request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.UserId))
            throw new ArgumentException("UserId wajib diisi.");

        _operationalStateRepo.SetMigrationModeOverride(request.Mode, request.UserId);
        return Task.CompletedTask;
    }
}
