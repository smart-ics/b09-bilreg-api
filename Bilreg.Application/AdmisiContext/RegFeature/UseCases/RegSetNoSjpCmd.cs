using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegSetNoSjpCmd(String RegId, string SjpNo) : IRequest, IRegKey;

public class RegSetNoSjpHandler : IRequestHandler<RegSetNoSjpCmd>
{
    private readonly IRegRepo _regRepo;

    public RegSetNoSjpHandler(IRegRepo regRepo)
    {
        _regRepo = regRepo;
    }

    public Task Handle(RegSetNoSjpCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId);
        Guard.Against.NullOrWhiteSpace(request.SjpNo);
        Guard.Against.LengthOutOfRange(request.SjpNo, 1, 30);

        var reg = _regRepo.LoadEntity(request).GetValueOrThrow($"Register {request.RegId} not found");
        reg.SetNoSjp(request.SjpNo);

        _regRepo.SaveChanges(reg);
        return Task.CompletedTask;
    }
}
