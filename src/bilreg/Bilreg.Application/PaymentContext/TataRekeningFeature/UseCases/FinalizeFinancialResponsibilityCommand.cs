using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record FinalizeFinancialResponsibilityCommand(
    string RegId,
    string PetugasVerif,
    DateTime FinalizationDate) : IRequest<FinalizeFinancialResponsibilityResponse>, IRegKey;

public record FinalizeFinancialResponsibilityResponse(TataRekeningSummaryDto Summary);

public class FinalizeFinancialResponsibilityHandler
    : IRequestHandler<FinalizeFinancialResponsibilityCommand, FinalizeFinancialResponsibilityResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IUnitOfWork _unitOfWork;

    public FinalizeFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<FinalizeFinancialResponsibilityResponse> Handle(
        FinalizeFinancialResponsibilityCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.PetugasVerif);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.FinalizeFinancialResponsibility(request.PetugasVerif, request.FinalizationDate);

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        scope.Complete();

        return Task.FromResult(
            new FinalizeFinancialResponsibilityResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
