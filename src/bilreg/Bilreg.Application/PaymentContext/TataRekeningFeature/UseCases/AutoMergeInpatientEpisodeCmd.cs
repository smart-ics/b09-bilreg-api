using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record AutoMergeInpatientEpisodeCmd(
    string InpatientRegId,
    string PrecedingRegId,
    string? UserId = null) : IRequest<AutoMergeInpatientEpisodeResponse>;

public record AutoMergeInpatientEpisodeResponse(
    string InpatientRegId,
    string PrecedingRegId,
    string NoRM,
    bool Success,
    string Message);

public class AutoMergeInpatientEpisodeCmdHandler : IRequestHandler<AutoMergeInpatientEpisodeCmd, AutoMergeInpatientEpisodeResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IMergeBillingRepo _mergeBillingRepo;
    private readonly IMergeBillingDomainService _mergeBillingService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAuditRepo? _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public AutoMergeInpatientEpisodeCmdHandler(
        IRegRepo regRepo,
        ITataRekeningRepo tataRekeningRepo,
        IMergeBillingRepo mergeBillingRepo,
        IMergeBillingDomainService mergeBillingService,
        IUnitOfWork unitOfWork,
        ITglJamProvider tglJamProvider,
        IAuditRepo? auditRepo = null)
    {
        _regRepo = regRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _mergeBillingRepo = mergeBillingRepo;
        _mergeBillingService = mergeBillingService;
        _unitOfWork = unitOfWork;
        _tglJamProvider = tglJamProvider;
        _auditRepo = auditRepo;
    }

    public Task<AutoMergeInpatientEpisodeResponse> Handle(AutoMergeInpatientEpisodeCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.InpatientRegId, nameof(request.InpatientRegId));
        Guard.Against.NullOrWhiteSpace(request.PrecedingRegId, nameof(request.PrecedingRegId));

        var targetRegId = request.InpatientRegId.Trim();
        var sourceRegId = request.PrecedingRegId.Trim();

        if (string.Equals(targetRegId, sourceRegId, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException("Nomor registrasi rawat inap dan registrasi asal tidak boleh sama.");
        }

        using var scope = _unitOfWork.Begin();

        var targetKey = RegModel.Key(targetRegId);
        var sourceKey = RegModel.Key(sourceRegId);

        var targetReg = _regRepo.LoadEntity(targetKey)
            .GetValueOrThrow($"Registrasi rawat inap '{targetRegId}' tidak ditemukan.");

        var sourceReg = _regRepo.LoadEntity(sourceKey)
            .GetValueOrThrow($"Registrasi asal '{sourceRegId}' tidak ditemukan.");

        var targetTrk = _tataRekeningRepo.LoadEntity(targetKey).GetValueOrDefault();
        var sourceTrk = _tataRekeningRepo.LoadEntity(sourceKey).GetValueOrDefault();

        // 1. Patient identity integrity & status checks via Domain Service
        _mergeBillingService.ValidateAutoMergeIntegrity(sourceReg, targetReg, sourceTrk, targetTrk);

        // 2. Persist merge relation in ta_trs_merge
        _mergeBillingRepo.SaveMergeRelation(targetRegId, sourceRegId);

        // 3. Write Audit Trail if audit repo is present
        if (_auditRepo is not null)
        {
            var audit = AuditLog.Create(
                userId: string.IsNullOrWhiteSpace(request.UserId) ? "SYSTEM" : request.UserId,
                eventTime: _tglJamProvider.Now,
                actionType: "AUTO_MERGE_INPATIENT_EPISODE",
                entityName: "ta_trs_merge",
                entityId: $"{targetRegId}:{sourceRegId}",
                originalDataJson: AuditLogSnapshotJson.Serialize(new
                {
                    InpatientRegId = targetRegId,
                    PrecedingRegId = sourceRegId,
                    NoRM = targetReg.Pasien.PasienId
                }),
                correlationId: targetRegId);
            _auditRepo.SaveChanges(audit);
        }

        scope.Complete();

        return Task.FromResult(new AutoMergeInpatientEpisodeResponse(
            targetRegId,
            sourceRegId,
            targetReg.Pasien.PasienId,
            true,
            "Auto-merge registrasi ke episode rawat inap berhasil disimpan."));
    }
}
