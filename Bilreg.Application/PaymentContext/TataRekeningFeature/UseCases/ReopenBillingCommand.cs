using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record ReopenBillingCommand(string RegId, string Reason) : IRequest<ReopenBillingResponse>, IRegKey;

public record ReopenBillingResponse(TataRekeningSummaryDto Summary);

public class ReopenBillingHandler : IRequestHandler<ReopenBillingCommand, ReopenBillingResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IUnitOfWork _unitOfWork;

    public ReopenBillingHandler(ITataRekeningRepo tataRekeningRepo, IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<ReopenBillingResponse> Handle(ReopenBillingCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.Reason);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.ReOpen();

        _tataRekeningRepo.SaveChanges(tataRekening);
        scope.Complete();

        return Task.FromResult(new ReopenBillingResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
