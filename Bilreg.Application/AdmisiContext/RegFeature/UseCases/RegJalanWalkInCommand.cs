using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.Helpers.CommonValueObjects;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanWalkInCommand(string PasienId, string UserId,
    string TipeJaminanId, string CaraMasukDkId, string RujukanId, string DokterId,
    string LayananId, string JamPraktek, string KarcisId) 
    : IRequest<RegJalanCreateResponse>; 
 
public record RegJalanCreateResponse(string RegId, int NoAntrian);

public class RegJalanCreateHandler : IRequestHandler<RegJalanWalkInCommand, RegJalanCreateResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IPetugasMedisRepo _dokterRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IPasienTrackerRepo _trackerRepo;

    private const string BAYAR_SENDIRI = "1";
    
    public RegJalanCreateHandler(IPasienRepo pasienRepo, 
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo, 
        ICaraMasukDkRepo caraMasukDkRepo, 
        IRujukanRepo rujukanRepo, 
        ILayananRepo layananRepo, 
        IPetugasMedisRepo dokterRepo, 
        IAntrianFactory antrianFactory, 
        IAntrianRepo antrianRepo, 
        IJadwalPraktekRepo jadwalRepo, 
        IRegFactory regFactory, 
        IKarcisRepo karcisRepo, 
        IRegRepo regRepo, 
        IPasienTrackerRepo trackerRepo)
    {
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _rujukanRepo = rujukanRepo;
        _layananRepo = layananRepo;
        _dokterRepo = dokterRepo;
        _antrianFactory = antrianFactory;
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalRepo;
        _regFactory = regFactory;
        _karcisRepo = karcisRepo;
        _regRepo = regRepo;
        _trackerRepo = trackerRepo;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanWalkInCommand request, CancellationToken cancellationToken)
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
            .GetValueOrThrow("'Cara Masuk' not found");
        var rujukan = caraMasuk == CaraMasukDkType.DatangSendiri ?
            RujukanType.Default :
            _rujukanRepo
                .LoadEntity(RujukanType.Key(request.RujukanId))
                .GetValueOrThrow("'Rujukan' not found");
        
        var layanan = _layananRepo
            .LoadEntity(LayananType.Key(request.LayananId))
            .GetValueOrThrow("'Layanan' not found");
        if (layanan.InstalasiDk == InstalasiDkType.RawatInap)
            throw new ArgumentException("Layanan Rawat Inap tidak bisa digunakan di Registrasi Rawat Jalan");
        var dokter = _dokterRepo
            .LoadEntity(PetugasMedisType.Key(request.DokterId))
            .GetValueOrThrow("Dokter not found");
        var karcis = _karcisRepo.LoadEntity(KarcisType.Key(request.KarcisId))
            .GetValueOrThrow("Karcis not found");
        
        //  ambil nomor antrian
        //      pertama coba cari jadwal dokter ybs
        //      jika tidak ditemukan maka create antrian 1 full day (mulai jam 00 s.d 23)
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var tglBerobat = DateOnly.FromDateTime(DateTime.Now);
        var hari = tglBerobat.DayOfWeek;
        var listJadwalHari = listJadwal
            .Where(x => x.Hari == hari)?.ToList() ?? [];
        var jadwal = listJadwalHari.Count switch
        {
            1 => listJadwalHari.First(),
            > 1 => listJadwalHari.FirstOrDefault(x => x.JamMulai == TimeOnly.Parse(request.JamPraktek)) 
                   ?? throw new ArgumentException($"Dokter tidak praktek pada jam {request.JamPraktek}"),
            _ => JadwalPraktekType.Default with
            {
                Dokter = dokter.ToReff(),
                Hari = hari,
                JamMulai = TimeOnly.Parse("00:00:00"),
                JamSelesai = TimeOnly.Parse("23:59:59")
            }
        };
        var listAntrian = _antrianRepo.ListData(tglBerobat);
        var sequenceTag = AntrianModel.GenSequenceTag(tglBerobat, dokter);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var antrian = antrianView is null ? 
            _antrianFactory.Create(tglBerobat, jadwal) :
            _antrianRepo.LoadEntity(antrianView).Value;
        var regMasukAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajalWalkIn(pasien, regMasukAudit, 
            tipeJaminan, polis, caraMasuk, rujukan, dokter, layanan, karcis);
        var tracker = PasienTrackerModel.Create(reg);
        
        using var trans = TransHelper.NewScope();
        var antEntry = antrian.AddEntry(tracker);
        _regRepo.SaveChanges(reg);
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);
        trans.Complete();

        return Task.FromResult(new RegJalanCreateResponse(reg.RegId, antEntry.NoUrut));
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