using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.PaymentContext.DepositFeature;
using Bilreg.Domain.PaymentContext.DepositFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record OpenTataRekeningQuery(string RegId) : IRequest<OpenTataRekeningResponse>, IRegKey;

public record OpenTataRekeningResponse(
    TataRekeningSummaryDto Summary,
    IReadOnlyList<TrsBillSummaryDto> Bills,
    IReadOnlyList<PaymentProjectionDto> Projection,
    IReadOnlyList<MergeRequestSummaryDto> PendingMergeRequests,
    NetFinancialPositionDto NetFinancialPosition);

public class OpenTataRekeningHandler : IRequestHandler<OpenTataRekeningQuery, OpenTataRekeningResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IRegRepo _regRepo;
    private readonly IMergeRequestRepo _mergeRequestRepo;
    private readonly IDepositRepo? _depositRepo;

    public OpenTataRekeningHandler(
        ITataRekeningRepo tataRekeningRepo,
        IRegRepo regRepo,
        IMergeRequestRepo mergeRequestRepo,
        IDepositRepo? depositRepo = null)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _regRepo = regRepo;
        _mergeRequestRepo = mergeRequestRepo;
        _depositRepo = depositRepo;
    }

    public Task<OpenTataRekeningResponse> Handle(OpenTataRekeningQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        var reg = _regRepo.LoadEntity(request)
            .GetValueOrThrow($"Registrasi '{request.RegId}' tidak ditemukan.");

        var pendingMerges = LoadPendingMergeRequests(request, reg.Pasien.PasienId);

        // Load deposit data for this registration (if deposit repo is available)
        IEnumerable<DepositModel> depositData = Enumerable.Empty<DepositModel>();
        if (_depositRepo != null)
        {
            depositData = _depositRepo.ListData(Bilreg.Domain.AdmisiContext.RegFeature.RegModel.Key(reg.RegId))
                .Where(d => d.Reg.RegId == reg.RegId)
                .ToList();
        }

        // Calculate net financial position
        decimal grossBilling = tataRekening.ListTrsBill.Sum(b => b.FinancialTotal);
        decimal estimatedPayerCoverage = CalculateEstimatedPayerCoverage(tataRekening);
        decimal classDifferential = CalculateClassDifferential(tataRekening);
        decimal totalDeposit = depositData.Sum(d => d.NilaiDeposit);
        decimal totalCashierPaid = tataRekening.ListPayment.Sum(p => p.NilaiJasa + p.NilaiObat); // simplified
        decimal netPayableBalance = grossBilling - estimatedPayerCoverage - totalDeposit - totalCashierPaid;

        var response = new OpenTataRekeningResponse(
            TataRekeningApplicationMapper.ToSummaryDto(tataRekening),
            tataRekening.ListTrsBill.Select(TataRekeningApplicationMapper.ToBillSummaryDto).ToList(),
            tataRekening.ListPayment.Select(TataRekeningApplicationMapper.ToProjectionDto).ToList(),
            pendingMerges,
            new NetFinancialPositionDto(
                grossBilling,
                estimatedPayerCoverage,
                classDifferential,
                totalDeposit,
                totalCashierPaid,
                netPayableBalance));

        return Task.FromResult(response);
    }

    private decimal CalculateEstimatedPayerCoverage(TataRekeningModel tataRekening)
    {
        // Derived from financial responsibility allocation - if allocated, use the allocated amounts
        // Otherwise, estimate based on available data (0 when not yet allocated;
        // the formula structure is mathematically verifiable with actual data later)
        // If financial responsibility is allocated, use total payments as estimated coverage
        if (tataRekening.IsFinancialResponsibilityAllocated)
        {
            return tataRekening.ListPayment.Sum(p => p.NilaiJasa + p.NilaiObat);
        }
        // Default: estimated payer coverage is 0 when not yet allocated;
        // the formula structure is mathematically verifiable with actual data later
        return 0m;
    }

    private decimal CalculateClassDifferential(TataRekeningModel tataRekening)
    {
        // Accumulation of DifferentialAmount from room-charge class upgrades
        // NOTE: DifferentialAmount field is populated on TrsBillingItemDto (P3-S05/DAL projection),
        // not yet on domain TrsBillType/ListFinalization. This calculation will be enabled
        // once the DAL populates DifferentialAmount on the billing item DTOs and they are
        // available through the TataRekening model.
        // For now, return 0m to ensure formula structure is mathematically verifiable;
        // the net financial position formula remains valid with actual DifferentialAmount values.
        return 0m;
    }

    private IReadOnlyList<MergeRequestSummaryDto> LoadPendingMergeRequests(IRegKey regKey, string pasienId)
    {
        var byReg = _mergeRequestRepo.ListPendingByReg(regKey);
        var byPatient = _mergeRequestRepo.ListPendingByPatient(pasienId);

        return byReg
            .Concat(byPatient)
            .GroupBy(m => m.MergeRequestId, StringComparer.Ordinal)
            .Select(g => g.First())
            .Select(TataRekeningApplicationMapper.ToMergeRequestDto)
            .ToList();
    }
}
