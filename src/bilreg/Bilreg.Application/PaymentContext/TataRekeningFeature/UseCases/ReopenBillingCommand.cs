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

public record ReopenBillingCommand(string RegId, string Reason, string UserId) : IRequest<ReopenBillingResponse>, IRegKey;

public record ReopenBillingResponse(TataRekeningSummaryDto Summary);

public class ReopenBillingHandler : IRequestHandler<ReopenBillingCommand, ReopenBillingResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;

    public ReopenBillingHandler(
        ITataRekeningRepo tataRekeningRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
    }

    public Task<ReopenBillingResponse> Handle(ReopenBillingCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.Reason);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.ReOpen();

        _tataRekeningRepo.SaveChanges(tataRekening);

        var audit = AuditLog.Create(
            userId: request.UserId,
            eventTime: _tglJamProvider.Now,
            actionType: "TATA_REKENING_REOPEN_BILLING",
            entityName: nameof(TataRekeningModel),
            entityId: request.RegId,
            reason: request.Reason,
            correlationId: request.RegId);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(new ReopenBillingResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
