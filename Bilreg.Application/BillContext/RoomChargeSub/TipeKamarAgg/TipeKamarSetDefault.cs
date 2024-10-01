using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarSetDefault(string TipeKamarId):IRequest,ITipeKamarKey;

public class TipeKamarSetDefaultHandler : IRequestHandler<TipeKamarSetDefault>
{
    private readonly ITipeKamarDal _tipeKamarDal;
    private readonly ITipeKamarWriter _writer;

    public TipeKamarSetDefaultHandler(ITipeKamarDal tipeKamarDal, ITipeKamarWriter writer)
    {
        _tipeKamarDal = tipeKamarDal;
        _writer = writer;
    }

    public Task Handle(TipeKamarSetDefault request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.IsNotNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.TipeKamarId);
        var tipeKamar = _tipeKamarDal.GetData(request)
                              ?? throw new KeyNotFoundException($"Tipe Kamar with id {request.TipeKamarId} not found");
        
        //  BUILD
        var list = _tipeKamarDal.ListData();
        var currentDefault = list.FirstOrDefault(x => x.IsDefault);
        if (currentDefault is not null)
            currentDefault.ResetDefault();
        
        tipeKamar.SetDefault();

        //  WRITE
        if (currentDefault is not null)
            _writer.Save(currentDefault);
        
        _writer.Save(tipeKamar);
        return Task.CompletedTask;
    }
}
