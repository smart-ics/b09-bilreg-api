using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using System.Diagnostics;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanUbahKunjunganCmd(string RegId, 
    string LayananId, string DokterId, string JamPraktek, string KarcisId,
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
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;

    public RegJalanUbahKunjunganHandler(IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo,
        IAntrianRepo antrianRepo,
        IAntrianFactory antrianFactory,
        IAntrianMapHdrRepo antrianMapRepo,
        ILayananRepo layananRepo,
        IKarcisRepo karcisRepo,
        INilaiTarifRepo nilaiTarifRepo,
        IKomponenRepo komponenRepo,
        IJaminanRepo jaminanRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        ITarifRepo tarifRepo,
        IPasienTrackerRepo trackerRepo,
        ITrsBillingRepo trsBillingRepo,
        ITindakanRepo tindakanRepo)
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
        _nilaiTarifRepo = nilaiTarifRepo;
        _komponenRepo = komponenRepo;
        _jaminanRepo = jaminanRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _tarifRepo = tarifRepo;
        _trackerRepo = trackerRepo;
        _trsBillingRepo = trsBillingRepo;
        _tindakanRepo = tindakanRepo;
    }

    public Task Handle(RegJalanUbahKunjunganCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        var regCurrent = _regAktifRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan atau sudah tidak aktif");
        var reg = _regRepo.LoadEntity(request).GetValueOrThrow("Register tidak ditemukan");
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId)).GetValueOrThrow("Dokter not found");
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");

        var tipeJaminan = LoadTipeJaminan(TipeJaminanType.Key(reg.TipeJaminan.TipeJaminanId));
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);

        //  antrian
        var tglBerobat = reg.RegDate;
        var jadwal = ResolveJadwalPraktek(dokter, request.JamPraktek, tglBerobat);
        var antrian = ResolveAntrian(tglBerobat, dokter, jadwal);
        var antrianMap = LoadOrCreateAntrianMap(jadwal, tglBerobat);
        var noAntrian = antrianMap.GetNextNoAntrian();
        var tracker = PasienTrackerModel.Create(reg);
        
        //  registrasi
        reg.ChangeDataKunjungan(layanan, karcis, dokter);
        var regAktif = RegAktifModel.CreateFromReg(reg);

        //  tindakan
        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter, request.KarcisId);

        //  billing karcis
        var billKarcis = reg.Karcis.KarcisId == request.KarcisId ? TrsBillingType.Default :
            GenBillKarcis(reg, dokter, karcis, jaminan);
        
        //  billing tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var billTdk = (tindakan.TindakanId != "-" && karcis.KarcisId != "-")
            ? GenBillTdk(tindakan, reg, jaminan, tarif)
            : TrsBillingType.Default;

        //  Simpan
        using var trans = TransHelper.NewScope();
        
        _regRepo.SaveChanges(reg);
        _regAktifRepo.SaveChanges(regAktif);
        SaveAntrian(antrian, antrianMap, tracker, noAntrian, reg);
        _trsBillingRepo.SaveChanges(billKarcis);
        SaveTindakan(tindakan);
        SaveBillTdk(billTdk);
        
        trans.Complete();

        return Task.CompletedTask;

    }
    #region PRIVATE-HELPER
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
    private TipeJaminanType LoadTipeJaminan(ITipeJaminanKey key)
    {
        return _tipeJaminanRepo.LoadEntity(key).GetValueOrDefault(TipeJaminanType.Default);
    }
    private JaminanType LoadJaminan(IJaminanKey key)
    {
        var jaminan = _jaminanRepo.LoadEntity(key).GetValueOrDefault(JaminanType.Default);
        return jaminan;
    }
    private TarifType LoadTarif(ITarifKey key)
    {
        return _tarifRepo.LoadEntity(key).GetValueOrDefault(TarifType.Default);
            
    }
    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan,
        KarcisType karcis, string userId, PpaType dokter, string karcisReq)
    {
        if(reg.Karcis.KarcisId != karcisReq)
        {
            var tipeTarifReff = jaminan.TipeTarif.Rajal;
            var tarifKey = karcis.DefaultTarif;
            var nilaiTarifKey = NilaiTarifType.KeyComposite(tarifKey, tipeTarifReff, reg.Kelas);
            var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiTarifKey).GetValueOrThrow("NilaiTarif not found");

            var listKomp = _komponenRepo
                .ListData(nilaiTarif.ListKomponen.Select(x => x.Komponen))?.ToList() ?? [];
            var listPpa = listKomp
                .Where(x => x.ListSatTugas.Any())
                .Select(x => new KomponenPpaView(x, dokter));

            var tindakan = TindakanModel.FromReg(reg, nilaiTarif, listPpa, userId);
            return tindakan;
        }
        return TindakanModel.Default;
    }
    private TrsBillingType GenBillKarcis(RegModel reg, PpaType dokter, KarcisType karcis, JaminanType jaminan)
    {
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))?.ToList() ?? [];
        var trsBillKarcis = TrsBillingType.CreateFromRegistrasi(reg, karcis,
            jaminan, dokter, listKompKarcis);
        return trsBillKarcis;

    }

    private TrsBillingType GenBillTdk(TindakanModel tdk, RegModel reg, JaminanType jaminan, TarifType tarif)
    {
        var listKomp = new List<KomponenType>();
        foreach (var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = TrsBillingType.CreateFromTindakan(tdk, reg, tarif, jaminan, listKomp);
        return trsBilling;
    }
    private KomponenType LoadKomponen(IKomponenKey key)
    {
        return _komponenRepo.LoadEntity(key).GetValueOrDefault(KomponenType.Default);

    }
    private AntrianMapHdrModel LoadAntrianMap(RegModel reg, AntrianModel que)
    {
        var ppaKey = PpaType.Key(reg.Dokter.PpaId);
        var lynKey = LayananType.Key(reg.Layanan.LayananId);
        var listAntrianMap = _antrianMapRepo
            .ListData(lynKey, ppaKey, reg.RegDate)?
            .ToList() ?? [];

        var antrianThis = listAntrianMap.SingleOrDefault(x => x.JamJadwal == que.StartTime);

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
        else
            return AntrianMapHdrModel.Default;
    }
    private (AntrianModel Que, int NoUrut) LoadAntrianOldContext(RegModel reg)
    {
        var queDate = reg.RegDate.ToDateTime(TimeOnly.MinValue);
        var listQue = _antrianRepo.ListData(queDate)?.ToList() ?? [];

        var queReg = listQue.FirstOrDefault(x => x.ReffId == reg.RegId);
        if (queReg is null)
            return (AntrianModel.Default, 0);

        var que = _antrianRepo
            .LoadEntity(AntrianModel.Key(queReg.AntrianId))
            .GetValueOrDefault(AntrianModel.Default);

        return (
            que, queReg.NoUrut
        );
    }
    private void SaveAntrian(AntrianModel antrian, AntrianMapHdrModel antrianMap, 
        PasienTrackerModel tracker, int noAntrian, RegModel reg)
    {
        var antEntry = antrian.AddEntry(noAntrian, tracker, reg.RegId, "REG");
        var itemQueue = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == noAntrian)
            ?? AntrianEntryModel.Default;
        itemQueue.Serve();
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);
        //      antrianMap
        antrianMap.SetDataPasien(noAntrian, reg.Pasien, reg.ToReff(), reg.RegId, "AUTO");
        _antrianMapRepo.SaveChanges(antrianMap);

        //      antrian map data lama
        var queOldContext = LoadAntrianOldContext(reg);
        var queMap = LoadAntrianMap(reg, queOldContext.Que);
        VoidAntrianMap(queMap, queOldContext.NoUrut);
    }

    private void SaveTindakan(TindakanModel tindakan)
    {
        if (tindakan.TindakanId == "-")
            return;
        _tindakanRepo.SaveChanges(tindakan);
    }
    private void SaveBillTdk(TrsBillingType billTdk)
    {
        if (billTdk.TrsBillingId == "-")
            return;
        _trsBillingRepo.SaveChanges(billTdk);
    }
    private void VoidAntrianMap(AntrianMapHdrModel queMap, int noUrut)
    {
        if (queMap.JadwalId == "-")
            return;

        queMap.VoidSlot(noUrut);
        _antrianMapRepo.SaveChanges(queMap);
    }
    #endregion
}

