using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RemoteCetakFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Application.Shared.Helpers;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RemotCetakFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanWalkInCommand(string PasienId, string UserId,
    string TipeJaminanId, string CaraMasukDkId, string RujukanId, string DokterId,
    string LayananId, string JamPraktek, string KarcisId, string PesertaJaminanId) 
    : IRequest<RegJalanCreateResponse>, ILayananKey, ICaraMasukDkKey, IPasienKey,
        ITipeJaminanKey, IKarcisKey; 
 
public record RegJalanCreateResponse(string RegId, int NoAntrian);

public class RegJalanCreateHandler : IRequestHandler<RegJalanWalkInCommand, RegJalanCreateResponse>
{
    //  reg support
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly IRujukanRepo _rujukanRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IKarcisRepo _karcisRepo;
    //  reg
    private readonly IRegFactory _regFactory;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    //  antrian
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianMapRepo _antrianMapRepo;
    //  tindakan
    private readonly IJaminanRepo _jaminanRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITarifRepo _tarifRepo;
    // trsBill
    private readonly ITrsBillingRepo _trsBillingRepo;
    // jurnal
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;
    private const string BAYAR_SENDIRI = "1";

    private readonly IRemoteCetakRepo _remoteCetakRepo;
    private readonly IGetAppSettingService _getAppSettingSvc;
    private readonly IDashboardEMrAddRegService _addRegSvc;

    public RegJalanCreateHandler(
        //  reg support
        IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        IRujukanRepo rujukanRepo,
        ILayananRepo layananRepo,
        IPpaRepo ppaRepo,
        IKarcisRepo karcisRepo,
        //  registrasi
        IRegFactory regFactory,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        //  antrian
        IPasienTrackerRepo trackerRepo,
        IAntrianFactory antrianFactory,
        IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianMapRepo antrianMapRepo,
        //  tindakan
        IJaminanRepo jaminanRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo,
        ITarifRepo tarifRepo,
        //  trsBill
        ITrsBillingRepo trsBillingRepo,
        // jurnal
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo,
        IRemoteCetakRepo remoteCetakRepo,
        IGetAppSettingService getAppSettingSvc,
        IDashboardEMrAddRegService addRegSvc)
    {
        //      reg-support
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _rujukanRepo = rujukanRepo;
        _layananRepo = layananRepo;
        _ppaRepo = ppaRepo;
        _karcisRepo = karcisRepo;
        //      reg
        _regFactory = regFactory;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        //      antrian
        _trackerRepo = trackerRepo;
        _antrianFactory = antrianFactory;
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianMapRepo = antrianMapRepo;
        //      tindakan
        _jaminanRepo = jaminanRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
        _tarifRepo = tarifRepo;
        //      trsBill
        _trsBillingRepo = trsBillingRepo;
        //      jurnal
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
        _remoteCetakRepo = remoteCetakRepo;
        _getAppSettingSvc = getAppSettingSvc;
        _addRegSvc = addRegSvc;
    }

