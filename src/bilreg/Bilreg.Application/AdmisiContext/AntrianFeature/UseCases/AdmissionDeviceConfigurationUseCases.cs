using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record AdmissionWorkstationDtoResponse(
    string WorkstationKey,
    string DisplayName,
    string? LocationName,
    string LoketKey,
    string? LoketDisplayName,
    bool Active,
    string? Notes,
    string? CreatedAt,
    string? CreatedBy,
    string? UpdatedAt,
    string? UpdatedBy,
    string RowVersion);

public record AdmissionDisplayLoketDtoResponse(string LoketKey, int SortOrder);
public record AdmissionDisplayPoliDtoResponse(string PoliId, int SortOrder);

public record AdmissionQueueDisplayDtoResponse(
    string DisplayId,
    string DisplayName,
    string? LocationName,
    bool Active,
    bool AudioEnabled,
    int PollIntervalMs,
    string? LayoutKey,
    string? Notes,
    IReadOnlyList<AdmissionDisplayLoketDtoResponse> Lokets,
    string? CreatedAt,
    string? CreatedBy,
    string? UpdatedAt,
    string? UpdatedBy,
    string RowVersion,
    IReadOnlyList<AdmissionDisplayPoliDtoResponse>? Polis = null);

public record WorkstationContextDtoResponse(
    string WorkstationKey,
    string DisplayName,
    string? LocationName,
    string LoketKey,
    string? LoketDisplayName,
    bool Active);

public record DisplayBootConfigDtoResponse(
    string DeviceId,
    string Role,
    string DisplayName,
    string? LocationName,
    IReadOnlyList<string> LoketIds,
    int PollIntervalMs,
    bool AudioEnabled,
    string? LayoutKey,
    string? UpdatedAt,
    string? RowVersion,
    IReadOnlyList<string>? PoliIds = null);

public record ConfigurationSummaryDtoResponse(
    int ActiveWorkstationCount,
    int InactiveWorkstationCount,
    int ActiveDisplayCount,
    int InactiveDisplayCount,
    int ActiveKioskCount,
    int InactiveKioskCount,
    IReadOnlyList<string> ActiveLoketsWithoutDisplay,
    IReadOnlyList<string> DisplaysWithoutLoket,
    IReadOnlyList<string> KiosksWithoutServicePoint,
    IReadOnlyList<string> InvalidReferences,
    IReadOnlyList<object> RecentChanges);

public record SegmentationCoverageDtoResponse(
    IReadOnlyList<string> LoketKeys,
    IReadOnlyList<SegmentationDisplayDtoResponse> Displays);

public record SegmentationDisplayDtoResponse(
    string DisplayId,
    string DisplayName,
    bool Active,
    IReadOnlyList<string> LoketKeys);

public record ConfigurationAuditPageDtoResponse(
    IReadOnlyList<ConfigurationAuditEntryDtoResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);

public record ConfigurationAuditEntryDtoResponse(
    string AuditId,
    string EventTime,
    string UserId,
    string ActionType,
    string EntityName,
    string EntityId,
    string? Reason,
    string? OriginalDataJson,
    string? CorrelationId);

public record CreateWorkstationCmd(
    string WorkstationKey,
    string DisplayName,
    string? LocationName,
    string LoketKey,
    bool Active,
    string? Notes,
    string UserId) : IRequest<AdmissionWorkstationDtoResponse>;

public record UpdateWorkstationCmd(
    string WorkstationKey,
    string DisplayName,
    string? LocationName,
    string LoketKey,
    string? Notes,
    string RowVersion,
    string UserId) : IRequest<AdmissionWorkstationDtoResponse>;

public record SetWorkstationActiveCmd(
    string WorkstationKey,
    bool Active,
    string? RowVersion,
    string UserId) : IRequest<AdmissionWorkstationDtoResponse>;

