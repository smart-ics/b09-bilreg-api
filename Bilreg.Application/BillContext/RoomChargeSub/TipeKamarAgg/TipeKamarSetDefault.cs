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
        Guard.IsNotNullOrWhiteSpace(request.TipeKamarId);

        var tipeKamarList = _tipeKamarDal.ListData()
                            ?? throw new KeyNotFoundException("Tipe Kamar not Found");

        tipeKamarList.ToList().ForEach(kamar =>
        {
            (kamar.TipeKamarId == request.TipeKamarId ? (Action)kamar.SetDefault : kamar.ResetDefault)();
            _writer.Save(kamar);
        });
        
        return Task.CompletedTask;
    }
}
