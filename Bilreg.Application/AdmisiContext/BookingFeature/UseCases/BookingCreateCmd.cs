using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingCreateCmd(string PasienId,string PasienName, string TglLahir, 
    string Gender, string Alamat, string NoTelp, 
    string DokterId, string TglBerobat, string JamMulai, string UserId,
    bool IsForceDuplicatedTracker) : IRequest<BookingCreateResponse>;

public record BookingCreateResponse (string BookingId, int NoAntrian);

public class BookingCreateHandler : IRequestHandler<BookingCreateCmd, BookingCreateResponse>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IAntrianMapHdrRepo _antrianMapRepo;
    private readonly IDashboardAddBookService _addBookingSvc;
    public BookingCreateHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory,
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo,
        IPasienRepo pasienRepo, IAntrianMapHdrRepo antrianMapRepo, 
        IDashboardAddBookService addBookingSvc)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
        _pasienRepo = pasienRepo;
        _antrianMapRepo = antrianMapRepo;
        _addBookingSvc = addBookingSvc;
    }

    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        if (request.PasienId.Trim() != string.Empty && request.PasienName.Trim() != string.Empty)
            throw new ArgumentException("Kosongkan PasienName jika booking menggunakan PasienId");
        //      cek jadwal
        var dokter = PpaType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var hari = DateOnly.Parse(request.TglBerobat).DayOfWeek;
        var jamMulai = TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var jadwal = listJadwal
             .Where(x => x.Hari == hari)
             .FirstOrDefault(x => x.JamMulai == jamMulai) 
            ?? throw new ArgumentException("Jadwal tidak ditemukan");

        //  create person
        var px = request.PasienId != string.Empty ?
            FindPasien(request) :
            PasienModel.Default;

        var person = request.PasienId == string.Empty ? 
            CreatePerson(request) : 
            px.Person;

        //  create booking
        var tglBerobat = DateOnly.Parse(request.TglBerobat);
        var booking = BookingModel.CreateLocal(person, tglBerobat, jadwal, request.UserId);
        
        //  ambil nomor antrian
        var listAntrian = _antrianRepo.ListData(tglBerobat);
        var sequenceTag = AntrianModel.GenSequenceTag(tglBerobat, jadwal);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var antrian = antrianView is null ? 
            _antrianFactory.Create(tglBerobat, jadwal) :
            _antrianRepo.LoadEntity(antrianView).Value;

        if (!request.IsForceDuplicatedTracker)
            ThrowExceptionIfTrackerExists(booking);
        var tracker = PasienTrackerModel.Create(booking);

        // antrianMap
        var antrianMap = CekAntrianMap(jadwal, tglBerobat);
        var noAntrian = antrianMap.GetNextNoAntrian("UMUM");
        var pasien = new PasienReff(request.PasienId, person.PersonName, person.TglLahir, person.Gender);


        //  persisting
        BookingCreateResponse response;
        using (var trans = TransHelper.NewScope())
        {
            //      no antrian masuk ke transaction agar bisa rollback jika gagal
            var antEntry = antrian.AddEntry(noAntrian, tracker, booking.BookingId, "BOK");
            booking.AssignNoAntrian(antEntry.NoUrut);

            //      writing database
            _bookingRepo.SaveChanges(booking);
            _antrianRepo.SaveChanges(antrian);
            _trackerRepo.SaveChanges(tracker);

            // rubah antrianMapHdr
            antrianMap.SetDataPasien(noAntrian, pasien, booking.Reg, booking.BookingId, "UMUM");
            _antrianMapRepo.SaveChanges(antrianMap);

            trans.Complete();
            response = new BookingCreateResponse(booking.BookingId, antEntry.NoUrut);
        }

        AddBooking(booking, px);

        return Task.FromResult(response);
    }

    private PasienModel FindPasien(BookingCreateCmd request)
    {
        var pasienKey = PasienModel.Key(request.PasienId);
        var pasien = _pasienRepo.LoadEntity(pasienKey).GetValueOrDefault(PasienModel.Default);
        return pasien;
    }
    
    private static PersonInfoType CreatePerson(BookingCreateCmd request)
    {
        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var alamat = new AlamatType([request.Alamat], "-", "-");
        var contact = new ContactType(JenisContactEnum.Phone, request.NoTelp); 
        var person = new PersonInfoType(
            request.PasienName, tglLahir, request.Gender, 
            alamat, contact, IdentitasType.Default);
        return person;
    }

    private void ThrowExceptionIfTrackerExists(BookingModel booking)
    {
        var periodeVisit = new Periode(booking.TglBerobat.ToDateTime(TimeOnly.MinValue));
        var listTracker = _trackerRepo.ListData(periodeVisit, booking.Person.TglLahir)?.ToList() 
                          ?? [];
        var personNameEyd = booking.Person.PersonName.ToEyd();
        var duplicated = listTracker
            .FirstOrDefault(x => x.Person.PersonName.ToEyd() == personNameEyd);

        if (duplicated is not null)
            throw new ArgumentException("Pasien terdeteksi di tracker. Booking terduplikasi");
    }

    private AntrianMapHdrModel CekAntrianMap(JadwalPraktekType jadwal, DateOnly tglJadwal)
    {
        var queKey = AntrianMapHdrModel.Key(jadwal.JadwalPraktekId,
            tglJadwal, jadwal.Dokter.PpaId,
            jadwal.Layanan.LayananId, jadwal.JamMulai);

        var result = _antrianMapRepo.LoadEntity(queKey).GetValueOrDefault(AntrianMapHdrModel.Default);
        if(result.JadwalId == "-")
        {
            result = AntrianMapHdrModel.Create(
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

    private void  AddBooking(BookingModel book, PasienModel px)
    {
        var pasienId = px.PasienId == "-" ? "-" : px.PasienId;
        var payload = new AddBookCmd(book.BookingId, pasienId, book.Person.PersonName,
            book.Layanan.LayananId, book.Dokter.PpaId, book.TglBerobat.ToString("yyyy-MM-dd"),
            book.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture), book.NoAntrian);
        _addBookingSvc.Execute(payload);
    }



}