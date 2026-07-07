using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmUpdateWaitingListCmd(
    string WaitingListId,
    int Priority,
    string KelasId,
    string BangsalId,
    string UserId) : IRequest, IWaitingListKey;

public class AdmUpdateWaitingListHandler : IRequestHandler<AdmUpdateWaitingListCmd>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmUpdateWaitingListHandler(
        IWaitingListRepo waitingListRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _waitingListRepo = waitingListRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task Handle(AdmUpdateWaitingListCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        var waitingList = AdmisiRanapSupport.LoadWaitingList(_waitingListRepo, request);
        var updated = waitingList.Update(request.Priority, kelas, bangsal, request.UserId);

        _waitingListRepo.SaveChanges(updated);
        return Task.CompletedTask;
    }
}
