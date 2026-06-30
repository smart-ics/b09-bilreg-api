using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record SettlementInitiationCommand(
    string RegId,
    string PetugasVerif,
    DateTime InitiatedAt) : IRequest<SettlementInitiationResponse>, IRegKey;

public record SettlementInitiationResponse(TataRekeningSummaryDto Summary);

public class SettlementInitiationHandler : IRequestHandler<SettlementInitiationCommand, SettlementInitiationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly IUnitOfWork _unitOfWork;

    public SettlementInitiationHandler(
        ITataRekeningRepo tataRekeningRepo,
        IAuditRepo auditRepo,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _auditRepo = auditRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<SettlementInitiationResponse> Handle(
        SettlementInitiationCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.PetugasVerif);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.InitiateSettlement(request.PetugasVerif, request.InitiatedAt);

        _tataRekeningRepo.SaveChanges(tataRekening);

        var audit = AuditLog.Create(
            userId: request.PetugasVerif,
            actionType: "TATA_REKENING_SETTLEMENT_INITIATION",
            entityName: nameof(TataRekeningModel),
            entityId: request.RegId,
            originalDataJson: AuditLogSnapshotJson.Serialize(new
            {
                request.RegId,
                request.PetugasVerif,
                request.InitiatedAt
            }),
            correlationId: request.RegId);
        _auditRepo.SaveChanges(audit);

        scope.Complete();

        return Task.FromResult(
            new SettlementInitiationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
