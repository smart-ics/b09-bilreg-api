using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature.RujukanAgg;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature;

public record RegJalanCreateCommand(string PasienId, string RegDate, 
    string TipeJaminanId, string CaraMasukDkId, string RujukanId,
    string DokterId,string LayananId,  string KarcisId) : IRequest<RegJalanCreateResponse>; 

public record RegJalanCreateResponse(string RegId);

public class RegJalanCreateHandler : IRequestHandler<RegJalanCreateCommand, RegJalanCreateResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private const string BAYAR_SENDIRI = "1";
    
    public RegJalanCreateHandler(IPasienRepo pasienRepo, 
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo, 
        ICaraMasukDkRepo caraMasukDkRepo, IRujukanRepo rujukanRepo)
    {
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _rujukanRepo = rujukanRepo;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanCreateCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo
            .LoadEntity(PasienModel.Key(request.PasienId))
            .GetValueOrThrow("Pasien tidak ditemukan");
        var tipeJaminan = _tipeJaminanRepo
            .LoadEntity(TipeJaminanType.Key(request.TipeJaminanId))
            .GetValueOrThrow("Tipe Jaminan invalid");
        var polis = tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);

        var caraMasuk = _caraMasukDkRepo
            .LoadEntity(CaraMasukDkType.Key(request.CaraMasukDkId))
            .GetValueOrThrow("Cara Masuk invalid");
        var rujukan = caraMasuk == CaraMasukDkType.DatangSendiri ?
            RujukanType.Default :
            _rujukanRepo
                .LoadEntity(RujukanType.Key(request.RujukanId))
                .GetValueOrThrow("Rujukan invalid");
        
        //  TODO: Lanjutkan ke Layanan, Dokter dan NoAntrian
        // (Cek juga apakah booking atau bukan)
        throw new NotImplementedException();
    }

    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        if (polisView == null)
            throw new ArgumentException("Polis not found");
        var result = _polisRepo.LoadEntity(polisView).Value;
        return result;
    }
}