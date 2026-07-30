using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QueAnonymousIntakeCmd(
    string ServicePointId) : IRequest<QueAnonymousIntakeResponse>;

public record QueAnonymousIntakeResponse(
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    DateTime CreatedAt);

public sealed class QueAnonymousIntakeHandler
    : IRequestHandler<QueAnonymousIntakeCmd, QueAnonymousIntakeResponse>
{
    private readonly IAntrianRepo _queues;
    private readonly IAntrianFactory _factory;
    private readonly ITglJamProvider _clock;
    private readonly IAdmissionServicePointRepo _servicePoints;

    public QueAnonymousIntakeHandler(
        IAntrianRepo queues,
        IAntrianFactory factory,
        ITglJamProvider clock,
        IAdmissionServicePointRepo servicePoints)
    {
        _queues = queues;
        _factory = factory;
        _clock = clock;
        _servicePoints = servicePoints;
    }

    public Task<QueAnonymousIntakeResponse> Handle(
        QueAnonymousIntakeCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ServicePointId);

        var occurredAt = _clock.Now;
        var businessDate = DateOnly.FromDateTime(occurredAt);

        var servicePoint = _servicePoints
            .LoadEntity(AdmissionServicePointModel.Key(request.ServicePointId))
            .GetValueOrThrow($"Admission Service Point '{request.ServicePointId}' not found");
        servicePoint.EnsureCanAcceptIntake();

        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);
        var queue = ResolveOrCreateQueue(servicePoint, businessDate, sequenceTag);

        try
        {
            return Task.FromResult(CommitIntake(queue, occurredAt));
        }
        catch (AdmissionQueueSessionRaceException)
        {
            // One-winner reload: unique SequenceTag loser continues on the winner session.
            var winner = ReloadWinnerSession(businessDate, sequenceTag);
            return Task.FromResult(CommitIntake(winner, occurredAt));
        }
    }

    private AntrianModel ResolveOrCreateQueue(
        AdmissionServicePointModel servicePoint,
        DateOnly businessDate,
        string sequenceTag)
    {
        var existingView = _queues.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag);

        return existingView is null
            ? _factory.Create(servicePoint, businessDate)
            : _queues.LoadEntity(existingView).Value;
    }

    private AntrianModel ReloadWinnerSession(DateOnly businessDate, string sequenceTag)
    {
        var winnerView = _queues.ListData(businessDate)
            .FirstOrDefault(x => x.SequenceTag == sequenceTag)
            ?? throw new AdmissionQueueConcurrencyException(
                $"Daily queue session for '{sequenceTag}' was created concurrently but could not be reloaded.");

        return _queues.LoadEntity(winnerView)
            .GetValueOrThrow(
                $"Admission queue session '{winnerView.AntrianId}' not found after concurrent create.");
    }

    private QueAnonymousIntakeResponse CommitIntake(AntrianModel queue, DateTime occurredAt)
    {
        using var trans = TransHelper.NewScope();
        var entry = queue.AddAdmissionEntry(occurredAt);
        _queues.SaveNewEntry(queue, entry);
        trans.Complete();

        return new QueAnonymousIntakeResponse(
            queue.AntrianId,
            entry.NoUrut,
            queue.FormatQueueLabel(entry.NoUrut) ?? string.Empty,
            entry.CreatedAt);
    }
}
