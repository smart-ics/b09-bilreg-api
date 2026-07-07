using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmCloseWaitingListCmd(
    string WaitingListId,
    string UserId) : IRequest, IWaitingListKey;

public class AdmCloseWaitingListHandler : IRequestHandler<AdmCloseWaitingListCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public AdmCloseWaitingListHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task Handle(AdmCloseWaitingListCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var waitingList = AdmisiRanapSupport.LoadWaitingList(_waitingListRepo, request);
        var closed = waitingList.Close(request.UserId);

        _waitingListRepo.SaveChanges(closed);
        return Task.CompletedTask;
    }
}
