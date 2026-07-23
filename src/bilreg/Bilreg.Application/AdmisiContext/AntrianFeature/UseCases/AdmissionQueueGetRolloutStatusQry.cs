using MediatR;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionQueueGetRolloutStatusQry()
    : IRequest<AdmissionQueueGetRolloutStatusResponse>;

public record AdmissionQueueRolloutTableView(string Name, bool Ready);

public record AdmissionQueueRolloutIndexView(string Name, string TableName, bool Ready);

public record AdmissionQueueGetRolloutStatusResponse(
    bool AllSchemaReady,
    IReadOnlyList<AdmissionQueueRolloutTableView> Tables,
    IReadOnlyList<AdmissionQueueRolloutIndexView> Indexes,
    bool LegacyEndpointsEnabled,
    bool SignalRRefreshEnabled,
    bool WorkstationMappingsUnique,
    int WorkstationMappingCount);

public sealed class AdmissionQueueGetRolloutStatusHandler
    : IRequestHandler<AdmissionQueueGetRolloutStatusQry, AdmissionQueueGetRolloutStatusResponse>
{
    private readonly IAdmissionQueueRolloutRepo _repo;
    private readonly IAdmissionQueueRolloutConfig _config;

    public AdmissionQueueGetRolloutStatusHandler(
        IAdmissionQueueRolloutRepo repo,
        IAdmissionQueueRolloutConfig config)
    {
        _repo = repo;
        _config = config;
    }

    public Task<AdmissionQueueGetRolloutStatusResponse> Handle(
        AdmissionQueueGetRolloutStatusQry request,
        CancellationToken cancellationToken)
    {
        var tables = AdmissionQueueMigrationManifest.RequiredTables
            .Select(name => new AdmissionQueueRolloutTableView(name, _repo.TableExists(name)))
            .ToList();

        var indexes = AdmissionQueueMigrationManifest.RequiredIndexes
            .Select(i => new AdmissionQueueRolloutIndexView(
                i.IndexName, i.TableName, _repo.IndexExists(i.IndexName, i.TableName)))
            .ToList();

        var allSchemaReady = tables.All(t => t.Ready) && indexes.All(i => i.Ready);

        return Task.FromResult(new AdmissionQueueGetRolloutStatusResponse(
            allSchemaReady,
            tables,
            indexes,
            _config.LegacyEndpointsEnabled,
            _config.SignalRRefreshEnabled,
            _config.WorkstationMappingsUnique,
            _config.WorkstationMappingCount));
    }
}
