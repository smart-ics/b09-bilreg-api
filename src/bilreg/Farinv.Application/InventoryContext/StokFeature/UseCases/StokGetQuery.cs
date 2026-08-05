using Farinv.Domain.InventoryContext.StokFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Farinv.Application.InventoryContext.StokFeature.UseCases;

public record StokGetKartuStokQuery(string BrgId, string LayananId) : IRequest<StokModel>;

public class StokGetKartuStokHandler : IRequestHandler<StokGetKartuStokQuery, StokModel>
{
    private readonly IStokRepo _stokRepo;

    public StokGetKartuStokHandler(IStokRepo stokRepo)
    {
        _stokRepo = stokRepo;
    }

    public Task<StokModel> Handle(StokGetKartuStokQuery request, CancellationToken cancellationToken)
    {
        var brgLayananKey = StokModel.Key(request.BrgId, request.LayananId);
        using var trans = TransHelper.NewScope();
        var result = _stokRepo.LoadEntity(brgLayananKey);
        trans.Complete();
        return Task.FromResult(result.Value);
    }
}
