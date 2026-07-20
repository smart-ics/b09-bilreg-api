using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.WaitingListFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.ValidationHelper;

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
    private readonly IWardAccommodationGateway _wardGateway;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AdmCreateWaitingListHandler(
        IWaitingListRepo waitingListRepo,
        IAdmissionRepo admissionRepo,
        IWardAccommodationGateway wardGateway,
        IAuditRepo auditRepo,
        ITglJamProvider? tglJamProvider = null)
    {
        _waitingListRepo = waitingListRepo;
        _admissionRepo = admissionRepo;
        _wardGateway = wardGateway;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<AdmCreateWaitingListResponse> Handle(
        AdmCreateWaitingListCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.KelasId);
        Guard.Against.NullOrWhiteSpace(request.BangsalId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");

        if (_waitingListRepo.HasActiveByRegId(request.RegId))
            throw new InvalidOperationException(
                $"Admission '{request.RegId}' sudah memiliki Waiting List aktif.");

        var kelas = _wardGateway.ResolveKelas(request.KelasId);
        var bangsal = _wardGateway.ResolveBangsal(request.BangsalId);

        var occurredAt = _tglJamProvider.Now;
        var waitingList = WaitingListModel.Create(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien,
            kelas,
            bangsal,
            request.Priority,
            request.UserId,
            occurredAt);

        _waitingListRepo.SaveChanges(waitingList);

        _auditRepo.SaveChanges(AuditLog.Create(
            waitingList.AuditTrail.Created,
            "CREATE",
            nameof(WaitingListModel),
            waitingList.WaitingListId));

        _wardGateway.NotifyHandOver(new WardAccommodationHandOver(
            waitingList.WaitingListId,
            waitingList.RegId,
            waitingList.Bangsal.BangsalId,
            waitingList.KelasRawat.KelasId,
            waitingList.WaitingListStatus));

        return Task.FromResult(new AdmCreateWaitingListResponse(waitingList.WaitingListId));
    }
}
