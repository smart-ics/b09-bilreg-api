using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanSub;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
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
    private const string BAYAR_SENDIRI = "1";
    public RegJalanCreateHandler(IPasienRepo pasienRepo, 
        ITipeJaminanRepo tipeJaminanRepo)
    {
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanCreateCommand request, CancellationToken cancellationToken)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(request.PasienId))
            .GetValueOrThrow("Pasien tidak ditemukan");
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(request.TipeJaminanId))
            .GetValueOrThrow("Tipe Jaminan invalid");
        //var polis = PolisModel.Default;
        if (tipeJaminan.CaraBayarDk.CaraBayarDkId != BAYAR_SENDIRI)
            throw new NotImplementedException();
        
        throw new NotImplementedException();
    }
}