    public Task<RegJalanCreateResponse> Handle(RegJalanWalkInCommand request, CancellationToken cancellationToken)
    {
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   GUARD   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/
        Guard.Against.Null(request.PesertaJaminanId, nameof(request.PesertaJaminanId));
        var pasien = _pasienRepo.LoadEntity(request).GetValueOrThrow("Pasien not found");
        if (pasien.IsAktif == false) throw new KeyNotFoundException($"Pasien {request.PasienId} tidak aktif ");
        if(IsPasienAktifReg(pasien))
            throw new KeyNotFoundException($"Pasien sudah aktif registrasi");

        if (string.IsNullOrWhiteSpace(pasien.Ktp.Nik) || pasien.Ktp.Nik == "-")
            throw new KeyNotFoundException($"Nik Kosong, Lengkapi data Nik pasien {pasien.PasienId}");

        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request).GetValueOrThrow("TipeJaminan not found");
        var polis = ResolvePolis(pasien, tipeJaminan);
        var caraMasuk = _caraMasukDkRepo.LoadEntity(request).GetValueOrThrow("CaraMasuk not found");
        var rujukan = ResolveRujukan(caraMasuk, request.RujukanId);
        var layanan = _layananRepo.LoadEntity(request).GetValueOrThrow("Layanan not found");
        var dokter = _ppaRepo.LoadEntity(PpaType.Key(request.DokterId)).GetValueOrThrow("Dokter not found");
        var karcis = _karcisRepo.LoadEntity(request).GetValueOrThrow("Karcis not found");
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   BUILD   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/
        //      1-register
        var regMasukAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajal(pasien, regMasukAudit,
            tipeJaminan, polis, caraMasuk, rujukan, dokter, layanan, karcis, request.PesertaJaminanId);
        var regAktif = RegAktifModel.CreateFromReg(reg);
        //      2-antrian
        var tglBerobat = DateOnly.FromDateTime(DateTime.Now);
        var jadwal = ResolveJadwalPraktek(dokter, request.JamPraktek, tglBerobat);
        var antrian = ResolveAntrian(tglBerobat, dokter, jadwal);
        var antrianMap = LoadOrCreateAntrianMap(jadwal, tglBerobat);
        var noAntrian = antrianMap.GetNextAntrian();
        var tracker = PasienTrackerModel.Create(reg);
        //      3-trs-billing-karcis
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var listKompKarcis = karcis.ListKomponen
            .Select(x => LoadKomponen(KomponenType.Key(x.KomponenTarif.KomponenId)))
            .ToList();
        var trsBillingReg = TrsBillingType.CreateFromRegistrasi(reg, karcis,
            jaminan, dokter, listKompKarcis);
        //      4-jurnal-karcis
        var mapJaminanJk = LoadMapJmnJk(tipeJaminan.Jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(trsBillingReg, layanan, mapJaminanJk);
        //      5-tindakan
        var tindakan =  karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter);
        //      6-trs-billing-tindakan
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var trsBilling = tindakan == TindakanModel.Default 
            ? TrsBillingType.Default
            : GenBill(tindakan, reg, tarif, jaminan);
        //      7-jurnal-tindakan
        var jurnalTindakan = tindakan == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        //      8-remote Cetak
        var appSetting = _getAppSettingSvc.Execute();
        var rmtCetak = new RemoteCetakType(
            reg.RegId, "RG-ANTRIAN", DateTime.Now,
            appSetting.Registrasi.RemoteCetakRegistrasi,
            false, new DateTime(3000, 1, 1),
            "", "");
        /*  ▐▀▀▀▀▀▀▀▀▀▀▀▌
            ▐   WRITE   ▌
            ▐▄▄▄▄▄▄▄▄▄▄▄▌*/

        RegJalanCreateResponse response;
        
        using (var trans = TransHelper.NewScope())
        {

            var antEntry = antrian.AddEntry(noAntrian, tracker, reg.RegId, "REG");
            var itemQueue = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == noAntrian)
                ?? AntrianEntryModel.Default;
            itemQueue.Serve();
            _regRepo.SaveChanges(reg);
            _regAktifRepo.SaveChanges(regAktif);
            _antrianRepo.SaveChanges(antrian);
            _trackerRepo.SaveChanges(tracker);
            //      ubah antrianMapHdr
            antrianMap.SetDataPasien(noAntrian, reg.Pasien, reg.ToReff(), reg.RegId, "AUTO");
            _antrianMapRepo.SaveChanges(antrianMap);
            _trsBillingRepo.SaveChanges(trsBillingReg);
            if (tindakan.TindakanId != "-")
                _tindakanRepo.SaveChanges(tindakan);
            if (trsBilling.TrsBillingId != "-")
                _trsBillingRepo.SaveChanges(trsBilling);
            _jurnalRepo.SaveChanges(jurnalReg);
            if (jurnalTindakan.JurnalId != "-")
                _jurnalRepo.SaveChanges(jurnalTindakan);
            _remoteCetakRepo.SaveChanges(rmtCetak);

