using Ardalis.GuardClauses;
using Bilreg.Application.PaymentContext.TataRekeningFeature.Dtos;
using Bilreg.Application.Shared;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PaymentContext.TataRekeningFeature.UseCases;

public record CloseBillCommand(string RegId) : IRequest<CloseBillResponse>, IRegKey;

public record CloseBillResponse(TataRekeningSummaryDto Summary);

public class CloseBillHandler : IRequestHandler<CloseBillCommand, CloseBillResponse>
{
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IUnitOfWork _unitOfWork;

    public CloseBillHandler(ITataRekeningRepo tataRekeningRepo, IUnitOfWork unitOfWork)
    {
        _tataRekeningRepo = tataRekeningRepo;
        _unitOfWork = unitOfWork;
    }

    public Task<CloseBillResponse> Handle(CloseBillCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);

        using var scope = _unitOfWork.Begin();

        var tataRekening = _tataRekeningRepo.LoadEntity(request)
            .GetValueOrThrow($"Tata Rekening '{request.RegId}' tidak ditemukan.");

        tataRekening.Close();

        _tataRekeningRepo.SaveChanges(tataRekening);
        scope.Complete();

        return Task.FromResult(new CloseBillResponse(TataRekeningApplicationMapper.ToSummaryDto(tataRekening)));
    }
}
