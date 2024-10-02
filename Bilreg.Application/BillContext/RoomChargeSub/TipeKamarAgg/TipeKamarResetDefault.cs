using Bilreg.Domain.BillContext.RoomChargeSub.TipeKamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.TipeKamarAgg;

public record TipeKamarResetDefault : IRequest;

public class TipeKamarResetDefaultHandler : IRequestHandler<TipeKamarResetDefault>
{
    private readonly ITipeKamarDal _tipeKamarDal;
    private readonly ITipeKamarWriter _writer;

    public TipeKamarResetDefaultHandler(ITipeKamarDal tipeKamarDal, ITipeKamarWriter writer)
    {
        _tipeKamarDal = tipeKamarDal;
        _writer = writer;
    }

    public Task Handle(TipeKamarResetDefault request, CancellationToken cancellationToken)
    {
        var tipeKamarList = _tipeKamarDal.ListData()
                            ?? throw new KeyNotFoundException("Tipe Kamar not Found");

        tipeKamarList.ToList().ForEach(kamar =>
        {
            kamar.ResetDefault(); 
            _writer.Save(kamar);
        });

        return Task.CompletedTask;

    }
}