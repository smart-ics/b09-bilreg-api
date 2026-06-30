using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record MergeBillingCommand(string MergeRequestId) : IRequest<MergeBillingResponse>, IMergeRequestKey;

public record MergeBillingResponse(
    MergeRequestSummaryDto MergeRequest,
    TataRekeningSummaryDto SourceSummary,
    TataRekeningSummaryDto TargetSummary);

public class MergeBillingHandler : IRequestHandler<MergeBillingCommand, MergeBillingResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IMergeRequestRepo _mergeRequestRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    private readonly IMergeBillingDomainService _mergeBillingService;
    private readonly IUnitOfWork _unitOfWork;

    public MergeBillingHandler(
        ITataRekeningRepo tataRekeningRepo,
        IMergeRequestRepo mergeRequestRepo,
        ITrsBillingRepo trsBillingRepo,
        IMergeBillingDomainService mergeBillingService,
        IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _mergeRequestRepo = mergeRequestRepo;
        _trsBillingRepo = trsBillingRepo;
        _mergeBillingService = mergeBillingService;
        _unitOfWork = unitOfWork;
    }

    public Task<MergeBillingResponse> Handle(MergeBillingCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.MergeRequestId);

        using var scope = _unitOfWork.Begin();

        var mergeRequest = _mergeRequestRepo.LoadEntity(request)
            .GetValueOrThrow($"Merge Request '{request.MergeRequestId}' tidak ditemukan.");

        var sourceKey = RegModel.Key(mergeRequest.SourceRegId);
        var targetKey = RegModel.Key(mergeRequest.TargetRegId
            ?? throw new InvalidOperationException("Merge Request memerlukan Target Registrasi."));

        var source = _tataRekeningRepo.LoadEntity(sourceKey)
            .GetValueOrThrow($"Tata Rekening sumber '{mergeRequest.SourceRegId}' tidak ditemukan.");

        var target = _tataRekeningRepo.LoadEntity(targetKey)
            .GetValueOrThrow($"Tata Rekening tujuan '{mergeRequest.TargetRegId}' tidak ditemukan.");

        var sourceBillIds = source.ListTrsBill.Select(b => b.TrsBillingId).ToHashSet(StringComparer.Ordinal);

        _mergeBillingService.Execute(mergeRequest, source, target);

        _tataRekeningRepo.SaveChanges(source);
        _tataRekeningRepo.SaveChanges(target);
        _mergeRequestRepo.SaveChanges(mergeRequest);

        foreach (var bill in target.ListTrsBill.Where(b => sourceBillIds.Contains(b.TrsBillingId)))
            _trsBillingRepo.SaveChanges(bill);

        scope.Complete();

        return Task.FromResult(new MergeBillingResponse(
            TataRekeningApplicationMapper.ToMergeRequestDto(mergeRequest),
            TataRekeningApplicationMapper.ToSummaryDto(source),
            TataRekeningApplicationMapper.ToSummaryDto(target)));
    }
}
