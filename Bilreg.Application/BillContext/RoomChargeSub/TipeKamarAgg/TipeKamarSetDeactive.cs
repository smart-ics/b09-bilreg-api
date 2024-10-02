using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarSetDeactive(string TipeKamarId):IRequest,ITipeKamarKey;

public class TipeKamarDeactiveHandler : IRequestHandler<TipeKamarSetDeactive>
{
    private readonly ITipeKamarDal _tipeKamarDal;
    private readonly ITipeKamarWriter _writer;

    public TipeKamarDeactiveHandler(ITipeKamarDal tipeKamarDal, ITipeKamarWriter writer)
    {
        _tipeKamarDal = tipeKamarDal;
        _writer = writer;
    }

    public Task Handle(TipeKamarSetDeactive request, CancellationToken cancellationToken)
    {
        Guard.IsNotNullOrWhiteSpace(request.TipeKamarId);
        
        var tipeKamar = _tipeKamarDal.GetData(request)
                        ?? throw new KeyNotFoundException($"Tipe Kamar with id {request.TipeKamarId} not found.");
        
        tipeKamar.UnSetAktif();
        _writer.Save(tipeKamar);
        return Task.CompletedTask;
    }
}