using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public enum FinancialVerificationAction
{
    Verify,
    RequireAdjustment
}

public record FinancialVerificationCommand(
    string RegId,
    FinancialVerificationAction Action,
    DateTime VerifiedAt, string UserId) : IRequest<FinancialVerificationResponse>, IRegKey;

public record FinancialVerificationResponse(TataRekeningSummaryDto Summary);

public class FinancialVerificationHandler : IRequestHandler<FinancialVerificationCommand, FinancialVerificationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IMergeRequestRepo _mergeRequestRepo;
    private readonly IFinancialVerificationDomainService _verificationService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;

    public FinancialVerificationHandler(
        ITataRekeningRepo tataRekeningRepo,
        IMergeRequestRepo mergeRequestRepo,
        IFinancialVerificationDomainService verificationService,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _mergeRequestRepo = mergeRequestRepo;
        _verificationService = verificationService;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
    }

    public Task<FinancialVerificationResponse> Handle(
        FinancialVerificationCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        switch (request.Action)
        {
            case FinancialVerificationAction.Verify:
                var petugasVerif = request.UserId; // _currentUser.GetActorUserId();
                Guard.Against.NullOrWhiteSpace(petugasVerif);
                var pendingMerges = _mergeRequestRepo.ListPendingByReg(request).ToList();
                _verificationService.Verify(
                    tataRekening,
                    petugasVerif,
                    request.VerifiedAt,
                    pendingMerges);
                break;
            case FinancialVerificationAction.RequireAdjustment:
                _verificationService.RequireAdjustment(tataRekening);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(request.Action), request.Action, null);
        }

        _tataRekeningRepo.SaveChanges(tataRekening);
        scope.Complete();

        return Task.FromResult(
            new FinancialVerificationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
