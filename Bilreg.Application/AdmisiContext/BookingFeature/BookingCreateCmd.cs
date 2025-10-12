using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.JadwalFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;


namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record BookingCreateCmd(string PasienName, string TglLahir, 
    string Gender, string Alamat, string NoTelp, 
    string DokterId, string TglBerobat, string JamMulai) : IRequest<BookingCreateResponse>;

public record BookingCreateResponse (string BookingId, int NoAntrian);

public class BookingCreateHandler : IRequestHandler<BookingCreateCmd, BookingCreateResponse>
{
    private readonly IJadwalPraktekDal _jadwalPraktekDal;
    private readonly IGenderDal _genderDal;
    private readonly IAntrianRepo _antrianRepo;
    private readonly IAntrianFactory _antrianFactory;
    public BookingCreateHandler(IJadwalPraktekDal jadwalPraktekDal, 
        IGenderDal genderDal, IAntrianRepo antrianRepo, IAntrianFactory antrianFactory)
    {
        _jadwalPraktekDal = jadwalPraktekDal;
        _genderDal = genderDal;
        _antrianRepo = antrianRepo;
        _antrianFactory = antrianFactory;
    }

    public Task<BookingCreateResponse> Handle(BookingCreateCmd request, CancellationToken cancellationToken)
    {
        //  create person
        var gender = _genderDal.GetData(request.Gender)
                .GetValueOrThrow("Gender invalid");
        var person = new PersonType(
            request.PasienName,
            DateTime.Parse(request.TglLahir),
            gender,
            new AlamatType([request.Alamat], "-", "-"),
            new ContactType(JenisContactEnum.Phone, request.NoTelp),
            IdentitasType.Default);

        //  get jadwal
        var dokter = PetugasMedisType.Key(request.DokterId);
        var listJadwal = _jadwalPraktekDal.ListData(dokter)?.ToList() ?? [];
        var jamMulai = TimeOnly.Parse(request.JamMulai);
        var jadwal = listJadwal.FirstOrDefault(x => x.JamMulai == jamMulai) 
                     ?? throw new ArgumentException("Jadwal tidak ditemukan");

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
        var tracker = PasienTrackerModel.Create(booking.Person);
        antrian.AddEntry(tracker);
        
        throw new NotImplementedException();
    }
}