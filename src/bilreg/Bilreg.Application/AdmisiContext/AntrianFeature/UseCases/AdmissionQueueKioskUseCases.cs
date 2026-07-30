using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public sealed record AdmissionKioskServicePointDtoResponse(string ServicePointId, int SortOrder);

public sealed record AdmissionQueueKioskDtoResponse(
    string StationId,
    string DisplayName,
    string? LocationName,
    bool Active,
    int PrinterProxyPort,
    string? Notes,
    IReadOnlyList<AdmissionKioskServicePointDtoResponse> ServicePoints,
    string? CreatedAt,
    string? CreatedBy,
    string? UpdatedAt,
    string? UpdatedBy,
    string RowVersion);

public sealed record KioskBootConfigDtoResponse(
    string DeviceId,
    string Role,
    string DisplayName,
    string? LocationName,
    IReadOnlyList<string> ServicePointIds,
    int PrinterProxyPort,
    string? UpdatedAt,
    string? RowVersion);

public sealed record CreateKioskCmd(
    string StationId,
    string DisplayName,
    string? LocationName,
    bool Active,
    int PrinterProxyPort,
    string? Notes,
    IReadOnlyList<AdmissionKioskServicePointDtoResponse> ServicePoints,
    string UserId) : IRequest<AdmissionQueueKioskDtoResponse>;

public sealed record UpdateKioskCmd(
    string StationId,
    string DisplayName,
    string? LocationName,
    int PrinterProxyPort,
    string? Notes,
    string RowVersion,
    string UserId) : IRequest<AdmissionQueueKioskDtoResponse>;

public sealed record ReplaceKioskServicePointsCmd(
    string StationId,
    IReadOnlyList<AdmissionKioskServicePointDtoResponse> ServicePoints,
    string RowVersion,
    string UserId) : IRequest<AdmissionQueueKioskDtoResponse>;

public sealed record SetKioskActiveCmd(
    string StationId,
    bool Active,
    string? RowVersion,
    string UserId) : IRequest<AdmissionQueueKioskDtoResponse>;

public sealed record ListKiosksQry : IRequest<IReadOnlyList<AdmissionQueueKioskDtoResponse>>;
public sealed record GetKioskQry(string StationId) : IRequest<AdmissionQueueKioskDtoResponse>;
public sealed record GetKioskBootConfigQry(string StationId) : IRequest<KioskBootConfigDtoResponse>;

public static class KioskConfigurationMapping
{
    public static AdmissionQueueKioskDtoResponse ToDto(AdmissionQueueKioskModel model) => new(
        model.StationId,
        model.DisplayName,
        string.IsNullOrWhiteSpace(model.LocationName) ? null : model.LocationName,
        model.Active,
        model.PrinterProxyPort,
        string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes,
        model.ServicePoints.Select(x =>
            new AdmissionKioskServicePointDtoResponse(x.ServicePointId, x.SortOrder)).ToList(),
        model.CreatedAt.ToString("O"),
        model.CreatedBy,
        model.UpdatedAt.ToString("O"),
        model.UpdatedBy,
        model.RowVersion.ToString());
}

