using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekMigrasiCmd() : IRequest;

public class JadwalPraktekMigrasiHandler : IRequestHandler<JadwalPraktekMigrasiCmd>
{
    private readonly IJadwalPraktekRepo _repo;

    public JadwalPraktekMigrasiHandler(IJadwalPraktekRepo repo)
    {
        _repo = repo;
    }

    public Task Handle(JadwalPraktekMigrasiCmd request, CancellationToken cancellationToken)
    {
        _repo.Migrasi();
        return Task.CompletedTask;
    }
}
