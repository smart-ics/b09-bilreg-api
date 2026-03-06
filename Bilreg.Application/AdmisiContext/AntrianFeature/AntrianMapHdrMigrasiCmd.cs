using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianMapHdrMigrasiCmd() : IRequest;
public class AntrianMapHdrMigrasiHandler : IRequestHandler<AntrianMapHdrMigrasiCmd>
{
    private readonly IAntrianMapHdrRepo _mapHdrRepo;

    public AntrianMapHdrMigrasiHandler(IAntrianMapHdrRepo mapHdrRepo)
    {
        _mapHdrRepo = mapHdrRepo;
    }

    public Task Handle(AntrianMapHdrMigrasiCmd request, CancellationToken cancellationToken)
    {
        var tglNow = DateTime.Now;
        _mapHdrRepo.Migrasi(tglNow);
        return Task.CompletedTask;
    }
}
