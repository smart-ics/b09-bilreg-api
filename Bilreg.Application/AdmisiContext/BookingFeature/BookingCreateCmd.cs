using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;


namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record BookingCreateCmd(string PasienName, string TglLahir, 
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
    public BookingCreateHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory, 
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
    }

    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        //      cek jadwal
        var dokter = PetugasMedisType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekRepo.ListData(dokter)?.ToList() ?? [];
        var jamMulai = TimeOnly.Parse(request.JamMulai);
        var jadwal = listJadwal.FirstOrDefault(x => x.JamMulai == jamMulai) 
                     ?? throw new ArgumentException("Jadwal tidak ditemukan");

        //  create person
        var tglLahir = DateOnly.ParseExact(request.TglLahir, "yyyy-MM-dd");
        var alamat = new AlamatType([request.Alamat], "-", "-");
        var contact = new ContactType(JenisContactEnum.Phone, request.NoTelp); 
        var person = new PersonInfoType(
            request.PasienName, tglLahir, request.Gender, 
            alamat, contact, IdentitasType.Default);

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

    private void ThrowExceptionIfTrackerExists(BookingModel booking)
    {
        var periodeVisit = new Periode(booking.TglBerobat.ToDateTime(TimeOnly.MinValue));
        var listTracker = _trackerRepo.ListData(periodeVisit, booking.Person.TglLahir);
        
        if (listTracker.Any())
            throw new ArgumentException("Tracker sudah ada");
    }
}