public record ListWorkstationsQry : IRequest<IReadOnlyList<AdmissionWorkstationDtoResponse>>;
public record GetWorkstationQry(string WorkstationKey) : IRequest<AdmissionWorkstationDtoResponse>;
public record ListAvailableWorkstationsQry : IRequest<IReadOnlyList<WorkstationContextDtoResponse>>;
public record GetWorkstationContextQry(string WorkstationKey) : IRequest<WorkstationContextDtoResponse>;

public record CreateDisplayCmd(
    string DisplayId,
    string DisplayName,
    string? LocationName,
    bool Active,
    bool AudioEnabled,
    int PollIntervalMs,
    string? LayoutKey,
    string? Notes,
    IReadOnlyList<AdmissionDisplayLoketDtoResponse> Lokets,
    string UserId,
    IReadOnlyList<AdmissionDisplayPoliDtoResponse>? Polis = null) : IRequest<AdmissionQueueDisplayDtoResponse>;

public record UpdateDisplayCmd(
    string DisplayId,
    string DisplayName,
    string? LocationName,
    bool AudioEnabled,
    int PollIntervalMs,
    string? LayoutKey,
    string? Notes,
    string RowVersion,
    string UserId) : IRequest<AdmissionQueueDisplayDtoResponse>;

public record ReplaceDisplayLoketsCmd(
    string DisplayId,
    IReadOnlyList<AdmissionDisplayLoketDtoResponse> Lokets,
    string RowVersion,
    string UserId) : IRequest<AdmissionQueueDisplayDtoResponse>;

public record ReplaceDisplayPolisCmd(
    string DisplayId,
    IReadOnlyList<AdmissionDisplayPoliDtoResponse> Polis,
    string RowVersion,
    string UserId) : IRequest<AdmissionQueueDisplayDtoResponse>;

public record SetDisplayActiveCmd(
    string DisplayId,
    bool Active,
    string? RowVersion,
    string UserId) : IRequest<AdmissionQueueDisplayDtoResponse>;

public record ListDisplaysQry : IRequest<IReadOnlyList<AdmissionQueueDisplayDtoResponse>>;
public record GetDisplayQry(string DisplayId) : IRequest<AdmissionQueueDisplayDtoResponse>;
public record GetDisplayBootConfigQry(string DisplayId) : IRequest<DisplayBootConfigDtoResponse>;
public record GetPublicDisplaySnapshotQry(string DisplayId) : IRequest<IReadOnlyList<CurrentLoketDisplayItem>>;
public record GetConfigurationSummaryQry : IRequest<ConfigurationSummaryDtoResponse>;
public record GetSegmentationQry : IRequest<SegmentationCoverageDtoResponse>;
public record ListConfigurationAuditQry(int Page, int PageSize) : IRequest<ConfigurationAuditPageDtoResponse>;

public static class DeviceConfigurationMapping
{
    public static AdmissionWorkstationDtoResponse ToDto(AdmissionWorkstationModel model) => new(
        model.WorkstationKey,
        model.DisplayName,
        string.IsNullOrWhiteSpace(model.LocationName) ? null : model.LocationName,
        model.LoketKey,
        model.LoketKey,
        model.Active,
        string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes,
        model.CreatedAt.ToString("O"),
        model.CreatedBy,
        model.UpdatedAt.ToString("O"),
        model.UpdatedBy,
        model.RowVersion.ToString());

    public static AdmissionQueueDisplayDtoResponse ToDto(AdmissionQueueDisplayModel model) => new(
        model.DisplayId,
        model.DisplayName,
        string.IsNullOrWhiteSpace(model.LocationName) ? null : model.LocationName,
        model.Active,
        model.AudioEnabled,
        model.PollIntervalMs,
        string.IsNullOrWhiteSpace(model.LayoutKey) ? null : model.LayoutKey,
        string.IsNullOrWhiteSpace(model.Notes) ? null : model.Notes,
        model.Lokets.Select(x => new AdmissionDisplayLoketDtoResponse(x.LoketKey, x.SortOrder)).ToList(),
        model.CreatedAt.ToString("O"),
        model.CreatedBy,
        model.UpdatedAt.ToString("O"),
        model.UpdatedBy,
        model.RowVersion.ToString(),
        model.Polis.Select(x => new AdmissionDisplayPoliDtoResponse(x.PoliId, x.SortOrder)).ToList());

