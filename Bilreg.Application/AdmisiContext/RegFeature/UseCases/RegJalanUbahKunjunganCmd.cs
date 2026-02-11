using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanUbahKunjunganCmd(string RegId, 
    string LayananId, string JamPraktek, string KarcisId,
    string UserId) : IRequest, IRegKey, ILayananKey, IKarcisKey;
public record RegJalanUbahKunjunganHandler : IRequestHandler<RegJalanUbahKunjunganCmd>
{
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianMapHdrRepo _antrianMapRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;

    public RegJalanUbahKunjunganHandler(IRegRepo regRepo, 
        IRegAktifRepo regAktifRepo, 
        IJadwalPraktekRepo jadwalPraktekRepo, 
        IPpaRepo ppaRepo, 
        IAntrianRepo antrianRepo, 
        IAntrianFactory antrianFactory, 
        IAntrianMapHdrRepo antrianMapRepo, ILayananRepo layananRepo, IKarcisRepo karcisRepo)
    {
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _antrianMapRepo = antrianMapRepo;
        _layananRepo = layananRepo;
        _karcisRepo = karcisRepo;
    }

    public Task Handle(RegJalanUbahKunjunganCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        var regCurrent = _regAktifRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan atau sudah tidak aktif");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var dokter = _ppaRepo.LoadEntity(reg.Dokter).GetValueOrThrow("Dokter not found");
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");

        //  antrian
        var tglBerobat = reg.RegDate;
        var jadwal = ResolveJadwalPraktek(dokter, request.JamPraktek, tglBerobat);
        var antrian = ResolveAntrian(tglBerobat, dokter, jadwal);
        var antrianMap = LoadOrCreateAntrianMap(jadwal, tglBerobat);
        var noAntrian = antrianMap.GetNextNoAntrian();
        var tracker = PasienTrackerModel.Create(reg);
        
        //  registrasi
        reg.ChangeLayanan(layanan, karcis);
        
        //  TODO: Simpan
        throw new NotImplementedException();
    }
    private JadwalPraktekType ResolveJadwalPraktek(PpaType dokter, string jamPraktek, DateOnly tgl)
    {
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var listJadwalHari = listJadwal.Where(x => x.Hari == tgl.DayOfWeek)?.ToList() ?? [];

        return listJadwalHari.Count switch
        {
            1 => listJadwalHari.First(),
            > 1 => listJadwalHari.FirstOrDefault(x => x.JamMulai == TimeOnly.Parse(jamPraktek))
                   ?? throw new ArgumentException($"Dokter tidak praktek pada jam {jamPraktek}"),
            _ => JadwalPraktekType.Default with
            {
                Dokter = dokter.ToReff(),
                Hari = tgl.DayOfWeek,
                JamMulai = TimeOnly.Parse("00:00:00"),
                JamSelesai = TimeOnly.Parse("23:59:59")
            }
        };
    }
    private AntrianModel ResolveAntrian(DateOnly tgl, PpaType dokter, JadwalPraktekType jadwal)
    {
        var listAntrian = _antrianRepo.ListData(tgl);
        var tag = AntrianModel.GenSequenceTag(tgl, jadwal.JamMulai, dokter);
        var existingView = listAntrian.FirstOrDefault(x => x.SequenceTag == tag);
        return existingView is null
            ? _antrianFactory.Create(tgl, jadwal)
            : _antrianRepo.LoadEntity(existingView).Value;
    }
    private AntrianMapHdrModel LoadOrCreateAntrianMap(JadwalPraktekType jadwal, DateOnly tglJadwal)
    {
        var ppaKey = PpaType.Key(jadwal.Dokter.PpaId);
        var listAntrianMap = _antrianMapRepo
            .ListData(jadwal.Layanan, ppaKey, tglJadwal)?
            .ToList() ?? [];
        var antrianThis = listAntrianMap
            .SingleOrDefault(x => x.JamJadwal == jadwal.JamMulai);
        if (antrianThis is not null)
        {
            var antKey = AntrianMapHdrModel.Key(
                antrianThis.JadwalId,
                antrianThis.TglJadwal,
                antrianThis.dokter.PpaId,
                antrianThis.Layanan.LayananId,
                antrianThis.JamJadwal);

            return _antrianMapRepo.LoadEntity(antKey).Value;
        }
        var queueHdr = AntrianMapHdrModel.Create(
            jadwal.JadwalPraktekId,
            jadwal.Dokter,
            jadwal.Layanan,
            tglJadwal,
            jadwal.JamMulai,
            jadwal.JamMulai,
            []);
        queueHdr.GenerateSlot(jadwal.MaxPasien);
        return queueHdr;
    }
    
}

