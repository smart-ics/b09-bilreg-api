using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
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
    private readonly IUnitOfWork _unitOfWork;

    public SettlementInitiationHandler(ITataRekeningRepo tataRekeningRepo, IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
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
        scope.Complete();

        return Task.FromResult(
            new SettlementInitiationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
