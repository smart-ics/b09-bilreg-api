using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueSelesaiPeriksaCmd(string AntrianId, int NoUrut) : IRequest, IAntrianKey;

public class QueSelesaiPeriksaHandler : IRequestHandler<QueSelesaiPeriksaCmd>
{
    private readonly IAntrianRepo _queRepo;

    public QueSelesaiPeriksaHandler(IAntrianRepo queRepo)
    {
        _queRepo = queRepo;
    }

    public Task Handle(QueSelesaiPeriksaCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.Null(request.NoUrut);

        var que = _queRepo.LoadEntity(request).GetValueOrDefault();
        var item = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut) ?? AntrianEntryModel.Default;
        item.Done();

        _queRepo.SaveChanges(que);

        return Task.CompletedTask;
    }
}
