using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

/// <summary>
/// SOP-TR-03 — Command for merging billing items from a source registration into a target registration.
/// Enforces valid admission relationship via IRegRelationPolicy and records the relation permanently on BILRG_MergeRequest.
/// </summary>
public record MergeBillingCommand(string MergeRequestId, string UserId) : IRequest<MergeBillingResponse>, IMergeRequestKey;

public record MergeBillingResponse(
    MergeRequestSummaryDto MergeRequest,
    TataRekeningSummaryDto SourceSummary,
    TataRekeningSummaryDto TargetSummary);

/// <summary>
/// Handler for SOP-TR-03 Merge Billing.
/// Coordinates validation via IMergeBillingDomainService (enforcing IRegRelationPolicy),
/// transfers bills and receivables, records permanent merge relation in MergeRequestModel, and writes audit trail.
/// </summary>
public class MergeBillingHandler : IRequestHandler<MergeBillingCommand, MergeBillingResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IMergeRequestRepo _mergeRequestRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IMergeBillingDomainService _mergeBillingService;
    private readonly ITransferReceivableService _transferReceivableService;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;

    public MergeBillingHandler(
        ITataRekeningRepo tataRekeningRepo,
        IMergeRequestRepo mergeRequestRepo,
        ITrsBillingRepo trsBillingRepo,
        IMergeBillingDomainService mergeBillingService,
        ITransferReceivableService transferReceivableService,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _mergeRequestRepo = mergeRequestRepo;
        _trsBillingRepo = trsBillingRepo;
        _mergeBillingService = mergeBillingService;
        _transferReceivableService = transferReceivableService;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
    }

    public Task<MergeBillingResponse> Handle(MergeBillingCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.MergeRequestId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        using var scope = _unitOfWork.Begin();

        var mergeRequest = _mergeRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Merge Request '{request.MergeRequestId}' tidak ditemukan.");

        var sourceKey = RegModel.Key(mergeRequest.SourceRegId);
        var targetKey = RegModel.Key(mergeRequest.TargetRegId
            ?? throw new InvalidOperationException("Merge Request memerlukan Target Registrasi."));

        var source = _tataRekeningRepo.LoadEntity(sourceKey)
            .GetValueOrThrow($"Tata Rekening sumber '{mergeRequest.SourceRegId}' tidak ditemukan.");

        var target = _tataRekeningRepo.LoadEntity(targetKey)
            .GetValueOrThrow($"Tata Rekening tujuan '{mergeRequest.TargetRegId}' tidak ditemukan.");

        var sourceBillIds = source.ListTrsBill.Select(b => b.TrsBillingId).ToHashSet(StringComparer.Ordinal);

        _mergeBillingService.Execute(mergeRequest, source, target);

        _tataRekeningRepo.SaveChanges(source);
        _tataRekeningRepo.SaveChanges(target);
        _mergeRequestRepo.SaveChanges(mergeRequest);

        foreach (var bill in target.ListTrsBill.Where(b => sourceBillIds.Contains(b.TrsBillingId)))
            _trsBillingRepo.SaveChanges(bill);

        _transferReceivableService.Transfer(mergeRequest.SourceRegId, mergeRequest.TargetRegId!);

        var audit = AuditLog.Create(
            userId: request.UserId,
            eventTime: _tglJamProvider.Now,
            actionType: "TATA_REKENING_MERGE_BILLING",
            entityName: nameof(MergeRequestModel),
            entityId: mergeRequest.MergeRequestId,
            originalDataJson: AuditLogSnapshotJson.Serialize(new
            {
                mergeRequest.MergeRequestId,
                mergeRequest.SourceRegId,
                mergeRequest.TargetRegId
            }),
            correlationId: mergeRequest.TargetRegId);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(new MergeBillingResponse(
            TataRekeningApplicationMapper.ToMergeRequestDto(mergeRequest),
            TataRekeningApplicationMapper.ToSummaryDto(source),
            TataRekeningApplicationMapper.ToSummaryDto(target)));
    }
}