            trans.Complete();
            response = new RegJalanCreateResponse(reg.RegId, antEntry.NoUrut);
        }
        AddReg(reg, noAntrian, jadwal);
        
        return Task.FromResult(response);
        
    }

    #region PRIVATE-HELPERS
    private bool IsPasienAktifReg(IPasienKey pasien)
    {
        return _regAktifRepo.IsPasienAktif(pasien)
            || _regRepo.IsPasienAktifReg(pasien);
    }
    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);

    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string rujukanId) =>
        caraMasuk == CaraMasukDkType.DatangSendiri
            ? RujukanType.Default
            : _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
                .GetValueOrThrow("'Rujukan' not found");

    private JadwalPraktekType ResolveJadwalPraktek(PpaType dokter, string jamPraktek, DateOnly tgl)
    {
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var listJadwalHari = listJadwal.Where(x => x.Hari == tgl.DayOfWeek)?.ToList() ?? [];

        return listJadwalHari.Count switch
        {
            1 => listJadwalHari.First(),
            > 1 => listJadwalHari.FirstOrDefault(x => x.JamMulai == TimeOnly.ParseExact(jamPraktek, "HH:mm", CultureInfo.InvariantCulture))
                ?? throw new ArgumentException($"Dokter tidak praktek pada jam {jamPraktek}"),
            _ => JadwalPraktekType.Default with
            {
                Dokter = dokter.ToReff(),
                Hari = tgl.DayOfWeek,
                JamMulai = TimeOnly.ParseExact("00:00:00", "HH:mm:ss", CultureInfo.InvariantCulture),
                JamSelesai = TimeOnly.ParseExact("23:59:59", "HH:mm:ss", CultureInfo.InvariantCulture)
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

    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        return polisView == null ? 
            throw new ArgumentException("Polis not found") 
            : _polisRepo.LoadEntity(polisView).Value;
    }

    private AntrianMapModel LoadOrCreateAntrianMap(JadwalPraktekType jadwal, DateOnly tglJadwal)
    {
        var queKey = AntrianMapModel.Key(jadwal.JadwalPraktekId,
            tglJadwal, jadwal.Dokter.PpaId,
            jadwal.Layanan.LayananId, jadwal.JamMulai);

        var result = _antrianMapRepo.LoadEntity(queKey).GetValueOrDefault(AntrianMapModel.Default);
        if (result.JadwalId == "-")
        {
            result = AntrianMapModel.Create(
                jadwal.JadwalPraktekId,
                jadwal.Dokter,
                jadwal.Layanan,
                tglJadwal,
                jadwal.JamMulai,
                jadwal.JamMulai,
                []);
            result.GenerateSlot(jadwal.MaxPasien);
        }
        return result;
    }

    private TindakanModel GenTindakan(RegModel reg, JaminanType jaminan, 
        KarcisType karcis, string userId, PpaType dokter)
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

    private TrsBillingType GenBill(TindakanModel tdk, RegModel reg, TarifType tarif,
        JaminanType jaminan)
    {
        var listKomp = new List<KomponenType>();
        foreach(var item in tdk.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.Komponen.KomponenId));
            listKomp.Add(komp);
        }
        var trsBilling = TrsBillingType.CreateFromTindakan(tdk, reg, tarif, jaminan, listKomp);
        return trsBilling;
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
                onNone: () => TarifType.Default
            );
        return tarif;
    }
    private MapJaminanJkType LoadMapJmnJk(IJaminanKey key)
    {
        var map = _mapJaminanJkRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => MapJaminanJkType.Default
            );
        return map;
    }

    private void AddReg(RegModel reg, int noAntrian, JadwalPraktekType jadwal)
    {
        var payload = new AddRegCmd(
            reg.RegId, "-", reg.Pasien.PasienId,
            reg.Pasien.PasienName, reg.Layanan.LayananId, 
            reg.Dokter.PpaId, reg.RegDate.ToString("yyyy-MM-dd"),
            jadwal.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture), 
            noAntrian);
        _addRegSvc.Execute(payload);
    }
    #endregion
}
