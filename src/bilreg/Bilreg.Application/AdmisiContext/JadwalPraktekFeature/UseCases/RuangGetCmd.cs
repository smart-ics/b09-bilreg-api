using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record RuangGetCmd(string RuangId) : IRequest<RuangGetResponse>, IRuangKey;

public record RuangGetResponse(string RuangId, string RuangName, string PrefixAntrian);

public class RuangGetHandler : IRequestHandler<RuangGetCmd, RuangGetResponse>
{
    private readonly IRuangRepo _ruangRepo;

    public RuangGetHandler(IRuangRepo ruangRepo)
    {
        _ruangRepo = ruangRepo;
    }

    public Task<RuangGetResponse> Handle(RuangGetCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RuangId);

        var ruang = _ruangRepo.LoadEntity(request)
            .GetValueOrThrow($"Ruang {request.RuangId} invalid");
        var result = new RuangGetResponse(ruang.RuangId, ruang.RuangName, ruang.PrefixAntrian);

        return Task.FromResult(result);
    }
}
