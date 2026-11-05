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

public record CancelFinalizationCommand(
    string RegId,
    string UserId,
    string SupervisorUserId,
    string Reason) : IRequest<CancelFinalizationResponse>, IRegKey;

public record CancelFinalizationResponse(TataRekeningSummaryDto Summary);

public class CancelFinalizationHandler : IRequestHandler<CancelFinalizationCommand, CancelFinalizationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITataRekeningCancellationLogRepo _cancellationLogRepo;
    private readonly IDownstreamSettlementInspector _settlementInspector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IAuditRepo auditRepo,
        ITataRekeningCancellationLogRepo cancellationLogRepo,
        IDownstreamSettlementInspector settlementInspector,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _auditRepo = auditRepo;
        _cancellationLogRepo = cancellationLogRepo;
        _settlementInspector = settlementInspector;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
    }

    public Task<CancelFinalizationResponse> Handle(CancelFinalizationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.SupervisorUserId);
        Guard.Against.NullOrWhiteSpace(request.Reason);

        EnsureValidReason(request.Reason);
        EnsureSupervisorRole();

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var statusBefore = tataRekening.Status.ToString();

        tataRekening.CancelFinalization(_settlementInspector);

        var statusAfter = tataRekening.Status.ToString();

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        var cancelledAt = _tglJamProvider.Now;
        var clientIpAddress = _currentUser.GetClientIpAddress();

        var cancellationLog = TataRekeningCancellationLog.Create(
            regId: request.RegId,
            supervisorUserId: request.SupervisorUserId,
            userId: request.UserId,
            cancelledAt: cancelledAt,
            reason: request.Reason,
            statusBefore: statusBefore,
            statusAfter: statusAfter,
            clientIpAddress: clientIpAddress);
        _cancellationLogRepo.SaveChanges(cancellationLog);

        var audit = AuditLog.Create(
            userId: request.UserId,
            eventTime: cancelledAt,
            actionType: "TATA_REKENING_CANCEL_FINALIZATION",
            entityName: nameof(TataRekeningModel),
            entityId: request.RegId,
            reason: request.Reason,
            correlationId: request.RegId,
            clientIpAddress: clientIpAddress);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(new CancelFinalizationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }

    private static void EnsureValidReason(string reason)
    {
        if (reason.Trim().Length < TataRekeningCancellationLog.MinimumReasonLength)
            throw new ArgumentException(
                $"Alasan pembatalan finalisasi minimal {TataRekeningCancellationLog.MinimumReasonLength} karakter.",
                nameof(reason));
    }

    private void EnsureSupervisorRole()
    {
        if (!TataRekeningSupervisorRole.IsAnySupervisor(_currentUser.GetRoles()))
            throw new UnauthorizedAccessException(
                "Pembatalan finalisasi hanya dapat dilakukan oleh Supervisor Tata Rekening atau Admin Keuangan.");
    }
}
