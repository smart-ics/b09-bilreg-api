using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.Shared;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;

public record FinalizeFinancialResponsibilityCommand(
    string RegId, string UserId,
    DateTime? FinalizationDate) : IRequest<FinalizeFinancialResponsibilityResponse>, IRegKey;

public record FinalizeFinancialResponsibilityResponse(TataRekeningSummaryDto Summary);

public class FinalizeFinancialResponsibilityHandler
    : IRequestHandler<FinalizeFinancialResponsibilityCommand, FinalizeFinancialResponsibilityResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IRegRepo? _regRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICurrentUserContext _currentUser;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IFinancialVerificationDomainService? _financialVerificationService;

    public FinalizeFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IRegRepo regRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider,
        IFinancialVerificationDomainService? financialVerificationService = null)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _regRepo = regRepo;
        _unitOfWork = unitOfWork;
        _currentUser = currentUser;
        _tglJamProvider = tglJamProvider;
        _financialVerificationService = financialVerificationService;
    }

    public FinalizeFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IUnitOfWork unitOfWork,
        ICurrentUserContext currentUser,
        ITglJamProvider tglJamProvider,
        IFinancialVerificationDomainService financialVerificationService)
        : this(tataRekeningRepo, trsBillingRepo, null!, unitOfWork, currentUser, tglJamProvider, financialVerificationService)
    {
    }

    public Task<FinalizeFinancialResponsibilityResponse> Handle(
        FinalizeFinancialResponsibilityCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.UserId);

        var petugasVerif = request.UserId;
        Guard.Against.NullOrWhiteSpace(petugasVerif);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        // Hard Blocking Guard: seluruh hari rawat inap wajib sudah dibebankan sewa kamar
        // (Room Charge) sebelum tagihan episode dapat difinalisasi.
        if (_financialVerificationService is not null)
        {
            var completeness = _financialVerificationService.ValidateRoomChargeCompleteness(request.RegId);
            if (!completeness.IsComplete)
                throw new BusinessValidationException(
                    "Tagihan episode rawat inap tidak dapat difinalisasi. Terdapat hari rawat inap yang belum dibebankan sewa kamar (Room Charge). Pastikan pembebanan harian telah diproses sampai dengan pelepasan bed riil.");
        }

        var occurredAt = request.FinalizationDate ?? _tglJamProvider.Now;

        if (tataRekening.Status == TataRekeningStatusEnum.Closed &&
            tataRekening.FinancialVerificationStatus == FinancialVerificationStatusEnum.NotVerified)
        {
            tataRekening.CompleteFinancialVerification(petugasVerif, occurredAt);
        }

        tataRekening.FinalizeFinancialResponsibility(petugasVerif, occurredAt);

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        if (_regRepo is not null)
        {
            var reg = _regRepo.LoadEntity(request)
                .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");
            reg.SetKeluar(petugasVerif, occurredAt);
            _regRepo.SaveChanges(reg);
        }

        scope.Complete();

        return Task.FromResult(
            new FinalizeFinancialResponsibilityResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
