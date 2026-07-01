using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

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
    private readonly IUnitOfWork _unitOfWork;

    public AllocateFinancialResponsibilityHandler(
        ITataRekeningRepo tataRekeningRepo,
        ITrsBillingRepo trsBillingRepo,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _trsBillingRepo = trsBillingRepo;
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
