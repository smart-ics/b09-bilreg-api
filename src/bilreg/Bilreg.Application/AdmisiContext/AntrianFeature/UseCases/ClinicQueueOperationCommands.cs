using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record ClinicQueueOperationResponse(
    string QueueCode,
    int NoUrut,
    string Status,
    int CallCount = 0)
{
    public string AntrianId => QueueCode;
}

public record ClinicQueueCallCmd(
    string QueueCode,
    int NoUrut,
    string PoliId,
    string DokterId,
    string? RuangId,
    string UserId) : IRequest<ClinicQueueOperationResponse>, IAntrianKey
{
    public string AntrianId => QueueCode;
}

public class ClinicQueueCallHandler : IRequestHandler<ClinicQueueCallCmd, ClinicQueueOperationResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IClinicQueueOperationDal _clinicQueueDal;
    private readonly ITglJamProvider _clock;
    private readonly IClinicQueueRefreshPublisher _publisher;

    public ClinicQueueCallHandler(
        IAntrianRepo antrianRepo,
        IClinicQueueOperationDal clinicQueueDal,
        ITglJamProvider clock,
        IClinicQueueRefreshPublisher? publisher = null)
    {
        _antrianRepo = antrianRepo;
        _clinicQueueDal = clinicQueueDal;
        _clock = clock;
        _publisher = publisher ?? new NullClinicQueueRefreshPublisher();
    }

    public async Task<ClinicQueueOperationResponse> Handle(
        ClinicQueueCallCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.QueueCode, nameof(request.QueueCode));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));
        Guard.Against.NullOrWhiteSpace(request.PoliId, nameof(request.PoliId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var que = _antrianRepo.LoadEntity(AntrianModel.Key(request.QueueCode)).GetValueOrDefault()
            ?? throw new KeyNotFoundException($"Antrian '{request.QueueCode}' not found");

        var entry = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut)
            ?? throw new KeyNotFoundException($"Antrian entry '{request.QueueCode}' / {request.NoUrut} not found");

        entry.RecordCall();
        _antrianRepo.SaveChanges(que);

        var now = _clock.Now;
        _clinicQueueDal.UpsertCall(new ClinicQueueCallDto
        {
            PoliId = request.PoliId.Trim(),
            DokterId = request.DokterId.Trim(),
            RuangId = request.RuangId?.Trim() ?? string.Empty,
            AntrianId = request.QueueCode.Trim(),
            NoUrut = request.NoUrut,
            CalledAt = now,
            CallCount = entry.CallCount,
            CallStatus = ClinicQueueCallStatus.Active,
            UserId = request.UserId.Trim(),
            CrtDate = now,
            UpdDate = now
        });

        await _publisher.PublishRefreshHint(request.PoliId.Trim(), request.DokterId.Trim(), cancellationToken);

        return new ClinicQueueOperationResponse(
            request.QueueCode,
            request.NoUrut,
            entry.AntrianStatus.ToString(),
            entry.CallCount);
    }
}

public record ClinicQueueRecallCmd(
    string QueueCode,
    int NoUrut,
    string PoliId,
    string DokterId,
    string? RuangId,
    string UserId) : IRequest<ClinicQueueOperationResponse>, IAntrianKey
{
    public string AntrianId => QueueCode;
}

