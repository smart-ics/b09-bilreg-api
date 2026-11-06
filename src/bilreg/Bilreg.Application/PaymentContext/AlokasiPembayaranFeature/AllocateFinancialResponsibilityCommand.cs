using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.AlokasiPembayaranFeature;

public record AllocateFinancialResponsibilityCommand(
    string RegId,
    IReadOnlyList<PaymentAllocationInputDto> Payments) : IRequest<AllocateFinancialResponsibilityResponse>, IRegKey;

public record AllocateFinancialResponsibilityResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<PaymentProjectionDto> Projection);

public class AllocateFinancialResponsibilityHandler
    : IRequestHandler<AllocateFinancialResponsibilityCommand, AllocateFinancialResponsibilityResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IDepositRepo _depositRepo;
    private readonly IRegJaminanPolicy _regJaminanPolicy;
    private readonly IUnitOfWork _unitOfWork;

    public AllocateFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IDepositRepo depositRepo,
        IRegJaminanPolicy regJaminanPolicy,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
        _depositRepo = depositRepo;
        _regJaminanPolicy = regJaminanPolicy;
        _unitOfWork = unitOfWork;
    }

    public Task<AllocateFinancialResponsibilityResponse> Handle(
        AllocateFinancialResponsibilityCommand request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.Null(request.Payments);

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

        tataRekening.AllocateFinancialResponsibility(listPayment);

        _tataRekeningRepo.SaveChanges(tataRekening);
        foreach (var bill in tataRekening.ListTrsBill)
            _trsBillingRepo.SaveChanges(bill);

        scope.Complete();

        return Task.FromResult(new AllocateFinancialResponsibilityResponse(
            TataRekeningApplicationMapper.ToSummaryDto(tataRekening),
            tataRekening.ListPayment.Select(TataRekeningApplicationMapper.ToProjectionDto).ToList()));
    }
}
