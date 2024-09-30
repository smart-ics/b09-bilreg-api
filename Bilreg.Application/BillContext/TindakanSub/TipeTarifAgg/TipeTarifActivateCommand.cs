using Bilreg.Domain.BillContext.TindakanSub.TipeTarifAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.TindakanSub.TipeTarifAgg;

public record TipeTarifActivateCommand(string TipeTarifId) : IRequest, ITipeTarifKey;

public class  TipeTarifActivateHandler : IRequestHandler<TipeTarifActivateCommand>
{
    private readonly ITipeTarifDal _tipeTarifDal;
    private readonly ITipeTarifWriter _writer;

    public TipeTarifActivateHandler(ITipeTarifDal tipeTarifDal, ITipeTarifWriter writer)
    {
        _tipeTarifDal = tipeTarifDal;
        _writer = writer;
    }
    public Task Handle(TipeTarifActivateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.TipeTarifId);
        
        // BUILD
        var status = _tipeTarifDal.GetData(request)
            ?? throw new KeyNotFoundException($"Tipe TarifId: {request.TipeTarifId} not found");
        
        status.Activate();
        _writer.Save(status);
        return Task.CompletedTask;

    }
}