    public static long ParseRowVersion(string rowVersion)
    {
        if (!long.TryParse(rowVersion, out var value))
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Invalid,
                "rowVersion is invalid.");
        return value;
    }
}

public sealed class CreateWorkstationHandler : IRequestHandler<CreateWorkstationCmd, AdmissionWorkstationDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _repo;
    private readonly ITglJamProvider _clock;

    public CreateWorkstationHandler(IAdmissionWorkstationRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionWorkstationDtoResponse> Handle(CreateWorkstationCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (_repo.LoadEntity(AdmissionWorkstationModel.Key(request.WorkstationKey)).HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Invalid,
                $"Workstation '{request.WorkstationKey}' already exists.");

        var conflict = _repo.FindActiveByLoketKey(request.LoketKey);
        if (request.Active && conflict is not null)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationLoketConflict,
                $"Loket '{request.LoketKey}' is already mapped to active workstation '{conflict.WorkstationKey}'.");

        var model = AdmissionWorkstationModel.Create(
            request.WorkstationKey, request.DisplayName, request.LocationName ?? string.Empty,
            request.LoketKey, request.Active, request.Notes ?? string.Empty, request.UserId, _clock.Now);
        _repo.SaveChanges(model);
        return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
    }
}

public sealed class UpdateWorkstationHandler : IRequestHandler<UpdateWorkstationCmd, AdmissionWorkstationDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _repo;
    private readonly ITglJamProvider _clock;

    public UpdateWorkstationHandler(IAdmissionWorkstationRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionWorkstationDtoResponse> Handle(UpdateWorkstationCmd request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionWorkstationModel.Key(request.WorkstationKey));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationNotFound,
                $"Workstation '{request.WorkstationKey}' was not found.");

        var expected = DeviceConfigurationMapping.ParseRowVersion(request.RowVersion);
        AdmissionWorkstationModel model;
        try
        {
            model = existing.Value.Update(
                request.DisplayName, request.LocationName ?? string.Empty, request.LoketKey,
                request.Notes ?? string.Empty, expected, request.UserId, _clock.Now);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }

        if (model.Active)
        {
            var conflict = _repo.FindActiveByLoketKey(model.LoketKey, model.WorkstationKey);
            if (conflict is not null)
                throw new AdmissionQueueConfigurationException(
                    AdmissionQueueConfigurationErrorCodes.WorkstationLoketConflict,
                    $"Loket '{model.LoketKey}' is already mapped to active workstation '{conflict.WorkstationKey}'.");
        }

        _repo.SaveChanges(model);
        return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
    }
}

public sealed class SetWorkstationActiveHandler : IRequestHandler<SetWorkstationActiveCmd, AdmissionWorkstationDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _repo;
    private readonly ITglJamProvider _clock;

    public SetWorkstationActiveHandler(IAdmissionWorkstationRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionWorkstationDtoResponse> Handle(SetWorkstationActiveCmd request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionWorkstationModel.Key(request.WorkstationKey));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationNotFound,
                $"Workstation '{request.WorkstationKey}' was not found.");

        var expected = string.IsNullOrWhiteSpace(request.RowVersion)
            ? existing.Value.RowVersion
            : DeviceConfigurationMapping.ParseRowVersion(request.RowVersion!);

        AdmissionWorkstationModel model;
        try
        {
            model = request.Active
                ? existing.Value.Activate(expected, request.UserId, _clock.Now)
                : existing.Value.Deactivate(expected, request.UserId, _clock.Now);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }

        if (model.Active)
        {
            var conflict = _repo.FindActiveByLoketKey(model.LoketKey, model.WorkstationKey);
            if (conflict is not null)
                throw new AdmissionQueueConfigurationException(
                    AdmissionQueueConfigurationErrorCodes.WorkstationLoketConflict,
                    $"Loket '{model.LoketKey}' is already mapped to active workstation '{conflict.WorkstationKey}'.");
        }

        _repo.SaveChanges(model);
        return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
    }
}

