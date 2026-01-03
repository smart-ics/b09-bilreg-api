using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
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
    private readonly IPpaRepo _dokterRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly IAntrianMapHdrRepo _antrianMapRepo;
    
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private const string BAYAR_SENDIRI = "1";

    public RegJalanCreateHandler(IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        IRujukanRepo rujukanRepo,
        ILayananRepo layananRepo,
        IPpaRepo dokterRepo,
        IAntrianFactory antrianFactory,
        IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalRepo,
        IRegFactory regFactory,
        IKarcisRepo karcisRepo,
        IRegRepo regRepo,
        IPasienTrackerRepo trackerRepo,
        IRegAktifRepo regAktifRepo,
        IAntrianMapHdrRepo antrianMapRepo,
        IJaminanRepo jaminanRepo,
        ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITipeTarifRepo tipeTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo)
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
        _regAktifRepo = regAktifRepo;
        _antrianMapRepo = antrianMapRepo;
        _jaminanRepo = jaminanRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanWalkInCommand request, CancellationToken cancellationToken)
    {

        var pasien = LoadPasien(request.PasienId);
        var pasienAktif = GetRegaktif(pasien, LayananType.Key(request.LayananId));
        if (pasienAktif is not null)
            throw new KeyNotFoundException($"Pasien aktif di register {pasienAktif.RegId}");

        var tipeJaminan = LoadTipeJaminan(request.TipeJaminanId);
        var polis = ResolvePolis(pasien, tipeJaminan);
        
        var caraMasuk = LoadCaraMasuk(request.CaraMasukDkId);
        var rujukan = ResolveRujukan(caraMasuk, request.RujukanId);
        
        var layanan = LoadLayanan(request.LayananId);
        ValidateLayanan(layanan);
        var dokter = LoadDokter(request.DokterId);
        var karcis = LoadKarcis(request.KarcisId);

        var tglBerobat = DateOnly.FromDateTime(DateTime.Now);
        var jadwal = ResolveJadwalPraktek(dokter, request.JamPraktek, tglBerobat);
        var antrian = ResolveAntrian(tglBerobat, dokter, jadwal);
        // antrianMap
        var antrianMap = CekAntrianMap(jadwal, tglBerobat);
        var noAntrian = antrianMap.GetNextNoAntrian();

        var regMasukAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajal(pasien, regMasukAudit,
            tipeJaminan, polis, caraMasuk, rujukan, dokter, layanan, karcis);

        var regDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);

        var regAktif = new RegAktifModel(reg.RegId, regDate,
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);

        var tracker = PasienTrackerModel.Create(reg);

        var tindakan = TindakanModel.Default; 
        if(karcis.DefaultTarif.TarifId != "-")
            tindakan = GenTindakan(reg, tipeJaminan, karcis.DefaultTarif, 
                pasien, layanan, request.UserId, dokter);
        


        using var trans = TransHelper.NewScope();
        var antEntry = antrian.AddEntry(noAntrian, tracker);
        _regRepo.SaveChanges(reg);
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);
        _regAktifRepo.SaveChanges(regAktif);

        // rubah antrianMapHdr
        antrianMap.SetDataPasien(noAntrian, reg.Pasien, reg.ToReff(), reg.RegId, "AUTO");
        _antrianMapRepo.SaveChanges(antrianMap);

        if (karcis.DefaultTarif.TarifId != "-")
            _tindakanRepo.SaveChanges(tindakan);


        trans.Complete();

        return Task.FromResult(new RegJalanCreateResponse(reg.RegId, antEntry.NoUrut));
    }

    #region PRIVATE-HELPERS
    private RegAktifModel? GetRegaktif(IPasienKey pasienKey, ILayananKey lynKey)
    {
        var listPasienAktif = _regAktifRepo.ListData(lynKey)?.ToList() ?? [];
        var pasienAktif = listPasienAktif
            .FirstOrDefault(x => x.Pasien.PasienId == pasienKey.PasienId);

        return pasienAktif; 
    }

    private PasienModel LoadPasien(string id) =>
        _pasienRepo.LoadEntity(PasienModel.Key(id))
            .GetValueOrThrow("Pasien tidak ditemukan");

    private TipeJaminanType LoadTipeJaminan(string id) =>
        _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(id))
            .GetValueOrThrow("Tipe Jaminan invalid");

    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);

    private CaraMasukDkType LoadCaraMasuk(string id) =>
        _caraMasukDkRepo.LoadEntity(CaraMasukDkType.Key(id))
            .GetValueOrThrow("'Cara Masuk' not found");

    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string rujukanId) =>
        caraMasuk == CaraMasukDkType.DatangSendiri
            ? RujukanType.Default
            : _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
                .GetValueOrThrow("'Rujukan' not found");

    private LayananType LoadLayanan(string id) =>
        _layananRepo.LoadEntity(LayananType.Key(id))
            .GetValueOrThrow("'Layanan' not found");

    private static void ValidateLayanan(LayananType layanan)
    {
        if (layanan.InstalasiDk == InstalasiDkType.RawatInap)
            throw new ArgumentException("Layanan Rawat Inap tidak bisa digunakan di Registrasi Rawat Jalan");
    }

    private PpaType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow("Dokter not found");

    private KarcisType LoadKarcis(string id) =>
        _karcisRepo.LoadEntity(KarcisType.Key(id))
            .GetValueOrThrow("Karcis not found");

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
        var tag = AntrianModel.GenSequenceTag(tgl, dokter);
        var existingView = listAntrian.FirstOrDefault(x => x.SequenceTag == tag);
        return existingView is null
            ? _antrianFactory.Create(tgl, jadwal)
            : _antrianRepo.LoadEntity(existingView).Value;
    }

    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        if (polisView == null)
            throw new ArgumentException("Polis not found");
        return _polisRepo.LoadEntity(polisView).Value;
    }

    private AntrianMapHdrModel CekAntrianMap(JadwalPraktekType jadwal, DateOnly tglJadwal)
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

    private TindakanModel GenTindakan(RegModel reg, TipeJaminanType tipeJaminan, 
        TarifReff tarifReff, PasienModel pasien, 
        LayananType layanan, string userId, PpaType dokter)
    {
        var jaminan = LoadJaminan(JaminanType.Key(tipeJaminan.Jaminan.JaminanId));
        var tipeTarifJmn = jaminan.ListTipeTarif
            ?.FirstOrDefault(x => x.JenisRegid == JenisRegEnum.RegJalan)
            ?? throw new InvalidOperationException(
                $"Tipe tarif untuk RegJalan tidak ditemukan pada jaminan {jaminan.JaminanId}");

        var tarif = LoadTarif(TarifType.Key(tarifReff.TarifId));
        var tipeTarif = LoadTipeTarif(TipeTarifType.Key(tipeTarifJmn.TipeTarif.TipeTarifId));
        var nilaiTarifKey = NilaiTarifType.KeyComposite(tarif.TarifId, tipeTarif.TipeTarifId, reg.Kelas.KelasId);
        var nilaiTarif = LoadNilaiTarif(nilaiTarifKey);

        var listKompMaster = _komponenRepo
            .ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];
        
        var listKompPpa = listKompMaster.Where(x => x.ListSatTugas.Any())?.ToList() ?? [];

        var listPpa = new List<KomponenPpaView>();
        foreach(var item in listKompPpa)
        {
            var komp = LoadKomponen(KomponenType.Key(item.KomponenId));
            var ppa = dokter;
            listPpa.Add(new KomponenPpaView(komp, ppa));
        }
        var tindakan = TindakanModel.Create(reg, layanan, nilaiTarif, listPpa, userId);

        return tindakan;
    }
    private KomponenType LoadKomponen(IKomponenKey key)
    {
        var komponen = _komponenRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Komponen Nilai Tarif '{key.KomponenId}' invalid")
            );
        return komponen;
    }
    private JaminanType LoadJaminan(IJaminanKey key)
    {
        var jaminan = _jaminanRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Jaminan {key.JaminanId} not found")
            );
        return jaminan;
    }
    private TarifType LoadTarif(ITarifKey key)
    {
        var tarif = _tarifRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Tarif {key.TarifId} not found")
            );
        return tarif;
    }
    private TipeTarifType LoadTipeTarif(ITipeTarifKey key)
    {
        var tipeTarif = _tipeTarifRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"TipeTarif {key.TipeTarifId} not found")
            );
        return tipeTarif;
    }
    private NilaiTarifType LoadNilaiTarif(INilaiTarifCompositKey key)
    {
        var nilaiTarif = _nilaiTarifRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"NilaiTarif Tarif:{key.TarifId}, Tipe:{key.TipeTarifId}, Kelas:{key.KelasId} not found")
            );
        return nilaiTarif;
    }
    #endregion
}