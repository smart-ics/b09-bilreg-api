using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmGetWaitingListQry(string WaitingListId)
    : IRequest<AdmGetWaitingListResponse>, IWaitingListKey;

public record AdmGetWaitingListResponse(
    string WaitingListId,
    WaitingListStatusEnum WaitingListStatus,
    string RegId,
    PasienReff Pasien,
    KelasReff KelasRawat,
    BangsalReff Bangsal,
    int Priority,
    DateTime CrtDate);

public class AdmGetWaitingListHandler : IRequestHandler<AdmGetWaitingListQry, AdmGetWaitingListResponse>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public AdmGetWaitingListHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task<AdmGetWaitingListResponse> Handle(
        AdmGetWaitingListQry request,
        CancellationToken cancellationToken)
    {
        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        return Task.FromResult(new AdmGetWaitingListResponse(
            waitingList.WaitingListId,
            waitingList.WaitingListStatus,
            waitingList.RegId,
            waitingList.Pasien,
            waitingList.KelasRawat,
            waitingList.Bangsal,
            waitingList.Priority,
            waitingList.AuditTrail.Created.Timestamp));
    }
}
