using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmGetWaitingListByRegIdQry(string RegId)
    : IRequest<AdmGetWaitingListResponse?>;

public class AdmGetWaitingListByRegIdHandler
    : IRequestHandler<AdmGetWaitingListByRegIdQry, AdmGetWaitingListResponse?>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public AdmGetWaitingListByRegIdHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task<AdmGetWaitingListResponse?> Handle(
        AdmGetWaitingListByRegIdQry request,
        CancellationToken cancellationToken)
    {
        var waitingList = _waitingListRepo.LoadActiveByRegId(request.RegId);
        if (!waitingList.HasValue)
            return Task.FromResult<AdmGetWaitingListResponse?>(null);

        return waitingList.Match(
            onSome: wl => Task.FromResult<AdmGetWaitingListResponse?>(new AdmGetWaitingListResponse(
                wl.WaitingListId,
                wl.WaitingListStatus,
                wl.RegId,
                wl.Pasien,
                wl.KelasRawat,
                wl.Bangsal,
                wl.Priority,
                wl.AuditTrail.Created.Timestamp)),
            onNone: () => Task.FromResult<AdmGetWaitingListResponse?>(null));
    }
}
