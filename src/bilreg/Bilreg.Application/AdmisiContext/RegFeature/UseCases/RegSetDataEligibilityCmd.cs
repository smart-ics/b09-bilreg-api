using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegSetDataEligibilityCmd(string RegId, string SjpNo, string PesertaJaminanId, string SjpId) : IRequest, IRegKey;

public class RegSetDataEligibilityHandler : IRequestHandler<RegSetDataEligibilityCmd>
{
    private readonly IRegRepo _regRepo;

    public RegSetDataEligibilityHandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public Task Handle(RegSetDataEligibilityCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.SjpNo);
        Guard.Against.NullOrWhiteSpace(request.PesertaJaminanId);
        Guard.Against.NullOrWhiteSpace(request.SjpId);
        Guard.Against.LengthOutOfRange(request.SjpNo, 1, 30);

        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        reg.SetEligibility(request.SjpNo, request.PesertaJaminanId, request.SjpId);
        
        _regRepo.SaveChanges(reg);
        return Task.CompletedTask;
    }
}
