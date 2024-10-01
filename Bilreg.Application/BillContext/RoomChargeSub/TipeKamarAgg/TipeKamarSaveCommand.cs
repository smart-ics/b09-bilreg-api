using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarSaveCommand(string TipeKamarId,string TipeKamarName, bool isGabung) : IRequest,ITipeKamarKey;

public class TipeKamarSaveHandler : IRequestHandler<TipeKamarSaveCommand>
{
    private readonly ITipeKamarDal _tipeKamarDal;
    private readonly ITipeKamarWriter _writer;

    public TipeKamarSaveHandler(ITipeKamarDal tipeKamarDal, ITipeKamarWriter writer)
    {
        _tipeKamarDal = tipeKamarDal;
        _writer = writer;
    }

    public Task Handle(TipeKamarSaveCommand request, CancellationToken cancellationToken)
    {
        Guard.IsNotNull(request);
        Guard.IsNotWhiteSpace(request.TipeKamarId);
        Guard.IsNotWhiteSpace(request.TipeKamarName);

        var tipeKamar = _tipeKamarDal.GetData(request)
                        ?? new TipeKamarModel(request.TipeKamarId, request.TipeKamarName);
        
        tipeKamar.SetGabung();
        _writer.Save(tipeKamar);
        return Task.CompletedTask;

    }
}