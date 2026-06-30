using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record FinancialAdjustmentCommand(
    string RegId,
    FinancialAdjustmentInputDto Adjustment,
    DateTime AppliedAt) : IRequest<FinancialAdjustmentResponse>, IRegKey;

public record FinancialAdjustmentResponse(
    TataRekeningSummaryDto Summary,
    bool RequiresReopen,
    FinancialAdjustmentTypeEnum AdjustmentType,
    string? TrsBillingId);

public class FinancialAdjustmentHandler : IRequestHandler<FinancialAdjustmentCommand, FinancialAdjustmentResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IFinancialAdjustmentDomainService _adjustmentService;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public FinancialAdjustmentHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IFinancialAdjustmentDomainService adjustmentService,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _adjustmentService = adjustmentService;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public Task<FinancialAdjustmentResponse> Handle(
        FinancialAdjustmentCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.Null(request.Adjustment);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var adjustmentRequest = BuildAdjustmentRequest(request.Adjustment);
        var result = _adjustmentService.Apply(tataRekening, adjustmentRequest, request.AppliedAt);

        if (!result.RequiresReopen)
        {
            _tataRekeningRepo.SaveChanges(tataRekening);
            PersistMutatedBills(tataRekening, adjustmentRequest);

            var audit = AuditLog.Create(
                userId: _currentUser.GetActorUserId(),
                actionType: "TATA_REKENING_FINANCIAL_ADJUSTMENT",
                entityName: nameof(TataRekeningModel),
                entityId: request.RegId,
                reason: request.Adjustment.Reason,
                originalDataJson: AuditLogSnapshotJson.Serialize(new
                {
                    request.Adjustment.Type,
                    request.Adjustment.Amount,
                    request.Adjustment.TrsBillingId
                }),
                correlationId: request.RegId);
            _auditRepo.SaveChanges(audit);
        }

        scope.Complete();

        return Task.FromResult(new FinancialAdjustmentResponse(
            TataRekeningApplicationMapper.ToSummaryDto(tataRekening),
            result.RequiresReopen,
            result.Type,
            result.TrsBillingId));
    }

    private FinancialAdjustmentRequest BuildAdjustmentRequest(FinancialAdjustmentInputDto input)
    {
        TrsBillType? manualChargeBill = null;
        if (input.Type == FinancialAdjustmentTypeEnum.ManualCharge)
        {
            if (string.IsNullOrWhiteSpace(input.ManualChargeTrsBillingId))
                throw new ArgumentException("Manual Charge memerlukan ManualChargeTrsBillingId.");

            var billKey = TrsBillType.Key(input.ManualChargeTrsBillingId);
            manualChargeBill = _trsBillingRepo.LoadEntity(billKey)
                .GetValueOrThrow($"TrsBill '{input.ManualChargeTrsBillingId}' tidak ditemukan.");
        }

        return new FinancialAdjustmentRequest(
            input.Type,
            input.Amount,
            input.Reason,
            input.TrsBillingId,
            TataRekeningApplicationMapper.ToSubsidyPayer(input),
            input.RequiresChargeSourceChange,
            manualChargeBill);
    }

    private void PersistMutatedBills(TataRekeningModel tataRekening, FinancialAdjustmentRequest adjustmentRequest)
    {
        if (adjustmentRequest.Type == FinancialAdjustmentTypeEnum.ManualCharge &&
            adjustmentRequest.ManualChargeBill is not null)
        {
            _trsBillingRepo.SaveChanges(adjustmentRequest.ManualChargeBill);
            return;
        }

        if (!string.IsNullOrWhiteSpace(adjustmentRequest.TrsBillingId))
        {
            var bill = tataRekening.ListTrsBill
                .FirstOrDefault(b => b.TrsBillingId == adjustmentRequest.TrsBillingId)
                ?? throw new KeyNotFoundException(
                    $"TrsBill '{adjustmentRequest.TrsBillingId}' tidak ditemukan pada Tata Rekening.");
            _trsBillingRepo.SaveChanges(bill);
        }
    }
}
