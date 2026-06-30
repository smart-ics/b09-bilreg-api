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

public record CancelFinalizationCommand(string RegId) : IRequest<CancelFinalizationResponse>, IRegKey;

public record CancelFinalizationResponse(TataRekeningSummaryDto Summary);

public class CancelFinalizationHandler : IRequestHandler<CancelFinalizationCommand, CancelFinalizationResponse>
{
    private const string SystemActor = "SYSTEM";

    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CancelFinalizationHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<CancelFinalizationResponse> Handle(CancelFinalizationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.CancelFinalization();

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        var audit = AuditLog.Create(
            userId: SystemActor,
            actionType: "TATA_REKENING_CANCEL_FINALIZATION",
            entityName: nameof(TataRekeningModel),
            entityId: request.RegId,
            correlationId: request.RegId);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(new CancelFinalizationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
