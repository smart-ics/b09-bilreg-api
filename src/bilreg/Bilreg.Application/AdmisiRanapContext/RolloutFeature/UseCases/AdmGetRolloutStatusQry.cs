using MediatR;
using Microsoft.Extensions.Options;

namespace Bilreg.Application.AdmisiRanapContext.RolloutFeature.UseCases;

public record AdmGetRolloutStatusQry() : IRequest<AdmRolloutStatusResponse>;

public record AdmRolloutTableStatus(string Name, bool Ready);

public record AdmRolloutStatusResponse(
    bool Enabled,
    bool AllTablesReady,
    IReadOnlyList<AdmRolloutTableStatus> Tables);

public class AdmGetRolloutStatusHandler : IRequestHandler<AdmGetRolloutStatusQry, AdmRolloutStatusResponse>
{
    private static readonly string[] RequiredTables =
    [
        "BILRG_AdmOpnameRequest",
        "BILRG_AdmReservation",
        "BILRG_AdmAdmission",
        "BILRG_BedWaitingList"
    ];

    private readonly IAdmisiRanapRolloutDal _rolloutDal;
    private readonly AdmisiRanapOptions _options;

    public AdmGetRolloutStatusHandler(
        IAdmisiRanapRolloutDal rolloutDal,
        IOptions<AdmisiRanapOptions> options)
    {
        _rolloutDal = rolloutDal;
        _options = options.Value;
    }

    public Task<AdmRolloutStatusResponse> Handle(
        AdmGetRolloutStatusQry request,
        CancellationToken cancellationToken)
    {
        var tables = RequiredTables
            .Select(name => new AdmRolloutTableStatus(name, _rolloutDal.TableExists(name)))
            .ToList();

        var allReady = tables.All(t => t.Ready);

        return Task.FromResult(new AdmRolloutStatusResponse(
            _options.Enabled,
            allReady,
            tables));
    }
}
