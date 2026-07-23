using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueAnonymousIntakeCmd(
    string ServicePointId) : IRequest<QueAnonymousIntakeResponse>;

public record QueAnonymousIntakeResponse(
    string AntrianId,
    int NoUrut,
    string QueueLabel,
    DateTime CreatedAt);

public class QueAnonymousIntakeHandler
    : IRequestHandler<QueAnonymousIntakeCmd, QueAnonymousIntakeResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IAdmissionServicePointRepo _servicePointRepo;

    public QueAnonymousIntakeHandler(
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        ITglJamProvider tglJamProvider,
        IAdmissionServicePointRepo servicePointRepo)
    {
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _tglJamProvider = tglJamProvider;
        _servicePointRepo = servicePointRepo;
    }

    public Task<QueAnonymousIntakeResponse> Handle(
        QueAnonymousIntakeCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.ServicePointId);

        var occurredAt = _tglJamProvider.Now;
        var businessDate = DateOnly.FromDateTime(occurredAt);
        var servicePoint = _servicePointRepo
            .LoadEntity(AdmissionServicePointModel.Key(request.ServicePointId))
            .GetValueOrThrow($"Admission Service Point '{request.ServicePointId}' not found.");
        servicePoint.EnsureCanAcceptIntake();
        var reference = new ServicePointType(servicePoint.ServicePointId, servicePoint.DisplayName);
        var sequenceTag = AntrianModel.GenSequenceTag(businessDate, TimeOnly.MinValue, reference);

        var listQue = _antrianRepo.ListData(businessDate);
        var queView = listQue.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var que = queView is null
            ? _antrianFactory.Create(servicePoint, businessDate)
            : _antrianRepo.LoadEntity(queView).Value;

        try
        {
            return Task.FromResult(CommitIntake(que, occurredAt));
        }
        catch (AdmissionQueueSessionRaceException)
        {
            // One-winner reload: unique SequenceTag loser continues on the winner session.
            var winnerView = _antrianRepo.ListData(businessDate)
                .FirstOrDefault(x => x.SequenceTag == sequenceTag)
                ?? throw new AdmissionQueueConcurrencyException(
                    $"Daily queue session for '{sequenceTag}' was created concurrently but could not be reloaded.");
            var winner = _antrianRepo.LoadEntity(winnerView)
                .GetValueOrThrow($"Admission queue session '{winnerView.AntrianId}' not found after concurrent create.");
            return Task.FromResult(CommitIntake(winner, occurredAt));
        }
    }

    private QueAnonymousIntakeResponse CommitIntake(AntrianModel que, DateTime occurredAt)
    {
        using var trans = TransHelper.NewScope();
        var entry = que.AddAdmissionEntry(occurredAt);
        _antrianRepo.SaveNewEntry(que, entry);
        trans.Complete();

        return new QueAnonymousIntakeResponse(
            que.AntrianId,
            entry.NoUrut,
            que.FormatQueueLabel(entry.NoUrut) ?? string.Empty,
            entry.CreatedAt);
    }
}
