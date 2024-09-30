using Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BedAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.BedAgg;

public record BedSaveCommand(
    string BedId,
    string BedName,
    string Keterangan,
    string KamarId) : IRequest, IBedKey, IKamarKey;

public class BedSaveHandler : IRequestHandler<BedSaveCommand>
{
    private readonly IBedDal _bedDal;
    private readonly IKamarDal _kamarDal;
    private readonly IBedWriter _writer;

    public BedSaveHandler(IBedDal bedDal, IKamarDal kamarDal, IBedWriter writer)
    {
        _bedDal = bedDal;
        _kamarDal = kamarDal;
        _writer = writer;
    }
    public Task Handle(BedSaveCommand request, CancellationToken cancellationToken)
    { 
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.BedId);
        Guard.IsNotNullOrWhiteSpace(request.BedName);
        Guard.IsNotNullOrWhiteSpace(request.KamarId);
        
        // BUILD 
        var bed = _bedDal.GetData(request)
                  ?? new BedModel(request.BedId, request.BedName);
        
        var kamar = _kamarDal.GetData(request)
            ?? throw new KeyNotFoundException($"No kamar found for {request.KamarId}");
        
        bed.SetKeterangan(request.Keterangan);
        bed.SetKamar(kamar);
        
        // WRITE
        _writer.Save(bed);
        return Task.CompletedTask;

    }
}