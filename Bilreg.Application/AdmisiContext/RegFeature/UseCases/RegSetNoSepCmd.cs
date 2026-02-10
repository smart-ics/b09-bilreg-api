using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegSetNoSepCmd(String RegId, string SepNo) : IRequest, IRegKey;

public class RegSetNoSepHandler : IRequestHandler<RegSetNoSepCmd>
{
    private readonly IRegRepo _regRepo;

    public RegSetNoSepHandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public Task Handle(RegSetNoSepCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.SepNo);
        Guard.Against.LengthOutOfRange(request.SepNo, 1, 30);

        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        reg.SetNoSep(request.SepNo);

        _regRepo.SaveChanges(reg);
        return Task.CompletedTask;
    }
}
