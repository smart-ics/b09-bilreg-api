using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.Shared;
using Bilreg.Domain.BedUsageContext.WaitingListFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.WaitingListFeature.UseCases;

public record WaitingListCreateCmd(
    string RegId,
    string KelasId,
    string BangsalId,
    ArrivalSourceTypeEnum ArrivalSourceType = ArrivalSourceTypeEnum.Adm,
    string ReferringUnitId = "",
    string ReferringUnitName = "",
    string ReferringDoctorId = "",
    string ReferringDoctorName = "",
    ClinicalUrgencyEnum ClinicalUrgency = ClinicalUrgencyEnum.Routine,
    string SpecialRequirements = "",
    string PlannedBedId = "",
    string PlannedBedName = "",
    int Priority = 0,
    string UserId = "") : IRequest<WaitingListCreateResponse>, IRegKey;

public record WaitingListCreateResponse(string WaitingListId);

public class WaitingListCreateHandler : IRequestHandler<WaitingListCreateCmd, WaitingListCreateResponse>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;
    private readonly IBedRepo _bedRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public WaitingListCreateHandler(
        IWaitingListRepo waitingListRepo,
        IAdmissionRepo admissionRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo,
        IBedRepo bedRepo,
        ITglJamProvider tglJamProvider)
    {
        _waitingListRepo = waitingListRepo;
        _admissionRepo = admissionRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
        _bedRepo = bedRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<WaitingListCreateResponse> Handle(
        WaitingListCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.KelasId, nameof(request.KelasId));
        Guard.Against.NullOrWhiteSpace(request.BangsalId, nameof(request.BangsalId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");

        AdmissionStatusGuard.EnsureCanEnterWaitingList(admission.AdmissionStatus);

        if (_waitingListRepo.HasActiveByRegId(request.RegId))
            throw new InvalidOperationException($"Admission '{request.RegId}' sudah memiliki Waiting List aktif.");

        var kelas = _kelasRepo.LoadEntity(KelasType.Key(request.KelasId))
            .GetValueOrThrow($"Kelas '{request.KelasId}' tidak ditemukan.")
            .ToReff();

        var bangsal = _bangsalRepo.LoadEntity(BangsalType.Key(request.BangsalId))
            .GetValueOrThrow($"Bangsal '{request.BangsalId}' tidak ditemukan.")
            .ToReff();

        BedReff plannedBed;
        if (!string.IsNullOrWhiteSpace(request.PlannedBedId) && request.PlannedBedId != "-")
        {
            var bed = _bedRepo.LoadEntity(BedType.Key(request.PlannedBedId))
                .GetValueOrThrow($"Bed '{request.PlannedBedId}' tidak ditemukan.");
            plannedBed = bed.ToReff();
        }
        else
        {
            plannedBed = new BedReff("-", "-", false);
        }

        var referringUnit = new LayananReff(
            string.IsNullOrWhiteSpace(request.ReferringUnitId) ? "-" : request.ReferringUnitId,
            string.IsNullOrWhiteSpace(request.ReferringUnitName) ? "-" : request.ReferringUnitName);

        var referringDoctor = new DokterReff(
            string.IsNullOrWhiteSpace(request.ReferringDoctorId) ? "-" : request.ReferringDoctorId,
            string.IsNullOrWhiteSpace(request.ReferringDoctorName) ? "-" : request.ReferringDoctorName);

        var occurredAt = _tglJamProvider.Now;
        var waitingList = WaitingListModel.Create(
            admission.RegId,
            admission.Pasien,
            kelas,
            bangsal,
            request.ArrivalSourceType,
            referringUnit,
            referringDoctor,
            request.ClinicalUrgency,
            request.SpecialRequirements ?? string.Empty,
            request.Priority,
            request.UserId,
            occurredAt);

        if (plannedBed.BedId != "-")
        {
            waitingList = waitingList.ReservePlannedBed(plannedBed, request.UserId, occurredAt);
        }

        _waitingListRepo.Save(waitingList);

        return Task.FromResult(new WaitingListCreateResponse(waitingList.WaitingListId));
    }
}
