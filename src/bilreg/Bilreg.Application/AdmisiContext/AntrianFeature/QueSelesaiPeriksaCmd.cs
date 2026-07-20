using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueSelesaiPeriksaCmd(string AntrianId, int NoUrut) : IRequest, IAntrianKey;

public class QueSelesaiPeriksaHandler : IRequestHandler<QueSelesaiPeriksaCmd>
{
    private readonly IAntrianRepo _queRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public QueSelesaiPeriksaHandler(IAntrianRepo queRepo, ITglJamProvider? tglJamProvider = null)
    {
        _queRepo = queRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(QueSelesaiPeriksaCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.Null(request.NoUrut);

        var que = _queRepo.LoadEntity(request).GetValueOrDefault();
        var item = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut) ?? 
            throw new KeyNotFoundException($"antrian {request.NoUrut} not found");
        item.Done(_tglJamProvider.Now);

        _queRepo.SaveChanges(que);

        return Task.CompletedTask;
    }
}
