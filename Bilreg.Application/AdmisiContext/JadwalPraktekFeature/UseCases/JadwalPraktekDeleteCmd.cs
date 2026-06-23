using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JadwalPraktekFeature.UseCases;

public record JadwalPraktekDeleteCmd(string JadwalPraktekId) : IRequest, IJadwalPraktekKey;

public class JadwalPraktekDeleteHandler : IRequestHandler<JadwalPraktekDeleteCmd>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;

    public JadwalPraktekDeleteHandler(IJadwalPraktekRepo jadwalRepo)
    {
        _jadwalRepo = jadwalRepo;
    }

    public Task Handle(JadwalPraktekDeleteCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.JadwalPraktekId, nameof(request.JadwalPraktekId));

        var Jadwal = _jadwalRepo.LoadEntity(request)
            .GetValueOrDefault(JadwalPraktekType.Default);

        _jadwalRepo.DeleteEntity(request);

        return Task.CompletedTask;
    }
}
