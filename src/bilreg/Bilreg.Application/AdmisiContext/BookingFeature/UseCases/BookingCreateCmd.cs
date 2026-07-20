using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
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

public record BookingCreateCmd(string PasienId, string PasienName, string TglLahir,
    string Gender, string Alamat, string NoTelp,
    string DokterId, string TglBerobat, string JamMulai, string UserId,
    bool IsForceDuplicatedTracker) : IRequest<BookingCreateResponse>;

public record BookingCreateResponse(string BookingId, int NoAntrian);

public class BookingCreateHandler : IRequestHandler<BookingCreateCmd, BookingCreateResponse>
{
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    private readonly IBookingRepo _bookingRepo;
    private readonly IPasienTrackerRepo _trackerRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IAntrianMapRepo _antrianMapRepo;
    private readonly IAddAntrianEmrByBookingService _addAntrianEmrByBookingService;
    private readonly IAntrianMapWithBookingResolver _antrianMapWithBookingResolver;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly ITglJamProvider _tglJamProvider;

    public BookingCreateHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory,
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo,
        IPasienRepo pasienRepo, IAntrianMapRepo antrianMapRepo,
        IAddAntrianEmrByBookingService addAntrianEmrByBookingService, 
        IAntrianMapWithBookingResolver antrianMapWithBookingResolver,
        IJadwalPraktekFeatureResolver featureResolver,
        ITglJamProvider? tglJamProvider = null)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
        _pasienRepo = pasienRepo;
        _antrianMapRepo = antrianMapRepo;
        _addAntrianEmrByBookingService = addAntrianEmrByBookingService;
        _antrianMapWithBookingResolver = antrianMapWithBookingResolver;
        _featureResolver = featureResolver;
        _tglJamProvider = tglJamProvider;
    }

    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
        //  GUARD
        if (request.PasienId.Trim() != string.Empty && request.PasienName.Trim() != string.Empty)
            throw new ArgumentException("Kosongkan PasienName jika booking menggunakan PasienId");
        //      cek jadwal
        var dokter = PpaType.Key(request.DokterId);
        var tglBerobat = DateOnly.Parse(request.TglBerobat);
        var jamMulai = TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture);
        var schedule = BookingScheduleResolver.Resolve(
            _featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, jamMulai);

        //  create person
        var px = request.PasienId != string.Empty ?
            FindPasien(request) :
            PasienModel.Default;

        var person = request.PasienId == string.Empty ?
            CreatePerson(request) :
            px.Person;

        //  create booking
        var booking = _featureResolver.UseResolver
            ? BookingModel.CreateLocalFromEffective(person, schedule.Effective, request.UserId, occurredAt)
            : BookingModel.CreateLocal(person, tglBerobat, schedule.LegacyJadwal, request.UserId, occurredAt);

        //  ambil nomor antrian
        var listAntrian = _antrianRepo.ListData(tglBerobat);
        var sequenceTag = _featureResolver.UseResolver
            ? AntrianModel.GenSequenceTag(tglBerobat, schedule.Effective)
            : AntrianModel.GenSequenceTag(tglBerobat, schedule.LegacyJadwal);
        var antrianView = listAntrian.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var antrian = antrianView is null
            ? (_featureResolver.UseResolver
                ? _antrianFactory.Create(tglBerobat, schedule.Effective)
                : _antrianFactory.Create(tglBerobat, schedule.LegacyJadwal))
            : _antrianRepo.LoadEntity(antrianView).Value;

        if (!request.IsForceDuplicatedTracker)
            ThrowExceptionIfTrackerExists(booking);
        var tracker = PasienTrackerModel.Create(booking, occurredAt);

        // antrianMap
        var antrianMap = _antrianMapWithBookingResolver.Resolve(
            schedule.LegacyJadwal, tglBerobat, booking, px);


        //  persisting
        BookingCreateResponse response;
        using (var trans = TransHelper.NewScope())
        {
            //      no antrian masuk ke transaction agar bisa rollback jika gagal
            var antEntry = antrian.AddEntry(antrianMap.Value.Item2.NoUrut, tracker, booking.BookingId, "BOK", occurredAt);
            booking.AssignNoAntrian(antEntry.NoUrut);

            //      writing database
            _bookingRepo.SaveChanges(booking);
            _antrianRepo.SaveChanges(antrian);
            _trackerRepo.SaveChanges(tracker);
            _antrianMapRepo.SaveChanges(antrianMap.Value.Item1);

            trans.Complete();
            response = new BookingCreateResponse(booking.BookingId, antEntry.NoUrut);
        }

        AddAntrianEmrByBooking(booking, px);

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

    private void AddAntrianEmrByBooking(BookingModel book, PasienModel px)
    {
        var pasienId = px.PasienId == "-" ? "-" : px.PasienId;
        var payload = new AddAntrianEmrByBookingCmd(book.BookingId, pasienId, book.Person.PersonName,
            book.Layanan.LayananId, book.Dokter.PpaId, book.TglBerobat.ToString("yyyy-MM-dd"),
            book.JamPraktek.ToString("HH:mm", CultureInfo.InvariantCulture), book.NoAntrian);
        _addAntrianEmrByBookingService.Execute(payload);
    }



}
