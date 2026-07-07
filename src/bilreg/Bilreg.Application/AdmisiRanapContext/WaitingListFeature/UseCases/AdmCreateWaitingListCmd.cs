using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.Shared;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.WaitingListFeature.UseCases;

public record AdmCreateWaitingListCmd(
    string RegId,
    string KelasId,
    string BangsalId,
    int Priority,
    string UserId) : IRequest<AdmCreateWaitingListResponse>, IRegKey;

public record AdmCreateWaitingListResponse(string WaitingListId);

public class AdmCreateWaitingListHandler : IRequestHandler<AdmCreateWaitingListCmd, AdmCreateWaitingListResponse>
{
    private readonly IWaitingListRepo _waitingListRepo;
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IBangsalRepo _bangsalRepo;

    public AdmCreateWaitingListHandler(
        IWaitingListRepo waitingListRepo,
        IAdmissionRepo admissionRepo,
        IKelasRepo kelasRepo,
        IBangsalRepo bangsalRepo)
    {
        _waitingListRepo = waitingListRepo;
        _admissionRepo = admissionRepo;
        _kelasRepo = kelasRepo;
        _bangsalRepo = bangsalRepo;
    }

    public Task<AdmCreateWaitingListResponse> Handle(
        AdmCreateWaitingListCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var admission = AdmisiRanapSupport.LoadAdmission(_admissionRepo, request);

        if (_waitingListRepo.HasActiveByRegId(request.RegId))
            throw new InvalidOperationException(
                $"Admission '{request.RegId}' sudah memiliki Waiting List aktif.");

        var kelas = AdmisiRanapSupport.LoadKelasReff(_kelasRepo, request.KelasId);
        var bangsal = AdmisiRanapSupport.LoadBangsalReff(_bangsalRepo, request.BangsalId);

        var waitingList = WaitingListModel.Create(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien,
            kelas,
            bangsal,
            request.Priority,
            request.UserId);

        _waitingListRepo.SaveChanges(waitingList);

        return Task.FromResult(new AdmCreateWaitingListResponse(waitingList.WaitingListId));
    }
}