public sealed class ListWorkstationsHandler : IRequestHandler<ListWorkstationsQry, IReadOnlyList<AdmissionWorkstationDtoResponse>>
{
    private readonly IAdmissionWorkstationRepo _repo;
    public ListWorkstationsHandler(IAdmissionWorkstationRepo repo) => _repo = repo;
    public Task<IReadOnlyList<AdmissionWorkstationDtoResponse>> Handle(ListWorkstationsQry request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdmissionWorkstationDtoResponse>>(
            _repo.ListAll().Select(DeviceConfigurationMapping.ToDto).ToList());
}

public sealed class GetWorkstationHandler : IRequestHandler<GetWorkstationQry, AdmissionWorkstationDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _repo;
    public GetWorkstationHandler(IAdmissionWorkstationRepo repo) => _repo = repo;
    public Task<AdmissionWorkstationDtoResponse> Handle(GetWorkstationQry request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionWorkstationModel.Key(request.WorkstationKey));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationNotFound,
                $"Workstation '{request.WorkstationKey}' was not found.");
        return Task.FromResult(DeviceConfigurationMapping.ToDto(existing.Value));
    }
}

public sealed class ListAvailableWorkstationsHandler
    : IRequestHandler<ListAvailableWorkstationsQry, IReadOnlyList<WorkstationContextDtoResponse>>
{
    private readonly IAdmissionWorkstationRepo _repo;
    public ListAvailableWorkstationsHandler(IAdmissionWorkstationRepo repo) => _repo = repo;
    public Task<IReadOnlyList<WorkstationContextDtoResponse>> Handle(
        ListAvailableWorkstationsQry request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<WorkstationContextDtoResponse>>(
            _repo.ListAll().Where(x => x.Active).Select(x => new WorkstationContextDtoResponse(
                x.WorkstationKey, x.DisplayName,
                string.IsNullOrWhiteSpace(x.LocationName) ? null : x.LocationName,
                x.LoketKey, x.LoketKey, x.Active)).ToList());
}

public sealed class GetWorkstationContextHandler : IRequestHandler<GetWorkstationContextQry, WorkstationContextDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _repo;
    public GetWorkstationContextHandler(IAdmissionWorkstationRepo repo) => _repo = repo;
    public Task<WorkstationContextDtoResponse> Handle(GetWorkstationContextQry request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionWorkstationModel.Key(request.WorkstationKey));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationNotFound,
                $"Workstation '{request.WorkstationKey}' was not found.");
        if (!existing.Value.Active)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.WorkstationInactive,
                $"Workstation '{request.WorkstationKey}' is inactive.");
        var x = existing.Value;
        return Task.FromResult(new WorkstationContextDtoResponse(
            x.WorkstationKey, x.DisplayName,
            string.IsNullOrWhiteSpace(x.LocationName) ? null : x.LocationName,
            x.LoketKey, x.LoketKey, x.Active));
    }
}

public sealed class CreateDisplayHandler : IRequestHandler<CreateDisplayCmd, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly ITglJamProvider _clock;
    private readonly IClinicQueueOperationDal? _clinicQueueDal;

    public CreateDisplayHandler(
        IAdmissionQueueDisplayRepo repo,
        ITglJamProvider clock,
        IClinicQueueOperationDal? clinicQueueDal = null)
    {
        _repo = repo;
        _clock = clock;
        _clinicQueueDal = clinicQueueDal;
    }

    public Task<AdmissionQueueDisplayDtoResponse> Handle(CreateDisplayCmd request, CancellationToken cancellationToken)
    {
        if (_repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId)).HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Invalid,
                $"Display '{request.DisplayId}' already exists.");

        try
        {
            var polis = request.Polis?.Select(x => new AdmissionDisplayPoliMapping(x.PoliId, x.SortOrder));
            var model = AdmissionQueueDisplayModel.Create(
                request.DisplayId, request.DisplayName, request.LocationName ?? string.Empty,
                request.Active, request.AudioEnabled, request.PollIntervalMs,
                request.LayoutKey ?? string.Empty, request.Notes ?? string.Empty,
                request.Lokets.Select(x => new AdmissionDisplayLoketMapping(x.LoketKey, x.SortOrder)),
                request.UserId, _clock.Now,
                polis);
            _repo.SaveChanges(model);

            if (model.Polis.Count > 0 && _clinicQueueDal is not null)
            {
                _clinicQueueDal.ReplaceDisplayPolis(
                    request.DisplayId,
                    model.Polis.Select(x => x.PoliId));
            }

            return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("requires at least one loket", StringComparison.OrdinalIgnoreCase) ||
                                                    ex.Message.Contains("requires at least one poli", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }
    }
}

