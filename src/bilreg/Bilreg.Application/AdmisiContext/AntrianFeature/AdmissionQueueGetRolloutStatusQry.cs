using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AdmissionQueueGetRolloutStatusQry() : IRequest<AdmissionQueueRolloutStatusResponse>;

public record AdmissionQueueRolloutTableStatus(string Name, bool Ready);

public record AdmissionQueueRolloutIndexStatus(string Name, string TableName, bool Ready);

public record AdmissionQueueRolloutStatusResponse(
    bool AllSchemaReady,
    IReadOnlyList<AdmissionQueueRolloutTableStatus> Tables,
    IReadOnlyList<AdmissionQueueRolloutIndexStatus> Indexes,
    bool LegacyEndpointsEnabled,
    bool SignalRRefreshEnabled,
    bool WorkstationMappingsUnique,
    int WorkstationMappingCount);

public sealed class AdmissionQueueGetRolloutStatusHandler
    : IRequestHandler<AdmissionQueueGetRolloutStatusQry, AdmissionQueueRolloutStatusResponse>
{
    private readonly IAdmissionQueueRolloutDal _dal;
    private readonly IAdmissionQueueRolloutConfig _config;

    public AdmissionQueueGetRolloutStatusHandler(
        IAdmissionQueueRolloutDal dal,
        IAdmissionQueueRolloutConfig config)
    {
        _dal = dal;
        _config = config;
    }

    public Task<AdmissionQueueRolloutStatusResponse> Handle(
        AdmissionQueueGetRolloutStatusQry request,
        CancellationToken cancellationToken)
    {
        var tables = AdmissionQueueMigrationManifest.RequiredTables
            .Select(name => new AdmissionQueueRolloutTableStatus(name, _dal.TableExists(name)))
            .ToList();

        var indexes = AdmissionQueueMigrationManifest.RequiredIndexes
            .Select(i => new AdmissionQueueRolloutIndexStatus(
                i.IndexName, i.TableName, _dal.IndexExists(i.IndexName, i.TableName)))
            .ToList();

        var allReady = tables.All(t => t.Ready) && indexes.All(i => i.Ready);

        return Task.FromResult(new AdmissionQueueRolloutStatusResponse(
            allReady,
            tables,
            indexes,
            _config.LegacyEndpointsEnabled,
            _config.SignalRRefreshEnabled,
            _config.WorkstationMappingsUnique,
            _config.WorkstationMappingCount));
    }
}
