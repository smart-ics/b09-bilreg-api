using Ardalis.GuardClauses;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueMulaiPeriksaCmd(string AntrianId, int NoUrut) : IRequest, IAntrianKey;

public class QueMulaiPeriksaHandler : IRequestHandler<QueMulaiPeriksaCmd>
{
    private readonly IAntrianRepo _queRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public QueMulaiPeriksaHandler(IAntrianRepo queRepo, ITglJamProvider tglJamProvider)
    {
        _queRepo = queRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task Handle(QueMulaiPeriksaCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AntrianId);
        Guard.Against.Null(request.NoUrut);

        var que = _queRepo.LoadEntity(request).GetValueOrDefault();
        var item = que.ListEntry.FirstOrDefault(x => x.NoUrut == request.NoUrut)
            ?? throw new KeyNotFoundException($"antrian {request.NoUrut} not found");
        item.Serve(_tglJamProvider.Now);

        _queRepo.SaveChanges(que);

        return Task.CompletedTask;
    }
}
