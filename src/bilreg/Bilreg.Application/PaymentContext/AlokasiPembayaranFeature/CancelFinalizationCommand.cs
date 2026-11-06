using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;

public record CancelFinalizationCommand(
    string RegId,
    string UserId,
    string SupervisorUserId,
    string Reason) : IRequest<CancelFinalizationResponse>, IRegKey
{
    public CancelFinalizationCommand(string regId, string userId, string reason)
        : this(regId, userId, string.Empty, reason)
    {
    }
}

public record CancelFinalizationResponse(TataRekeningSummaryDto Summary);

public class CancelFinalizationHandler : IRequestHandler<CancelFinalizationCommand, CancelFinalizationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IRegRepo? _regRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITataRekeningCancellationLogRepo? _cancellationLogRepo;
    private readonly IDownstreamSettlementInspector? _settlementInspector;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegBiayaRepo? _regBiayaRepo;
    private readonly ITrsDirectDal? _trsDirectDal;
    private readonly IJurnalDal? _jurnalDal;

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IRegRepo regRepo,
        IAuditRepo auditRepo,
        ITataRekeningCancellationLogRepo cancellationLogRepo,
        IDownstreamSettlementInspector settlementInspector,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider,
        IRegBiayaRepo regBiayaRepo,
        ITrsDirectDal trsDirectDal,
        IJurnalDal jurnalDal)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _regRepo = regRepo;
        _auditRepo = auditRepo;
        _cancellationLogRepo = cancellationLogRepo;
        _settlementInspector = settlementInspector;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
        _regBiayaRepo = regBiayaRepo;
        _trsDirectDal = trsDirectDal;
        _jurnalDal = jurnalDal;
    }

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IRegRepo regRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
        : this(tataRekeningRepo, trsBillingRepo, regRepo, auditRepo, null!, null!, unitOfWork, currentUser, tglJamProvider,
            null!, null!, null!)
    {
    }

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IRegRepo regRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider,
        IRegBiayaRepo regBiayaRepo,
        ITrsDirectDal trsDirectDal,
        IJurnalDal jurnalDal)
        : this(tataRekeningRepo, trsBillingRepo, regRepo, auditRepo, null!, null!, unitOfWork, currentUser, tglJamProvider,
            regBiayaRepo, trsDirectDal, jurnalDal)
    {
    }

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IAuditRepo auditRepo,
        ITataRekeningCancellationLogRepo cancellationLogRepo,
        IDownstreamSettlementInspector settlementInspector,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
        : this(tataRekeningRepo, trsBillingRepo, null!, auditRepo, cancellationLogRepo, settlementInspector, unitOfWork, currentUser, tglJamProvider,
            null!, null!, null!)
    {
    }

    public Task<CancelFinalizationResponse> Handle(CancelFinalizationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        Guard.Against.NullOrWhiteSpace(request.Reason);

        if (_cancellationLogRepo is not null)
        {
            Guard.Against.NullOrWhiteSpace(request.SupervisorUserId);
            EnsureValidReason(request.Reason);
            EnsureSupervisorRole();
        }

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var statusBefore = tataRekening.Status.ToString();

        var adminTrsIds = _regBiayaRepo?.ListData(request)?
            .Where(x => !string.IsNullOrWhiteSpace(x.ReffBlId))
            .Select(x => x.ReffBlId)
            .Distinct()
            .ToList() ?? [];

        tataRekening.CancelFinalization(_settlementInspector, adminTrsIds);

        var statusAfter = tataRekening.Status.ToString();

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        if (_trsDirectDal is not null && _jurnalDal is not null)
        {
            var sentinelDate = new DateTime(3000, 1, 1, 0, 0, 0);
            foreach (var trsId in adminTrsIds)
            {
                _trsDirectDal.UpdateTimestamp(trsId, sentinelDate, 0m);
                _jurnalDal.UpdateTimestamp(trsId, sentinelDate);
            }
        }

        if (_regRepo is not null)
        {
            var reg = _regRepo.LoadEntity(request)
                .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");
            reg.CancelKeluar(request.UserId, _tglJamProvider.Now);
            _regRepo.SaveChanges(reg);
        }

        var cancelledAt = _tglJamProvider.Now;
        var clientIpAddress = _currentUser?.GetClientIpAddress() ?? "-";

        if (_cancellationLogRepo is not null && !string.IsNullOrWhiteSpace(request.SupervisorUserId))
        {
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
        }

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
