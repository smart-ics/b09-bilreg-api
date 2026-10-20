using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
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

public record CancelFinalizationCommand(string RegId, string UserId, string Reason) : IRequest<CancelFinalizationResponse>, IRegKey;

public record CancelFinalizationResponse(TataRekeningSummaryDto Summary);

public class CancelFinalizationHandler : IRequestHandler<CancelFinalizationCommand, CancelFinalizationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IRegRepo _regRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IRegRepo regRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _regRepo = regRepo;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
    }

    public Task<CancelFinalizationResponse> Handle(CancelFinalizationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.Reason);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.CancelFinalization();

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");
        reg.CancelKeluar(request.UserId, _tglJamProvider.Now);
        _regRepo.SaveChanges(reg);

        var audit = AuditLog.Create(
            userId: request.UserId,
            eventTime: _tglJamProvider.Now,
            actionType: "TATA_REKENING_CANCEL_FINALIZATION",
            entityName: nameof(TataRekeningModel),
            entityId: request.RegId,
            reason: request.Reason,
            correlationId: request.RegId);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(new CancelFinalizationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
