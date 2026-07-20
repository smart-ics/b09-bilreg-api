using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record FinalizeFinancialResponsibilityCommand(
    string RegId, string UserId,
    DateTime? FinalizationDate) : IRequest<FinalizeFinancialResponsibilityResponse>, IRegKey;

public record FinalizeFinancialResponsibilityResponse(TataRekeningSummaryDto Summary);

public class FinalizeFinancialResponsibilityHandler
    : IRequestHandler<FinalizeFinancialResponsibilityCommand, FinalizeFinancialResponsibilityResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;

    public FinalizeFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider? tglJamProvider = null)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
    }

    public Task<FinalizeFinancialResponsibilityResponse> Handle(
        FinalizeFinancialResponsibilityCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var petugasVerif = request.UserId; //_currentUser.GetActorUserId();
        Guard.Against.NullOrWhiteSpace(petugasVerif);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var occurredAt = request.FinalizationDate ?? _tglJamProvider.Now;
        tataRekening.FinalizeFinancialResponsibility(petugasVerif, occurredAt);

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        scope.Complete();

        return Task.FromResult(
            new FinalizeFinancialResponsibilityResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