public class ClinicQueueRecallHandler : IRequestHandler<ClinicQueueRecallCmd, ClinicQueueOperationResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IClinicQueueOperationDal _clinicQueueDal;
    private readonly ITglJamProvider _clock;
    private readonly IClinicQueueRefreshPublisher _publisher;

    public ClinicQueueRecallHandler(
        IAntrianRepo antrianRepo,
        IClinicQueueOperationDal clinicQueueDal,
        ITglJamProvider clock,
        IClinicQueueRefreshPublisher? publisher = null)
    {
        _antrianRepo = antrianRepo;
        _clinicQueueDal = clinicQueueDal;
        _clock = clock;
        _publisher = publisher ?? new NullClinicQueueRefreshPublisher();
    }

    public async Task<ClinicQueueOperationResponse> Handle(
        ClinicQueueRecallCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.QueueCode, nameof(request.QueueCode));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));
        Guard.Against.NullOrWhiteSpace(request.PoliId, nameof(request.PoliId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var que = _antrianRepo.LoadEntity(AntrianModel.Key(request.QueueCode)).GetValueOrDefault()
            ?? throw new KeyNotFoundException($"Antrian '{request.QueueCode}' not found");

        var entry = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut)
            ?? throw new KeyNotFoundException($"Antrian entry '{request.QueueCode}' / {request.NoUrut} not found");

        entry.RecordCall();
        _antrianRepo.SaveChanges(que);

        var now = _clock.Now;
        _clinicQueueDal.UpsertCall(new ClinicQueueCallDto
        {
            PoliId = request.PoliId.Trim(),
            DokterId = request.DokterId.Trim(),
            RuangId = request.RuangId?.Trim() ?? string.Empty,
            AntrianId = request.QueueCode.Trim(),
            NoUrut = request.NoUrut,
            CalledAt = now,
            CallCount = entry.CallCount,
            CallStatus = ClinicQueueCallStatus.Active,
            UserId = request.UserId.Trim(),
            CrtDate = now,
            UpdDate = now
        });

        await _publisher.PublishRefreshHint(request.PoliId.Trim(), request.DokterId.Trim(), cancellationToken);

        return new ClinicQueueOperationResponse(
            request.QueueCode,
            request.NoUrut,
            entry.AntrianStatus.ToString(),
            entry.CallCount);
    }
}

public record ClinicQueueSkipCmd(
    string QueueCode,
    int NoUrut,
    string PoliId,
    string DokterId,
    string UserId) : IRequest<ClinicQueueOperationResponse>, IAntrianKey
{
    public string AntrianId => QueueCode;
}

public class ClinicQueueSkipHandler : IRequestHandler<ClinicQueueSkipCmd, ClinicQueueOperationResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IClinicQueueOperationDal _clinicQueueDal;
    private readonly IClinicQueueRefreshPublisher _publisher;

    public ClinicQueueSkipHandler(
        IAntrianRepo antrianRepo,
        IClinicQueueOperationDal clinicQueueDal,
        IClinicQueueRefreshPublisher? publisher = null)
    {
        _antrianRepo = antrianRepo;
        _clinicQueueDal = clinicQueueDal;
        _publisher = publisher ?? new NullClinicQueueRefreshPublisher();
    }

    public async Task<ClinicQueueOperationResponse> Handle(
        ClinicQueueSkipCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.QueueCode, nameof(request.QueueCode));
        Guard.Against.NegativeOrZero(request.NoUrut, nameof(request.NoUrut));
        Guard.Against.NullOrWhiteSpace(request.PoliId, nameof(request.PoliId));
        Guard.Against.NullOrWhiteSpace(request.DokterId, nameof(request.DokterId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var que = _antrianRepo.LoadEntity(AntrianModel.Key(request.QueueCode)).GetValueOrDefault()
            ?? throw new KeyNotFoundException($"Antrian '{request.QueueCode}' not found");

        var entry = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut)
            ?? throw new KeyNotFoundException($"Antrian entry '{request.QueueCode}' / {request.NoUrut} not found");

        if (entry.AntrianStatus != AntrianStatusEnum.Waiting)
            throw new InvalidOperationException("Only a Waiting queue entry may be skipped.");

        _clinicQueueDal.MarkSkipped(request.PoliId.Trim(), request.DokterId.Trim(), request.UserId.Trim());

        await _publisher.PublishRefreshHint(request.PoliId.Trim(), request.DokterId.Trim(), cancellationToken);

        return new ClinicQueueOperationResponse(
            request.QueueCode,
            request.NoUrut,
            entry.AntrianStatus.ToString(),
            entry.CallCount);
    }
}
