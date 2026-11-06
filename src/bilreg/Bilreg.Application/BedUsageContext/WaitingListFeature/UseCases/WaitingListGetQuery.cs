using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListGetQuery(string WaitingListId)
    : IRequest<WaitingListGetResponse>, IWaitingListKey;

public record WaitingListGetResponse(
    string WaitingListId,
    WaitingListStatusEnum WaitingListStatus,
    string RegId,
    PasienReff Pasien,
    KelasReff KelasRawat,
    BangsalReff Bangsal,
    ArrivalSourceTypeEnum ArrivalSourceType,
    LayananReff ReferringUnit,
    DokterReff ReferringDoctor,
    ClinicalUrgencyEnum ClinicalUrgency,
    string SpecialRequirements,
    BedReff PlannedBed,
    string CancellationReason,
    string RerouteReason,
    string PreviousWaitListId,
    string PlacedBedId,
    string PlacedPakaiBedId,
    int Priority,
    DateTime CrtDate,
    string CrtUser,
    DateTime UpdDate,
    string UpdUser,
    DateTime VodDate,
    string VodUser)
{
    public static WaitingListGetResponse FromModel(WaitingListModel model) =>
        new(
            model.WaitingListId,
            model.WaitingListStatus,
            model.RegId,
            model.Pasien,
            model.KelasRawat,
            model.Bangsal,
            model.ArrivalSourceType,
            model.ReferringUnit,
            model.ReferringDoctor,
            model.ClinicalUrgency,
            model.SpecialRequirements,
            model.PlannedBed,
            model.CancellationReason,
            model.RerouteReason,
            model.PreviousWaitListId,
            model.PlacedBedId,
            model.PlacedPakaiBedId,
            model.Priority,
            model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Created.UserId,
            model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Modified.UserId,
            model.AuditTrail.Voided.Timestamp,
            model.AuditTrail.Voided.UserId);
}

public class WaitingListGetHandler : IRequestHandler<WaitingListGetQuery, WaitingListGetResponse>
{
    private readonly IWaitingListRepo _waitingListRepo;

    public WaitingListGetHandler(IWaitingListRepo waitingListRepo) =>
        _waitingListRepo = waitingListRepo;

    public Task<WaitingListGetResponse> Handle(
        WaitingListGetQuery request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.WaitingListId, nameof(request.WaitingListId));

        var waitingList = _waitingListRepo.LoadEntity(request)
            .GetValueOrThrow($"Waiting List '{request.WaitingListId}' tidak ditemukan.");

        return Task.FromResult(WaitingListGetResponse.FromModel(waitingList));
    }
}