public sealed class UpdateDisplayHandler : IRequestHandler<UpdateDisplayCmd, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly ITglJamProvider _clock;

    public UpdateDisplayHandler(IAdmissionQueueDisplayRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionQueueDisplayDtoResponse> Handle(UpdateDisplayCmd request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        try
        {
            var model = existing.Value.Update(
                request.DisplayName, request.LocationName ?? string.Empty, request.AudioEnabled,
                request.PollIntervalMs, request.LayoutKey ?? string.Empty, request.Notes ?? string.Empty,
                DeviceConfigurationMapping.ParseRowVersion(request.RowVersion), request.UserId, _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }
    }
}

public sealed class ReplaceDisplayLoketsHandler : IRequestHandler<ReplaceDisplayLoketsCmd, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly ITglJamProvider _clock;

    public ReplaceDisplayLoketsHandler(IAdmissionQueueDisplayRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionQueueDisplayDtoResponse> Handle(ReplaceDisplayLoketsCmd request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        try
        {
            var model = existing.Value.ReplaceLokets(
                request.Lokets.Select(x => new AdmissionDisplayLoketMapping(x.LoketKey, x.SortOrder)),
                DeviceConfigurationMapping.ParseRowVersion(request.RowVersion),
                request.UserId, _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("requires at least one loket", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }
    }
}

public sealed class ReplaceDisplayPolisHandler : IRequestHandler<ReplaceDisplayPolisCmd, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly IClinicQueueOperationDal _clinicQueueDal;
    private readonly ITglJamProvider _clock;

    public ReplaceDisplayPolisHandler(
        IAdmissionQueueDisplayRepo repo,
        IClinicQueueOperationDal clinicQueueDal,
        ITglJamProvider clock)
    {
        _repo = repo;
        _clinicQueueDal = clinicQueueDal;
        _clock = clock;
    }

    public Task<AdmissionQueueDisplayDtoResponse> Handle(ReplaceDisplayPolisCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.DisplayId, nameof(request.DisplayId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        try
        {
            var polis = (request.Polis ?? Array.Empty<AdmissionDisplayPoliDtoResponse>())
                .Select(x => new AdmissionDisplayPoliMapping(x.PoliId, x.SortOrder));

            var model = existing.Value.ReplacePolis(
                polis,
                DeviceConfigurationMapping.ParseRowVersion(request.RowVersion),
                request.UserId,
                _clock.Now);

            _repo.SaveChanges(model);

            _clinicQueueDal.ReplaceDisplayPolis(
                request.DisplayId,
                model.Polis.Select(x => x.PoliId));

            return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("requires at least one poli", StringComparison.OrdinalIgnoreCase) ||
                                                    ex.Message.Contains("no poli", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }
    }
}

public sealed class SetDisplayActiveHandler : IRequestHandler<SetDisplayActiveCmd, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly ITglJamProvider _clock;

    public SetDisplayActiveHandler(IAdmissionQueueDisplayRepo repo, ITglJamProvider clock)
    {
        _repo = repo;
        _clock = clock;
    }

    public Task<AdmissionQueueDisplayDtoResponse> Handle(SetDisplayActiveCmd request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        var expected = string.IsNullOrWhiteSpace(request.RowVersion)
            ? existing.Value.RowVersion
            : DeviceConfigurationMapping.ParseRowVersion(request.RowVersion!);

        try
        {
            var model = request.Active
                ? existing.Value.Activate(expected, request.UserId, _clock.Now)
                : existing.Value.Deactivate(expected, request.UserId, _clock.Now);
            _repo.SaveChanges(model);
            return Task.FromResult(DeviceConfigurationMapping.ToDto(model));
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("stale", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.Concurrency, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("requires at least one loket", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }
    }
}

public sealed class ListDisplaysHandler : IRequestHandler<ListDisplaysQry, IReadOnlyList<AdmissionQueueDisplayDtoResponse>>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    public ListDisplaysHandler(IAdmissionQueueDisplayRepo repo) => _repo = repo;
    public Task<IReadOnlyList<AdmissionQueueDisplayDtoResponse>> Handle(ListDisplaysQry request, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<AdmissionQueueDisplayDtoResponse>>(
            _repo.ListAll().Select(DeviceConfigurationMapping.ToDto).ToList());
}

public sealed class GetDisplayHandler : IRequestHandler<GetDisplayQry, AdmissionQueueDisplayDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    public GetDisplayHandler(IAdmissionQueueDisplayRepo repo) => _repo = repo;
    public Task<AdmissionQueueDisplayDtoResponse> Handle(GetDisplayQry request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");
        return Task.FromResult(DeviceConfigurationMapping.ToDto(existing.Value));
    }
}

public sealed class GetDisplayBootConfigHandler : IRequestHandler<GetDisplayBootConfigQry, DisplayBootConfigDtoResponse>
{
    private readonly IAdmissionQueueDisplayRepo _repo;
    private readonly IClinicQueueOperationDal? _clinicQueueDal;

    public GetDisplayBootConfigHandler(
        IAdmissionQueueDisplayRepo repo,
        IClinicQueueOperationDal? clinicQueueDal = null)
    {
        _repo = repo;
        _clinicQueueDal = clinicQueueDal;
    }

    public Task<DisplayBootConfigDtoResponse> Handle(GetDisplayBootConfigQry request, CancellationToken cancellationToken)
    {
        var existing = _repo.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        var x = existing.Value;
        if (x.IsClinicLayout && x.Polis.Count == 0 && _clinicQueueDal is not null)
        {
            var mappedPolis = _clinicQueueDal.GetDisplayPolis(request.DisplayId);
            if (mappedPolis.Count > 0)
            {
                x = x.ReplacePolis(mappedPolis.Select(p => new AdmissionDisplayPoliMapping(p.PoliId, p.SortOrder)), x.RowVersion, x.UpdatedBy, x.UpdatedAt);
            }
        }

        try
        {
            x.EnsureCanBoot();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayInactive, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no loket", StringComparison.OrdinalIgnoreCase) ||
                                                    ex.Message.Contains("no poli", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }

        var poliIds = x.Polis.Select(p => p.PoliId).ToList();
        if (x.IsClinicLayout && poliIds.Count == 0 && _clinicQueueDal is not null)
        {
            var fromDal = _clinicQueueDal.GetPolisByDisplayId(request.DisplayId);
            if (fromDal is not null && fromDal.Count > 0)
                poliIds = fromDal.ToList();
        }

        return Task.FromResult(new DisplayBootConfigDtoResponse(
            x.DisplayId, "display", x.DisplayName,
            string.IsNullOrWhiteSpace(x.LocationName) ? null : x.LocationName,
            x.Lokets.Select(l => l.LoketKey).ToList(),
            x.PollIntervalMs, x.AudioEnabled,
            string.IsNullOrWhiteSpace(x.LayoutKey) ? null : x.LayoutKey,
            x.UpdatedAt.ToString("O"),
            x.RowVersion.ToString(),
            x.IsClinicLayout ? (poliIds.Count > 0 ? poliIds : null) : null));
    }
}

public sealed class GetPublicDisplaySnapshotHandler
    : IRequestHandler<GetPublicDisplaySnapshotQry, IReadOnlyList<CurrentLoketDisplayItem>>
{
    private readonly IAdmissionQueueDisplayRepo _displays;
    private readonly IAdmissionQueueOperationalProjection _projection;
    private readonly IClinicQueueOperationDal? _clinicQueueDal;
    private readonly IAntrianRepo? _antrianRepo;

    public GetPublicDisplaySnapshotHandler(
        IAdmissionQueueDisplayRepo displays,
        IAdmissionQueueOperationalProjection projection,
        IClinicQueueOperationDal? clinicQueueDal = null,
        IAntrianRepo? antrianRepo = null)
    {
        _displays = displays;
        _projection = projection;
        _clinicQueueDal = clinicQueueDal;
        _antrianRepo = antrianRepo;
    }

    public Task<IReadOnlyList<CurrentLoketDisplayItem>> Handle(
        GetPublicDisplaySnapshotQry request,
        CancellationToken cancellationToken)
    {
        var existing = _displays.LoadEntity(AdmissionQueueDisplayModel.Key(request.DisplayId));
        if (!existing.HasValue)
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayNotFound,
                $"Display '{request.DisplayId}' was not found.");

        try
        {
            existing.Value.EnsureCanBoot();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("inactive", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayInactive, ex.Message);
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("no loket", StringComparison.OrdinalIgnoreCase) ||
                                                    ex.Message.Contains("no poli", StringComparison.OrdinalIgnoreCase))
        {
            throw new AdmissionQueueConfigurationException(
                AdmissionQueueConfigurationErrorCodes.DisplayMappingRequired, ex.Message);
        }

        var display = existing.Value;
        if (display.IsClinicLayout)
        {
            var poliIds = display.Polis.Select(p => p.PoliId).ToList();
            if (poliIds.Count == 0 && _clinicQueueDal is not null)
            {
                poliIds = _clinicQueueDal.GetPolisByDisplayId(display.DisplayId).ToList();
            }

            var activeCalls = _clinicQueueDal?.GetActiveCallsByPoliIds(poliIds) ?? Array.Empty<ClinicQueueCallDto>();
            var results = new List<CurrentLoketDisplayItem>();

            foreach (var call in activeCalls)
            {
                var queueLabel = call.NoUrut.ToString();
                if (_antrianRepo is not null)
                {
                    var queOpt = _antrianRepo.LoadEntity(AntrianModel.Key(call.AntrianId));
                    if (queOpt.HasValue)
                    {
                        var que = queOpt.Value;
                        queueLabel = $"{que.QueuePrefixSnapshot}{call.NoUrut:D4}";
                    }
                }

                results.Add(new CurrentLoketDisplayItem(
                    string.IsNullOrWhiteSpace(call.RuangId) ? call.PoliId : call.RuangId,
                    call.AntrianId,
                    call.NoUrut,
                    queueLabel,
                    call.PoliId,
                    AdmissionQueueClaimState.InService,
                    call.CallCount,
                    call.CalledAt,
                    call.CalledAt,
                    Array.Empty<byte>()));
            }

            return Task.FromResult<IReadOnlyList<CurrentLoketDisplayItem>>(results);
        }

        var displayLokets = display.Lokets
            .Select(x => x.LoketKey)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        return Task.FromResult<IReadOnlyList<CurrentLoketDisplayItem>>(
            _projection.ListCurrentLoket()
                .Where(x => displayLokets.Contains(x.LoketKey))
                .ToList());
    }
}

public sealed class GetConfigurationSummaryHandler : IRequestHandler<GetConfigurationSummaryQry, ConfigurationSummaryDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _workstations;
    private readonly IAdmissionQueueDisplayRepo _displays;
    private readonly IAdmissionQueueKioskRepo _kiosks;

    public GetConfigurationSummaryHandler(
        IAdmissionWorkstationRepo workstations,
        IAdmissionQueueDisplayRepo displays,
        IAdmissionQueueKioskRepo kiosks)
    {
        _workstations = workstations;
        _displays = displays;
        _kiosks = kiosks;
    }

    public Task<ConfigurationSummaryDtoResponse> Handle(GetConfigurationSummaryQry request, CancellationToken cancellationToken)
    {
        var ws = _workstations.ListAll();
        var ds = _displays.ListAll();
        var ks = _kiosks.ListAll();
        var coveredLokets = ds.Where(d => d.Active)
            .SelectMany(d => d.Lokets.Select(l => l.LoketKey))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var activeLoketsWithoutDisplay = ws.Where(w => w.Active && !coveredLokets.Contains(w.LoketKey))
            .Select(w => w.LoketKey).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToList();
        var displaysWithoutLoket = ds.Where(d => d.Active && d.Lokets.Count == 0)
            .Select(d => d.DisplayId).ToList();
        var kiosksWithoutServicePoint = ks.Where(k => k.Active && k.ServicePoints.Count == 0)
            .Select(k => k.StationId).ToList();

        return Task.FromResult(new ConfigurationSummaryDtoResponse(
            ws.Count(x => x.Active),
            ws.Count(x => !x.Active),
            ds.Count(x => x.Active),
            ds.Count(x => !x.Active),
            ks.Count(x => x.Active),
            ks.Count(x => !x.Active),
            activeLoketsWithoutDisplay,
            displaysWithoutLoket,
            kiosksWithoutServicePoint,
            Array.Empty<string>(),
            Array.Empty<object>()));
    }
}

public sealed class GetSegmentationHandler : IRequestHandler<GetSegmentationQry, SegmentationCoverageDtoResponse>
{
    private readonly IAdmissionWorkstationRepo _workstations;
    private readonly IAdmissionQueueDisplayRepo _displays;

    public GetSegmentationHandler(IAdmissionWorkstationRepo workstations, IAdmissionQueueDisplayRepo displays)
    {
        _workstations = workstations;
        _displays = displays;
    }

    public Task<SegmentationCoverageDtoResponse> Handle(GetSegmentationQry request, CancellationToken cancellationToken)
    {
        var displays = _displays.ListAll();
        var loketKeys = _workstations.ListAll().Select(x => x.LoketKey)
            .Concat(displays.SelectMany(d => d.Lokets.Select(l => l.LoketKey)))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x)
            .ToList();

        return Task.FromResult(new SegmentationCoverageDtoResponse(
            loketKeys,
            displays.Select(d => new SegmentationDisplayDtoResponse(
                d.DisplayId, d.DisplayName, d.Active,
                d.Lokets.Select(l => l.LoketKey).ToList())).ToList()));
    }
}

public sealed class ListConfigurationAuditHandler
    : IRequestHandler<ListConfigurationAuditQry, ConfigurationAuditPageDtoResponse>
{
    private readonly IAdmissionConfigurationAuditReader _reader;

    public ListConfigurationAuditHandler(IAdmissionConfigurationAuditReader reader) =>
        _reader = reader;

    public Task<ConfigurationAuditPageDtoResponse> Handle(
        ListConfigurationAuditQry request,
        CancellationToken cancellationToken)
    {
        var page = request.Page < 1 ? 1 : request.Page;
        var pageSize = request.PageSize is < 1 or > 200 ? 50 : request.PageSize;
        var items = _reader.List(page, pageSize, out var totalCount)
            .Select(x => new ConfigurationAuditEntryDtoResponse(
                x.AuditId,
                x.EventTime.ToString("O"),
                x.UserId,
                x.ActionType,
                x.EntityName,
                x.EntityId,
                string.IsNullOrWhiteSpace(x.Reason) ? null : x.Reason,
                string.IsNullOrWhiteSpace(x.OriginalDataJson) ? null : x.OriginalDataJson,
                string.IsNullOrWhiteSpace(x.CorrelationId) ? null : x.CorrelationId))
            .ToList();
        return Task.FromResult(new ConfigurationAuditPageDtoResponse(
            items,
            page,
            pageSize,
            totalCount));
    }
}
