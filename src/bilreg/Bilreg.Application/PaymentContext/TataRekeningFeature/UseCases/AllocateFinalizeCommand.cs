using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.RegOutFeature.RegOutAgg;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record AllocateFinalizeCommand(
    string RegId,
    IReadOnlyList<PaymentAllocationInputDto> Payments,
    string UserId,
    DateTime? FinalizationDate = null) : IRequest<AllocateFinalizeResponse>, IRegKey;

public record AllocateFinalizeResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<PaymentProjectionDto> Projection);

public class AllocateFinalizeHandler
    : IRequestHandler<AllocateFinalizeCommand, AllocateFinalizeResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IDepositRepo _depositRepo;
    private readonly IRegJaminanPolicy _regJaminanPolicy;
    private readonly IRegRepo _regRepo;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IRegBiayaRepo _regBiayaRepo;
    private readonly ITrsDirectDal _trsDirectDal;
    private readonly IJurnalDal _jurnalDal;

    public AllocateFinalizeHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IDepositRepo depositRepo,
        IRegJaminanPolicy regJaminanPolicy,
        IRegRepo regRepo,
        IUnitOfWork unitOfWork,
        ITglJamProvider tglJamProvider,
        IRegBiayaRepo regBiayaRepo,
        ITrsDirectDal trsDirectDal,
        IJurnalDal jurnalDal)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _depositRepo = depositRepo;
        _regJaminanPolicy = regJaminanPolicy;
        _regRepo = regRepo;
        _unitOfWork = unitOfWork;
        _tglJamProvider = tglJamProvider;
        _regBiayaRepo = regBiayaRepo;
        _trsDirectDal = trsDirectDal;
        _jurnalDal = jurnalDal;
    }

    public AllocateFinalizeHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IDepositRepo depositRepo,
        IRegJaminanPolicy regJaminanPolicy,
        IRegRepo regRepo,
        IUnitOfWork unitOfWork,
        ITglJamProvider tglJamProvider)
        : this(tataRekeningRepo, trsBillingRepo, depositRepo, regJaminanPolicy, regRepo, unitOfWork, tglJamProvider,
            null!, null!, null!)
    {
    }

    public Task<AllocateFinalizeResponse> Handle(
        AllocateFinalizeCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.Null(request.Payments);
        var petugasVerif = !string.IsNullOrWhiteSpace(request.UserId) ? request.UserId : "-";

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        if (tataRekening.Status != TataRekeningStatusEnum.Closed)
            throw new InvalidOperationException("Operasi ini hanya diizinkan saat TataRekening berstatus CLOSED.");

        foreach (var payment in request.Payments)
        {
            if (payment.NilaiJasa < 0 || payment.NilaiObat < 0)
            {
                if (payment.PaymentId != PaymentType.ByKas.PaymentId)
                    throw new InvalidOperationException(
                        $"Nilai alokasi negatif hanya diizinkan untuk tipe pembayaran Kasir (BYKAS), ditemukan pada '{payment.PaymentName}' ({payment.PaymentId}).");
            }

            if (payment.IsTipeJaminan)
            {
                if (!_regJaminanPolicy.IsJaminanValidForReg(request.RegId, payment.PaymentId))
                    throw new InvalidOperationException($"Penjamin '{payment.PaymentName}' ({payment.PaymentId}) tidak terdaftar pada registrasi '{request.RegId}'.");
            }
        }

        var hasDepositPayment = request.Payments.Any(x =>
            x.PaymentId == PaymentType.ByDpu.PaymentId || x.PaymentId == PaymentType.ByDpk.PaymentId);
        var totalDepositAllocated = request.Payments
            .Where(x => x.PaymentId == PaymentType.ByDpu.PaymentId || x.PaymentId == PaymentType.ByDpk.PaymentId)
            .Sum(x => x.NilaiJasa + x.NilaiObat);

        if (hasDepositPayment || totalDepositAllocated > 0)
        {
            var saldo = _depositRepo.GetSaldo(request);
            var saldoAktifDeposit = saldo?.SaldoAktif ?? 0m;

            if (totalDepositAllocated > saldoAktifDeposit)
            {
                throw new InvalidOperationException(
                    $"Alokasi deposit ({totalDepositAllocated:N0}) melebihi saldo deposit aktif yang tersedia ({saldoAktifDeposit:N0}).");
            }

            var totalTagihan = tataRekening.ListTrsBill.Sum(b => b.FinancialTotal);
            if (totalDepositAllocated > totalTagihan)
            {
                throw new InvalidOperationException(
                    $"Alokasi deposit ({totalDepositAllocated:N0}) melebihi total tagihan kunjungan ({totalTagihan:N0}).");
            }
        }

        var listPayment = request.Payments
            .Select(TataRekeningApplicationMapper.ToPaymentType)
            .ToList();

        // 1. Allocate financial responsibility
        tataRekening.AllocateFinancialResponsibility(listPayment);

        var occurredAt = request.FinalizationDate ?? _tglJamProvider.Now;

        // Auto-complete financial verification if not verified
        if (tataRekening.Status == TataRekeningStatusEnum.Closed &&
            tataRekening.FinancialVerificationStatus == FinancialVerificationStatusEnum.NotVerified)
        {
            tataRekening.CompleteFinancialVerification(petugasVerif, occurredAt);
        }

        // 2. Finalize financial responsibility
        var adminTrsIds = _regBiayaRepo?.ListData(request)?
            .Where(x => !string.IsNullOrWhiteSpace(x.ReffBlId))
            .Select(x => x.ReffBlId)
            .Distinct()
            .ToList() ?? [];

        tataRekening.FinalizeFinancialResponsibility(petugasVerif, occurredAt, adminTrsIds);

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        if (_trsDirectDal is not null && _jurnalDal is not null)
        {
            foreach (var trsId in adminTrsIds)
            {
                _trsDirectDal.UpdateTimestamp(trsId, occurredAt, 5m);
                _jurnalDal.UpdateTimestamp(trsId, occurredAt);
            }
        }

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");
        reg.SetKeluar(petugasVerif, occurredAt);
        _regRepo.SaveChanges(reg);

        scope.Complete();

        return Task.FromResult(new AllocateFinalizeResponse(
            TataRekeningApplicationMapper.ToSummaryDto(tataRekening),
            tataRekening.ListPayment.Select(TataRekeningApplicationMapper.ToProjectionDto).ToList()));
    }
}
