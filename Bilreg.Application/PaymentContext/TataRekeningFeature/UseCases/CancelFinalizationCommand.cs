using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record CancelFinalizationCommand(string RegId) : IRequest<CancelFinalizationResponse>, IRegKey;

public record CancelFinalizationResponse(TataRekeningSummaryDto Summary);

public class CancelFinalizationHandler : IRequestHandler<CancelFinalizationCommand, CancelFinalizationResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CancelFinalizationHandler(ITataRekeningRepo tataRekeningRepo, IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<CancelFinalizationResponse> Handle(CancelFinalizationCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.CancelFinalization();

        _tataRekeningRepo.SaveChanges(tataRekening);
        scope.Complete();

        return Task.FromResult(new CancelFinalizationResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
