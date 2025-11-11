using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.Helpers;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature.UseCases;

public record BookingCreateCmd(string PasienId,string PasienName, string TglLahir, 
    string Gender, string Alamat, string NoTelp, 
    string DokterId, string TglBerobat, string JamMulai,
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
    public BookingCreateHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory, 
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo, 
        IPasienRepo pasienRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
        _pasienRepo = pasienRepo;
    }

    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        if (request.PasienId.Trim() != string.Empty && request.PasienName.Trim() != string.Empty)
            throw new ArgumentException("Kosongkan PasienName jika booking menggunakan PasienId");
        //      cek jadwal
        var dokter = PetugasMedisType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var hari = DateOnly.Parse(request.TglBerobat).DayOfWeek;
        var jamMulai = TimeOnly.Parse(request.JamMulai);
        var jadwal = listJadwal
             .Where(x => x.Hari == hari)
             .FirstOrDefault(x => x.JamMulai == jamMulai) 
            ?? throw new ArgumentException("Jadwal tidak ditemukan");

        //  create person
        var person = request.PasienId == string.Empty ? 
            CreatePerson(request) : 
            FindPasien(request);

        //  create booking
        var tglBerobat = DateOnly.Parse(request.TglBerobat);
        var booking = BookingModel.Create(person, tglBerobat, jadwal);
        
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

        //  persisting
        using var trans = TransHelper.NewScope();
        //      no antrian masuk ke transaction agar bisa rollback jika gagal
        var antEntry = antrian.AddEntry(tracker);
        booking.AssignNoAntrian(antEntry.NoUrut);
        //      writing database
        _bookingRepo.SaveChanges(booking);
        _antrianRepo.SaveChanges(antrian);
        _trackerRepo.SaveChanges(tracker);

        trans.Complete();

        return Task.FromResult(new BookingCreateResponse(
            booking.BookingId, antEntry.NoUrut));
    }

    private PersonInfoType FindPasien(BookingCreateCmd request)
    {
        var pasienKey = PasienModel.Key(request.PasienId);
        var pasien = _pasienRepo.LoadEntity(pasienKey)
            .Match(
                onSome: x => x.Person,
                onNone: () => throw new KeyNotFoundException($"Pasien id {request.PasienId} not found")
            );
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
}