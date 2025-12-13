using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingCreateFromHidokCommand(
    string PasienId, string PasienName, string TglLahir,
    string Gender, string Alamat, string NoTelp,
    string DokterEmail, string TglBerobat, string JamMulai, int NoAntrian, 
    string AsuransiName, string NoPeserta, string NoRujukan,
    string ReffId, string UserId) : IRequest<BookingCreateFromHidokResponse>;

public record BookingCreateFromHidokResponse(string BookingId, int NoAntrian);

public class BookingCreateFromHidokHandler : IRequestHandler<BookingCreateFromHidokCommand, BookingCreateFromHidokResponse>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IPpaRepo _ppaRepo;

    public BookingCreateFromHidokHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory,
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo,
        IPasienRepo pasienRepo, IPpaRepo ppaRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
        _pasienRepo = pasienRepo;
        _ppaRepo = ppaRepo;
    }
    public Task<BookingCreateFromHidokResponse> Handle(BookingCreateFromHidokCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        if (request.PasienId.Trim() != string.Empty && request.PasienName.Trim() != string.Empty)
            throw new ArgumentException("Kosongkan PasienName jika booking menggunakan PasienId");
        //      cek jadwal
        var finder = new ContactFinder(JenisContactEnum.Email, request.DokterEmail);
        var dokter = _ppaRepo.LoadEntity(finder)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterEmail} not found")
                );
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var hari = DateOnly.Parse(request.TglBerobat).DayOfWeek;
        var jamMulai = TimeOnly.Parse(request.JamMulai);
        var jadwal = listJadwal
             .Where(x => x.Hari == hari)
             .FirstOrDefault(x => x.JamMulai == jamMulai)
            ?? throw new ArgumentException("Jadwal tidak ditemukan");

        //      create person
        var person = request.PasienId == string.Empty ?
            CreatePerson(request) :
            FindPasien(request);

        //      create booking
        var tglBerobat = DateOnly.Parse(request.TglBerobat);
        var extApp = new ExtAppReffType("HiDok", request.ReffId, "");
        var coverage = new CoverageInfoType(request.AsuransiName, request.NoPeserta, request.NoRujukan);
        var booking = BookingModel.CreateFromExternal(person, tglBerobat, jadwal, extApp, coverage, request.UserId);

        //      ambil nomor antrian
        var listAntrian = _antrianRepo.ListData(tglBerobat);
        var sequenceTag = AntrianModel.GenSequenceTag(tglBerobat, jadwal);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var antrian = antrianView is null ?
            _antrianFactory.Create(tglBerobat, jadwal) :
            _antrianRepo.LoadEntity(antrianView).Value;

        var tracker = PasienTrackerModel.Create(booking);

        //  WRITE
        using var trans = TransHelper.NewScope();

        var antEntry = antrian.AddEntry(request.NoAntrian, tracker);
        booking.AssignNoAntrian(antEntry.NoUrut);

        _bookingRepo.SaveChanges(booking);
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);

        trans.Complete();

        return Task.FromResult(new BookingCreateFromHidokResponse(
            booking.BookingId, antEntry.NoUrut));
    }

    private PersonInfoType FindPasien(BookingCreateFromHidokCommand request)
    {
        var pasienKey = PasienModel.Key(request.PasienId);
        var pasien = _pasienRepo.LoadEntity(pasienKey)
            .Match(
                onSome: x => x.Person,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
        return pasien;
    }

    private static PersonInfoType CreatePerson(BookingCreateFromHidokCommand request)
    {
        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var alamat = new AlamatType([request.Alamat], "-", "-");
        var contact = new ContactType(JenisContactEnum.Phone, request.NoTelp);
        var person = new PersonInfoType(
            request.PasienName, tglLahir, request.Gender,
            alamat, contact, IdentitasType.Default);
        return person;
    }
}
