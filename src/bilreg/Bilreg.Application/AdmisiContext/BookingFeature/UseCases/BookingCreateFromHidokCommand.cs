using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

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
    private readonly IAddAntrianEmrByBookingService _addBookingSvc;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;
    private readonly ITglJamProvider _tglJamProvider;

    public BookingCreateFromHidokHandler(IJadwalPraktekRepo jadwalPraktekRepo,
        IAntrianRepo antrianRepo, IAntrianFactory antrianFactory,
        IBookingRepo bookingRepo, IPasienTrackerRepo trackerRepo,
        IPasienRepo pasienRepo, IPpaRepo ppaRepo, 
        IAddAntrianEmrByBookingService addBookingSvc,
        IJadwalPraktekFeatureResolver featureResolver,
        ITglJamProvider tglJamProvider)
    {
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
        _bookingRepo = bookingRepo;
        _trackerRepo = trackerRepo;
        _pasienRepo = pasienRepo;
        _ppaRepo = ppaRepo;
        _addBookingSvc = addBookingSvc;
        _featureResolver = featureResolver;
        _tglJamProvider = tglJamProvider;
    }
    public Task<BookingCreateFromHidokResponse> Handle(BookingCreateFromHidokCommand request, CancellationToken cancellationToken)
    {
        var occurredAt = _tglJamProvider.Now;
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
        var tglBerobat = DateOnly.Parse(request.TglBerobat);
        var jamMulai = TimeOnly.Parse(request.JamMulai);
        var schedule = BookingScheduleResolver.Resolve(
            _featureResolver, _jadwalPraktekRepo, dokter, tglBerobat, jamMulai);

        //      create person
        var px = request.PasienId != string.Empty ?
            FindPasien(request) :
            PasienModel.Default;

        var person = request.PasienId == string.Empty ?
            CreatePerson(request) :
            px.Person;

        var extApp = new ExtAppReffType("HiDok", request.ReffId, "");
        var coverage = new CoverageInfoType(request.AsuransiName, request.NoPeserta, request.NoRujukan);
        var booking = _featureResolver.UseResolver
            ? BookingModel.CreateFromExternalFromEffective(
                person, schedule.Effective, extApp, coverage, request.UserId, occurredAt)
            : BookingModel.CreateFromExternal(
                person, tglBerobat, schedule.LegacyJadwal, extApp, coverage, request.UserId, occurredAt);

        //      ambil nomor antrian
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

        var tracker = PasienTrackerModel.Create(booking, occurredAt);

        //  WRITE
        BookingCreateFromHidokResponse response;
        using (var trans = TransHelper.NewScope())
        {
            var antEntry = antrian.AddEntry(request.NoAntrian, tracker, booking.BookingId, "BOK", occurredAt);
            booking.AssignNoAntrian(antEntry.NoUrut);

            _bookingRepo.SaveChanges(booking);
            _antrianRepo.SaveChanges(antrian);
            _trackerRepo.SaveChanges(tracker);
            trans.Complete();
            response = new BookingCreateFromHidokResponse(booking.BookingId, antEntry.NoUrut);
        }

        
        return Task.FromResult(response);
    }

    private PasienModel FindPasien(BookingCreateFromHidokCommand request)
    {
        var pasienKey = PasienModel.Key(request.PasienId);
        var pasien = _pasienRepo.LoadEntity(pasienKey).GetValueOrDefault(PasienModel.Default);
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
