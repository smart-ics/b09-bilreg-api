using Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Application.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Application.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Application.AdmisiContext.PetugasMedisSub.PetugasMedisAgg;
using Bilreg.Application.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Application.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Application.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.PolisAgg;
using Bilreg.Domain.AdmisiContext.JaminanSub.TipeJaminanAgg;
using Bilreg.Domain.AdmisiContext.LayananSub.LayananAgg;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasAgg;
using Bilreg.Domain.AdmisiContext.RegSub.KarcisAgg;
using Bilreg.Domain.AdmisiContext.RegSub.RegAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.CaraMasukDkAgg;
using Bilreg.Domain.AdmisiContext.RujukanSub.RujukanAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.AutoNumberHelper;

namespace Bilreg.Application.AdmisiContext.RegSub.RegJalanAgg;

public record RegJalanCreateCommand(
    string PasienId, string RegDate, 
    string TipeJaminanId, string PolisId, 
    string CaraMasukDkId, string RujukanId, string KarcisId,
    string LayananId, string DokterId, int noAntrian) 
    : IRequest<RegJalanCreateResponse>, IPasienKey,
        ILayananKey, ITipeJaminanKey, IPolisKey, ICaraMasukDkKey,
        IRujukanKey, IKarcisKey;

public record RegJalanCreateResponse(string RegId);

public class RegJalanCreateHandler : IRequestHandler<RegJalanCreateCommand, RegJalanCreateResponse>
{
    private readonly INunaCounterBL _counter;
    private readonly IPasienDal _pasienDal;
    private readonly ILayananDal _layananDal;
    private readonly IPetugasMedisDal _dokterDal;
    private readonly ITipeJaminanDal _tipeJaminanDal;
    private readonly IPolisDal _polisDal;
    private readonly ICaraMasukDkDal _caraMasukDkDal;
    private readonly IRujukanDal _rujukanDal;
    private readonly IKarcisDal _karcisDal;

    public RegJalanCreateHandler(INunaCounterBL counter, IPasienDal pasienDal, ILayananDal layananDal,
        IPetugasMedisDal dokterDal, ITipeJaminanDal tipeJaminanDal, IPolisDal polisDal, ICaraMasukDkDal caraMasukDkDal,
        IRujukanDal rujukanDal, IKarcisDal karcisDal)
    {
        _counter = counter;
        _pasienDal = pasienDal;
        _layananDal = layananDal;
        _dokterDal = dokterDal;
        _tipeJaminanDal = tipeJaminanDal;
        _polisDal = polisDal;
        _caraMasukDkDal = caraMasukDkDal;
        _rujukanDal = rujukanDal;
        _karcisDal = karcisDal;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        GuardInput(request);
        
        //  BUILD
        var pasien = _pasienDal.GetData(request); 
        var tipeJaminan = _tipeJaminanDal.GetData(request);
        var polis = _polisDal.GetData(request);
        var caraMasukDk = _caraMasukDkDal.GetData(request);
        var rujukan = _rujukanDal.GetData(request);
        var layanan = _layananDal.GetData(request);
        var dokter = _dokterDal.GetData(new PetugasMedisModel(request.DokterId, string.Empty));

        // var newRegId = _counter.GenerateDec("NOREG", "RG", 10, string.Empty);
        var reg = new RegBuilder()
            .SetRegDate(DateTime.Now)
            .WithPasien(pasien)
            .WithJaminan(tipeJaminan, polis)
            .WithCaraMasuk(caraMasukDk, rujukan);
            //.AddLayanan(layanan, dokter, request.noAntrian);
        
            

        throw new NotImplementedException();
    }

    private static void GuardInput(RegJalanCreateCommand request)
    {
        Guard.IsNotNull(request);
        Guard.IsNotEmpty(request.PasienId);
        Guard.IsNotEmpty(request.RegDate);
        Guard.IsNotEmpty(request.LayananId);
        Guard.IsNotEmpty(request.DokterId);
        Guard.IsNotEmpty(request.TipeJaminanId);
        Guard.IsNotEmpty(request.PolisId);
        Guard.IsNotEmpty(request.CaraMasukDkId);
        Guard.IsNotEmpty(request.RujukanId);
        Guard.IsNotEmpty(request.KarcisId);
        Guard.IsGreaterThanOrEqualTo(request.noAntrian, 0);
    }


}