using Bilreg.Application.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Application.BillContext.RoomChargeSub.KelasAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.BangsalAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KamarAgg;
using Bilreg.Domain.BillContext.RoomChargeSub.KelasAgg;
using CommunityToolkit.Diagnostics;
using MediatR;

namespace Bilreg.Application.BillContext.RoomChargeSub.KamarAgg;

public record KamarSaveCommand(
        string KamarId,
        string KamarName,
        string Ket1,
        string Ket2,
        string Ket3,
        string BangsalId,
        string KelasId
    ):IRequest,IKamarKey,IBangsalKey,IKelasKey;

public class KamarSaveHandler : IRequestHandler<KamarSaveCommand>
{
    private readonly IKamarDal _kamarDal;
    private readonly IBangsalDal _bangsalDal;
    private readonly IKelasDal _kelasDal;
    private readonly IKamarWriter _writer;

    public KamarSaveHandler(IKamarDal kamarDal, IBangsalDal bangsalDal, IKelasDal kelasDal, IKamarWriter writer)
    {
        _kamarDal = kamarDal;
        _bangsalDal = bangsalDal;
        _kelasDal = kelasDal;
        _writer = writer;
    }

    public Task Handle(KamarSaveCommand request, CancellationToken cancellationToken)
    {
        Guard.IsNotNull(request);
        Guard.IsNotNullOrWhiteSpace(request.KamarId);
        Guard.IsNotNull(request.KamarName);
        Guard.IsNotNullOrWhiteSpace(request.BangsalId);
        Guard.IsNotNullOrWhiteSpace(request.KelasId);
        Guard.IsNotNull(request.Ket1);
        Guard.IsNotNull(request.Ket2);
        Guard.IsNotNull(request.Ket3);
        
        var kamar = _kamarDal.GetData(request)
            ?? new KamarModel(request.KamarId,
                              request.KamarName);
        
        var bangsal = _bangsalDal.GetData(request) 
            ?? throw new KeyNotFoundException($"Bangsal with id {request.BangsalId} not found");
        
        var kelas = _kelasDal.GetData(request) 
            ?? throw new KeyNotFoundException($"Kelas with id {request.KelasId} not found");
        
        kamar.SetKet(request.Ket1,request.Ket2,request.Ket3);
        kamar.SetBangsal(bangsal);
        kamar.SetKelas(kelas);

        _writer.Save(kamar);
        return Task.CompletedTask;
    }
}

