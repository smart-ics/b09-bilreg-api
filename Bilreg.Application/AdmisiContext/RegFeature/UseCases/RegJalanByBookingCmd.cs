using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
using Bilreg.Application.AdmisiContext.RegSub;
using Bilreg.Application.PasienContext.PasienFeature;
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
using Nuna.Lib.ValidationHelper;
using System.Net.Http.Headers;

namespace Bilreg.Application.AdmisiContext.RegFeature.UseCases;

public record RegJalanByBookingCmd(string BookingId, string UserId, string KarcisId) : IRequest<RegJalanByBookingResponse>;

public record RegJalanByBookingResponse(string RegId, int NoAntrian);
public class RegJalanByBookingHandler 
    : IRequestHandler<RegJalanByBookingCmd, RegJalanByBookingResponse>
{
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IRegFactory _regFactory;
    private readonly IPetugasMedisRepo _dokterRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IKarcisRepo _karcisRepo;
    private readonly IRegRepo _regRepo;
    private readonly IRegAktifRepo _regAktifRepo;

    public RegJalanByBookingHandler(
        IBookingRepo bookingRepo,
        IPasienRepo pasienRepo,
        IRegFactory regFactory,
        IPetugasMedisRepo dokterRepo,
        ILayananRepo layananRepo,
        IKarcisRepo karcisRepo,
        IRegRepo regRepo,
        IRegAktifRepo regAktifRepo)
    {
        _bookingRepo = bookingRepo;
        _pasienRepo = pasienRepo;
        _regFactory = regFactory;
        _dokterRepo = dokterRepo;
        _layananRepo = layananRepo;
        _karcisRepo = karcisRepo;
        _regRepo = regRepo;
        _regAktifRepo = regAktifRepo;
    }

    public Task<RegJalanByBookingResponse> Handle(RegJalanByBookingCmd request, CancellationToken cancellationToken)
    {
        //  LOAD and GUARD
        var booking = LoadBooking(request.BookingId);
        var pasien = LoadPasien(booking.PasienId);
        var dokter = LoadDokter(booking.Dokter.PetugasMedisId);
        var layanan = LoadLayanan(booking.Layanan.LayananId); 
        var karcis = LoadKarcis(request.KarcisId);
        
        //  BUILD
        var regAudit = new AuditInfoType(request.UserId, DateTime.Now);
        var reg = _regFactory.CreateRegRajal(
            pasien,
            regAudit,
            TipeJaminanType.BayarSendiri,
            PolisModel.Default,
            CaraMasukDkType.Default,
            RujukanType.Default,
            dokter,
            layanan,
            karcis);
        booking.AssignReg(reg);

        var regAktif = new RegAktifModel(reg.RegId,  reg.RegDate.ToDateTime(TimeOnly.MinValue), 
            reg.Pasien, reg.JenisReg, reg.Layanan,
            reg.Dokter, reg.TipeJaminan);
        
        //  WRITE
        using var trans = TransHelper.NewScope();
        _regRepo.SaveChanges(reg);
        _bookingRepo.SaveChanges(booking);
        _regAktifRepo.SaveChanges(regAktif);
        trans.Complete();
        return Task.FromResult(new RegJalanByBookingResponse(reg.RegId, booking.NoAntrian));
    }

    //  PRIVATE HELPER
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

    private PetugasMedisType LoadDokter(string id) =>
        _dokterRepo.LoadEntity(PetugasMedisType.Key(id))
            .GetValueOrThrow("Dokter tidak valid");

    private LayananType LoadLayanan(string id) =>
        _layananRepo.LoadEntity(LayananType.Key(id))
            .GetValueOrThrow("Layanan tidak valid");

    private KarcisType LoadKarcis(string id) =>
        _karcisRepo.LoadEntity(KarcisType.Key(id))
            .GetValueOrThrow("Karcis tidak valid");
}
