using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiContext.RujukanFeature;
using Bilreg.Application.ChargeContext.TarifFeature;
using Bilreg.Application.ChargeContext.TindakanFeature;
using Bilreg.Application.ChargeContext.TindakanFeature.TindakanAgg;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RujukanFeature;
using Bilreg.Domain.BillContext.TindakanSub.TindakanAgg;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
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
    private readonly ITindakanFactory _tdkFactory;
    private readonly ITipeTarifRepo _tipeTarifRepo;
    private readonly ITindakanRepo _tindakanRepo;

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
        ITindakanFactory tdkFactory,
        ITipeTarifRepo tipeTarifRepo,
        ITindakanRepo tindakanRepo)
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
        _tdkFactory = tdkFactory;
        _tipeTarifRepo = tipeTarifRepo;
        _tindakanRepo = tindakanRepo;
    }

    public Task<RegJalanByBookingResponse> Handle(RegJalanByBookingCmd request, CancellationToken cancellationToken)
    {
        //  LOAD and GUARD
        var booking = LoadBooking(request.BookingId);
        var pasien = LoadPasien(booking.PasienId);
        var dokter = LoadDokter(booking.Dokter.PpaId);
        var layanan = LoadLayanan(booking.Layanan.LayananId); 
        var karcis = LoadKarcis(request.KarcisId);
        var caraMasuk = LoadCaraMasuk(request.CaraMasukDkId);
        var tipeJaminan = LoadTipeJaminan(request.TipeJaminanId);
        var polis = ResolvePolis(pasien, tipeJaminan);
        var rujukan = RujukanType.Default;
        if (!booking.CoverageInfo.NoRujukan.IsNullOrEmpty())
        {
            rujukan = ResolveRujukan(caraMasuk, booking.CoverageInfo.NoRujukan);
        }

        //  BUILD
        var regAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajal(
            pasien,
            regAudit,
            tipeJaminan,
            polis,
            caraMasuk,
            rujukan,
            dokter,
            layanan,
            karcis);
        booking.AssignReg(reg);

        var regAktif = new RegAktifModel(reg.RegId,  reg.RegDate.ToDateTime(TimeOnly.MinValue), 
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);

        var tindakan = TindakanModel.Default;
        if (karcis.DefaultTarif.TarifId != "-")
            tindakan = GenTindakan(reg, tipeJaminan, karcis.DefaultTarif, pasien, layanan, request.UserId);

        //  WRITE
        using var trans = TransHelper.NewScope();
        _regRepo.SaveChanges(reg);
        _bookingRepo.SaveChanges(booking);
        _regAktifRepo.SaveChanges(regAktif);

        if (karcis.DefaultTarif.TarifId != "-")
            _tindakanRepo.SaveChanges(tindakan);

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

    private TindakanModel GenTindakan(RegModel reg, TipeJaminanType tipeJaminan, TarifReff tarifReff,
        PasienModel pasien, LayananType layanan, string userId)
    {
        var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(tipeJaminan.Jaminan.JaminanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Jaminan {tipeJaminan.Jaminan.JaminanId} not found")
            );

        var tipeTarifJmn = jaminan.ListTipeTarif
            ?.FirstOrDefault(x => x.JenisRegid == JenisRegEnum.RegJalan)
            ?? throw new InvalidOperationException(
                $"Tipe tarif untuk RegJalan tidak ditemukan pada jaminan {jaminan.JaminanId}");

        var tarif = _tarifRepo.LoadEntity(TarifType.Key(tarifReff.TarifId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"Tarif {tarifReff.TarifId} not found")
            );

        var tipeTarif = _tipeTarifRepo.LoadEntity(
                TipeTarifType.Key(tipeTarifJmn.TipeTarif.TipeTarifId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException(
                    $"TipeTarif {tipeTarifJmn.TipeTarif.TipeTarifId} not found")
            );

        var nilaiTarifKey = NilaiTarifType.KeyComposite(
            tarif.TarifId,
            tipeTarif.TipeTarifId,
            reg.Kelas.KelasId);

        var nilaiTarif = _nilaiTarifRepo.LoadEntity(nilaiTarifKey)
        .Match(
            onSome: x => x,
            onNone: () => throw new KeyNotFoundException(
                $"NilaiTarif Tarif:{tarif.TarifId}, Tipe:{tipeTarif.TipeTarifId}, Kelas:{reg.Kelas.KelasId} not found")
        );

        var tarifTdk = new TindakanTarifDto(
            tarif.ToReff(),
            nilaiTarif.ListKomponen.Select(x => new TindakanTarifKompomnenDto(
                x.Komponen.KomponenId, reg.Dokter.PpaId, 1)));

        var tdkTarif = _tdkFactory.BuildTindakanTarif(tarif, nilaiTarif, tarifTdk);

        var tindakan = _tdkFactory.Create(JenisTindakanEnum.Tindakan, OrderTdkModel.Default,
            pasien, reg, layanan, tipeTarif, tdkTarif, userId);


        return tindakan;
    }

    #endregion
}