public sealed class CreateKioskHandler : IRequestHandler<CreateKioskCmd, AdmissionQueueKioskDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    private readonly IAdmissionServicePointRepo _servicePoints;
    private readonly ITglJamProvider _clock;

    public CreateKioskHandler(
        IAdmissionQueueKioskRepo repo,
        IAdmissionServicePointRepo servicePoints,
        ITglJamProvider clock)
    {
        _repo = repo;
        _servicePoints = servicePoints;
        _clock = clock;
    }

    public Task<AdmissionQueueKioskDtoResponse> Handle(CreateKioskCmd request, CancellationToken cancellationToken)
    {
        if (_repo.LoadEntity(AdmissionQueueKioskModel.Key(request.StationId)).HasValue)
            throw Invalid($"Kiosk '{request.StationId}' already exists.");
        ValidateServicePoints(_servicePoints, request.ServicePoints);
        try
        {
            var model = AdmissionQueueKioskModel.Create(
                request.StationId,
                request.DisplayName,
                request.LocationName ?? string.Empty,
                request.Active,
                request.PrinterProxyPort,
                request.Notes ?? string.Empty,
                request.ServicePoints.Select(x =>
                    new AdmissionKioskServicePointMapping(x.ServicePointId, x.SortOrder)),
                request.UserId,
                _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(KioskConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (IsMappingError(ex))
        {
            throw MappingRequired(ex.Message);
        }
    }

    internal static void ValidateServicePoints(
        IAdmissionServicePointRepo repo,
        IReadOnlyList<AdmissionKioskServicePointDtoResponse> mappings)
    {
        foreach (var mapping in mappings)
        {
            if (!repo.LoadEntity(AdmissionServicePointModel.Key(mapping.ServicePointId)).HasValue)
                throw Invalid($"Service point '{mapping.ServicePointId}' was not found.");
        }
    }

    internal static AdmissionQueueConfigurationException Invalid(string message) =>
        new(AdmissionQueueConfigurationErrorCodes.Invalid, message);

    internal static AdmissionQueueConfigurationException MappingRequired(string message) =>
        new(AdmissionQueueConfigurationErrorCodes.KioskMappingRequired, message);

    internal static bool IsMappingError(InvalidOperationException ex) =>
        ex.Message.Contains("service point", StringComparison.OrdinalIgnoreCase);
}

public sealed class UpdateKioskHandler : IRequestHandler<UpdateKioskCmd, AdmissionQueueKioskDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    private readonly ITglJamProvider _clock;

    public UpdateKioskHandler(IAdmissionQueueKioskRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionQueueKioskDtoResponse> Handle(UpdateKioskCmd request, CancellationToken cancellationToken)
    {
        var existing = GetExisting(_repo, request.StationId);
        try
        {
            var model = existing.Update(
                request.DisplayName,
                request.LocationName ?? string.Empty,
                request.PrinterProxyPort,
                request.Notes ?? string.Empty,
                DeviceConfigurationMapping.ParseRowVersion(request.RowVersion),
                request.UserId,
                _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(KioskConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (IsStale(ex))
        {
            throw Concurrency(ex.Message);
        }
    }

    internal static AdmissionQueueKioskModel GetExisting(IAdmissionQueueKioskRepo repo, string stationId)
    {
        var existing = repo.LoadEntity(AdmissionQueueKioskModel.Key(stationId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.KioskNotFound,
                $"Kiosk '{stationId}' was not found.");
        return existing.Value;
    }

    internal static bool IsStale(InvalidOperationException ex) =>
        ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase);

    internal static AdmissionQueueConfigurationException Concurrency(string message) =>
        new(AdmissionQueueConfigurationErrorCodes.Concurrency, message);
}

public sealed class ReplaceKioskServicePointsHandler :
    IRequestHandler<ReplaceKioskServicePointsCmd, AdmissionQueueKioskDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    private readonly IAdmissionServicePointRepo _servicePoints;
    private readonly ITglJamProvider _clock;

    public ReplaceKioskServicePointsHandler(
        IAdmissionQueueKioskRepo repo,
        IAdmissionServicePointRepo servicePoints,
        ITglJamProvider clock)
    {
        _repo = repo;
        _servicePoints = servicePoints;
        _clock = clock;
    }

    public Task<AdmissionQueueKioskDtoResponse> Handle(
        ReplaceKioskServicePointsCmd request,
        CancellationToken cancellationToken)
    {
        var existing = UpdateKioskHandler.GetExisting(_repo, request.StationId);
        CreateKioskHandler.ValidateServicePoints(_servicePoints, request.ServicePoints);
        try
        {
            var model = existing.ReplaceServicePoints(
                request.ServicePoints.Select(x =>
                    new AdmissionKioskServicePointMapping(x.ServicePointId, x.SortOrder)),
                DeviceConfigurationMapping.ParseRowVersion(request.RowVersion),
                request.UserId,
                _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(KioskConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (UpdateKioskHandler.IsStale(ex))
        {
            throw UpdateKioskHandler.Concurrency(ex.Message);
        }
        catch (InvalidOperationException ex) when (CreateKioskHandler.IsMappingError(ex))
        {
            throw CreateKioskHandler.MappingRequired(ex.Message);
        }
    }
}

public sealed class SetKioskActiveHandler : IRequestHandler<SetKioskActiveCmd, AdmissionQueueKioskDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    private readonly ITglJamProvider _clock;

    public SetKioskActiveHandler(IAdmissionQueueKioskRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionQueueKioskDtoResponse> Handle(SetKioskActiveCmd request, CancellationToken cancellationToken)
    {
        var existing = UpdateKioskHandler.GetExisting(_repo, request.StationId);
        var expected = string.IsNullOrWhiteSpace(request.RowVersion)
            ? existing.RowVersion
            : DeviceConfigurationMapping.ParseRowVersion(request.RowVersion!);
        try
        {
            var model = request.Active
                ? existing.Activate(expected, request.UserId, _clock.Now)
                : existing.Deactivate(expected, request.UserId, _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(KioskConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (UpdateKioskHandler.IsStale(ex))
        {
            throw UpdateKioskHandler.Concurrency(ex.Message);
        }
        catch (InvalidOperationException ex) when (CreateKioskHandler.IsMappingError(ex))
        {
            throw CreateKioskHandler.MappingRequired(ex.Message);
        }
    }
}

public sealed class ListKiosksHandler : IRequestHandler<ListKiosksQry, IReadOnlyList<AdmissionQueueKioskDtoResponse>>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    public ListKiosksHandler(IAdmissionQueueKioskRepo repo) => _repo = repo;

    public Task<IReadOnlyList<AdmissionQueueKioskDtoResponse>> Handle(
        ListKiosksQry request,
        CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdmissionQueueKioskDtoResponse>>(
            _repo.ListAll().Select(KioskConfigurationMapping.ToDto).ToList());
}

public sealed class GetKioskHandler : IRequestHandler<GetKioskQry, AdmissionQueueKioskDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    public GetKioskHandler(IAdmissionQueueKioskRepo repo) => _repo = repo;

    public Task<AdmissionQueueKioskDtoResponse> Handle(GetKioskQry request, CancellationToken cancellationToken) =>
        Task.FromResult(KioskConfigurationMapping.ToDto(
            UpdateKioskHandler.GetExisting(_repo, request.StationId)));
}

public sealed class GetKioskBootConfigHandler : IRequestHandler<GetKioskBootConfigQry, KioskBootConfigDtoResponse>
{
    private readonly IAdmissionQueueKioskRepo _repo;
    public GetKioskBootConfigHandler(IAdmissionQueueKioskRepo repo) => _repo = repo;

    public Task<KioskBootConfigDtoResponse> Handle(
        GetKioskBootConfigQry request,
        CancellationToken cancellationToken)
    {
        var kiosk = UpdateKioskHandler.GetExisting(_repo, request.StationId);
        try
        {
            kiosk.EnsureCanBoot();
        }
        catch (InvalidOperationException ex) when (
            ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.KioskInactive,
                ex.Message);
        }
        catch (InvalidOperationException ex) when (CreateKioskHandler.IsMappingError(ex))
        {
            throw CreateKioskHandler.MappingRequired(ex.Message);
        }

        return Task.FromResult(new KioskBootConfigDtoResponse(
            kiosk.StationId,
            "kiosk",
            kiosk.DisplayName,
            string.IsNullOrWhiteSpace(kiosk.LocationName) ? null : kiosk.LocationName,
            kiosk.ServicePoints.Select(x => x.ServicePointId).ToList(),
            kiosk.PrinterProxyPort,
            kiosk.UpdatedAt.ToString("O"),
            kiosk.RowVersion.ToString()));
    }
}
