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
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Application.Shared.Helpers;
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
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanByBookingCmd(string BookingId, string UserId, string KarcisId, 
    string CaraMasukDkId, string TipeJaminanId) : IRequest<RegJalanByBookingResponse>;

public record RegJalanByBookingResponse(string RegId, int NoAntrian);
public class RegJalanByBookingHandler 
    : IRequestHandler<RegJalanByBookingCmd, RegJalanByBookingResponse>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IRegFactory _regFactory;
    private readonly IPpaRepo _dokterRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;
    private readonly ICaraMasukDkRepo _caraMasukDkRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IPolisRepo _polisRepo;
    private readonly IRujukanRepo _rujukanRepo;
    
    private readonly IJaminanRepo _jaminanRepo;
    private readonly ITarifRepo _tarifRepo;
    private readonly INilaiTarifRepo _nilaiTarifRepo;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;
    private readonly IKomponenRepo _komponenRepo;
    private readonly ITrsBillingRepo _trsBillingRepo;
    
    private readonly IRemoteCetakRepo _remoteCetakRepo;
    private readonly IGetAppSettingService _getAppSettingSvc;

    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IJurnalRepo _jurnalRepo;

    private const string BAYAR_SENDIRI = "1";
    public RegJalanByBookingHandler(
        IBookingRepo bookingRepo,
        IPasienRepo pasienRepo,
        IRegFactory regFactory,
        IPpaRepo dokterRepo,
        ILayananRepo layananRepo,
        IKarcisRepo karcisRepo,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo,
        ICaraMasukDkRepo caraMasukDkRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IPolisRepo polisRepo,
        IRujukanRepo rujukanRepo,
        IJaminanRepo jaminanRepo,
        ITarifRepo tarifRepo,
        INilaiTarifRepo nilaiTarifRepo,
        ITipeTarifRepo tipeTarifRepo,
        ITindakanRepo tindakanRepo,
        IKomponenRepo komponenRepo,
        ITrsBillingRepo trsBillingRepo,
        IAntrianRepo antrianRepo,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IJurnalRepo jurnalRepo,
        IRemoteCetakRepo remoteCetakRepo,
        IGetAppSettingService getAppSettingSvc)
    {
        _bookingRepo = bookingRepo;
        _pasienRepo = pasienRepo;
        _regFactory = regFactory;
        _dokterRepo = dokterRepo;
        _layananRepo = layananRepo;
        _karcisRepo = karcisRepo;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
        _caraMasukDkRepo = caraMasukDkRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _polisRepo = polisRepo;
        _rujukanRepo = rujukanRepo;
        _jaminanRepo = jaminanRepo;
        _tarifRepo = tarifRepo;
        _nilaiTarifRepo = nilaiTarifRepo;
        _tipeTarifRepo = tipeTarifRepo;
        _tindakanRepo = tindakanRepo;
        _komponenRepo = komponenRepo;
        _trsBillingRepo = trsBillingRepo;
        _antrianRepo = antrianRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _jurnalRepo = jurnalRepo;
        _remoteCetakRepo = remoteCetakRepo;
        _getAppSettingSvc = getAppSettingSvc;
    }

    public Task<RegJalanByBookingResponse> Handle(RegJalanByBookingCmd request, CancellationToken cancellationToken)
    {
        //  LOAD and GUARD
        var booking = LoadBooking(request.BookingId);
        var antrian = LoadAntrian(booking);
        var pasien = LoadPasien(booking.PasienId);
        if (_regAktifRepo.IsPasienAktif(pasien))
            throw new KeyNotFoundException($"Pasien aktif sudah aktif registrasi");
        var dokter = LoadDokter(booking.Dokter.PpaId);
        var layanan = LoadLayanan(booking.Layanan.LayananId); 
        var karcis = LoadKarcis(request.KarcisId);
        var caraMasuk = LoadCaraMasuk(request.CaraMasukDkId);
        var tipeJaminan = LoadTipeJaminan(request.TipeJaminanId);
        var polis = ResolvePolis(pasien, tipeJaminan);
        var rujukan = RujukanType.Default;
        if (!booking.CoverageInfo.NoRujukan.IsNullOrEmpty())
            rujukan = ResolveRujukan(caraMasuk, booking.CoverageInfo.NoRujukan);
        
        //  BUILD
        var regAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajal(
            pasien, regAudit, tipeJaminan,
            polis, caraMasuk, rujukan,
            dokter, layanan, karcis);
        booking.AssignReg(reg);

        var regAktif = new RegAktifModel(reg.RegId,  reg.RegDate, 
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);

        //      BUILD TrsBill Reg
        //  ANTRIAN
        var itemQueue = antrian.ListEntry.FirstOrDefault(x => x.NoUrut == booking.NoAntrian) 
            ?? AntrianEntryModel.Default;
        itemQueue.SetReff(reg.RegId, "REG");
        itemQueue.Serve();

        //      BUILD TINDAKAN
        var jaminan = LoadJaminan(tipeJaminan.Jaminan);
        var listKompKarcis = new List<KomponenType>();
        foreach (var item in karcis.ListKomponen)
        {
            var komp = LoadKomponen(KomponenType.Key(item.KomponenTarif.KomponenId));
            listKompKarcis.Add(komp);
        }
        var trsBillingReg = TrsBillingType.CreateFromRegistrasi(reg, karcis,
            jaminan, dokter, listKompKarcis);

        var tindakan = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TindakanModel.Default
            : GenTindakan(reg, jaminan, karcis, request.UserId, dokter);


        //     BUILD Jurnal Reg
        var mapJaminanJk = LoadMapJmnJk(tipeJaminan.Jaminan);
        var jurnalReg = JurnalType.CreateFromTrsBilling(trsBillingReg, 
            layanan, mapJaminanJk);

        
        //      BUILD TrsBill
        var tarif = karcis.DefaultTarif == TarifType.Default.ToReff()
            ? TarifType.Default
            : LoadTarif(TarifType.Key(karcis.DefaultTarif.TarifId));
        var trsBilling = tindakan == TindakanModel.Default
            ? TrsBillingType.Default
            : GenBill(tindakan, reg, tarif, jaminan);

        //      BUILD Jurnal Tindakan
        var jurnalTindakan = tindakan == TindakanModel.Default
            ? JurnalType.Default
            : JurnalType.CreateFromTrsBilling(trsBilling,
                layanan, mapJaminanJk);
        //      REMOTE-CETAK
        var appSetting = _getAppSettingSvc.Execute();
        var rmtCetak = new RemoteCetakType(
            reg.RegId, "RG-ANTRIAN", DateTime.Now, 
            appSetting.Registrasi.RemoteCetakRegistrasi, 
            false, new DateTime(3000, 1, 1),
            "", "");


        //  WRITE
        using var trans = TransHelper.NewScope();
        _regRepo.SaveChanges(reg);
        _bookingRepo.SaveChanges(booking);
        _regAktifRepo.SaveChanges(regAktif);
        _trsBillingRepo.SaveChanges(trsBillingReg);
        _antrianRepo.SaveChanges(antrian);
        if (tindakan.TindakanId != "-")
            _tindakanRepo.SaveChanges(tindakan);
        if (trsBilling.TrsBillingId != "-")
            _trsBillingRepo.SaveChanges(trsBilling);

        _jurnalRepo.SaveChanges(jurnalReg);
        if (jurnalTindakan.JurnalId != "-")
            _jurnalRepo.SaveChanges(jurnalTindakan);

        _remoteCetakRepo.SaveChanges(rmtCetak);
        trans.Complete();
        return Task.FromResult(new RegJalanByBookingResponse(reg.RegId, booking.NoAntrian));
    }

    #region PRIVATE HELPER
    private BookingModel LoadBooking(string id)
    {
        var booking = _bookingRepo.LoadEntity(BookingModel.Key(id))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Booking {id} tidak ditemukan"));
        if (booking.HasBeenRegistered())
            throw new ArgumentException($"Booking {id} sudah teregister di {booking.Reg.RegId}");
        return booking;
    }

    private PasienModel LoadPasien(string id) =>
        _pasienRepo.LoadEntity(PasienModel.Key(id))
            .GetValueOrThrow("Pasien tidak ditemukan");

    private PpaType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PpaType.Key(id))
            .GetValueOrThrow("Dokter tidak valid");

    private LayananType LoadLayanan(string id) =>
        _layananRepo.LoadEntity(LayananType.Key(id))
            .GetValueOrThrow("Layanan tidak valid");

    private KarcisType LoadKarcis(string id) =>
        _karcisRepo.LoadEntity(KarcisType.Key(id))
            .GetValueOrThrow("Karcis tidak valid");

    private CaraMasukDkType LoadCaraMasuk(string id) =>
        _caraMasukDkRepo.LoadEntity(CaraMasukDkType.Key(id))
            .GetValueOrThrow("'Cara Masuk' not found");

    private TipeJaminanType LoadTipeJaminan(string id) =>
        _tipeJaminanRepo.LoadEntity(TipeJaminanType.Key(id))
            .GetValueOrThrow("Tipe Jaminan invalid");

    private PolisModel ResolvePolis(PasienModel pasien, TipeJaminanType tipeJaminan) =>
        tipeJaminan.CaraBayarDk.CaraBayarDkId == BAYAR_SENDIRI
            ? PolisModel.Default
            : FindPolis(pasien, tipeJaminan);
    private PolisModel FindPolis(PasienModel pasien, TipeJaminanType tipeJaminan)
    {
        var listPolis = _polisRepo.ListData(pasien);
        var polisView = listPolis.FirstOrDefault(x => x.TipeJaminan == tipeJaminan.ToReff());
        if (polisView == null)
            throw new ArgumentException("Polis not found");
        return _polisRepo.LoadEntity(polisView).Value;
    }
    private RujukanType ResolveRujukan(CaraMasukDkType caraMasuk, string rujukanId) =>
        caraMasuk == CaraMasukDkType.DatangSendiri
            ? RujukanType.Default
            : _rujukanRepo.LoadEntity(RujukanType.Key(rujukanId))
                .GetValueOrThrow("'Rujukan' not found");

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
    
    private AntrianModel LoadAntrian(BookingModel booking)
    {
        var ppa = LoadDokter(booking.Dokter.PpaId);
        var date = DateOnly.FromDateTime(DateTime.Now);
        var listQueue = _antrianRepo.ListData(date)?.ToList() ?? [];
        var squesceTag = AntrianModel.GenSequenceTag(date, booking.JamPraktek, ppa);
        var antrian = listQueue.FirstOrDefault(x => x.SequenceTag == squesceTag) 
            ?? new AntrianHeaderView("-", "-", DateOnly.MinValue, TimeOnly.MinValue, "");

        var result = _antrianRepo.LoadEntity(antrian).GetValueOrThrow("Antrian not found");
        return result;
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
    #endregion
}
