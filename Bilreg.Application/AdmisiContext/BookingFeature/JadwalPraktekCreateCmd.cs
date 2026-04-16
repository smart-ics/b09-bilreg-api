//using Ardalis.GuardClauses;
//using Bilreg.Application.AdmisiContext.LayananFeature;
//using Bilreg.Application.AdmisiContext.PpaFeature;
//using Bilreg.Domain.AdmisiContext.BookingFeature;
//using Bilreg.Domain.AdmisiContext.LayananFeature;
//using Bilreg.Domain.AdmisiContext.PpaFeature;
//using Bilreg.Domain.Shared.Helpers;
//using MediatR;
//using Newtonsoft.Json.Linq;
//using System.Globalization;

//namespace Bilreg.Application.AdmisiContext.BookingFeature;

//public record JadwalPraktekCreateCmd(
//    string DokterId, string LayananId, int Hari,
//    string JamMulai, string JamSelesai, int MaxPasien) : IRequest<JadwalPraktekCreateResponse>;

//public record JadwalPraktekCreateResponse(string JadwalPraktekId);

//public class JadwalPraktekCreateHandler : IRequestHandler<JadwalPraktekCreateCmd, JadwalPraktekCreateResponse>
//{
//    private readonly IJadwalPraktekRepo _jadwalRepo;
//    private readonly IJadwalPraktekFactory _jadwalNunaFactory;
//    private readonly IPpaRepo _petugasRepo;
//    private readonly ILayananRepo _layananRepo;

//    public JadwalPraktekCreateHandler(IJadwalPraktekRepo jadwalRepo, 
//        IJadwalPraktekFactory jadwalNunaFactory, 
//        IPpaRepo petugasRepo, 
//        ILayananRepo layananRepo)
//    {
//        _jadwalRepo = jadwalRepo;
//        _jadwalNunaFactory = jadwalNunaFactory;
//        _petugasRepo = petugasRepo;
//        _layananRepo = layananRepo;
//    }

//    public Task<JadwalPraktekCreateResponse> Handle(JadwalPraktekCreateCmd request, CancellationToken cancellationToken)
//    {
//        //  GUARD
//        Guard.Against.NegativeOrZero(request.MaxPasien, nameof(request.MaxPasien));
//        var dokterKey = PpaType.Key(request.DokterId);
//        var dokter = _petugasRepo.LoadEntity(dokterKey)
//            .GetValueOrThrow("Dokter tidak ditemukan");
//        var layananKey = LayananType.Key(request.LayananId);
//        var layanan = _layananRepo.LoadEntity(layananKey)
//            .GetValueOrThrow("Layanan tidak ditemukan");
//        if (!Enum.IsDefined(typeof(DayOfWeek), request.Hari))
//            throw new Exception("Hari tidak valid");
        
//        //  BUILD
//        var listJadwal = _jadwalRepo.ListData(dokterKey)?.ToList() ?? [];
//        var jadwalReq = CreateJadwal(request, dokter, layanan);
//        if (jadwalReq.JamMulai >= jadwalReq.JamSelesai)
//            throw new Exception("Jam Mulai harus lebih kecil dari Jam Selesai");
//        var isOverlap = listJadwal
//            .Where(x => x.Dokter.PpaId == jadwalReq.Dokter.PpaId && x.Hari == jadwalReq.Hari)
//            .Any(x =>
//                jadwalReq.JamMulai < x.JamSelesai &&
//                jadwalReq.JamSelesai > x.JamMulai
//            );
//        if (isOverlap)
//            throw new Exception("Jadwal beririsan");
        
//        //  WRITE
//        _jadwalRepo.SaveChanges(jadwalReq);
//        return Task.FromResult(new JadwalPraktekCreateResponse(jadwalReq.JadwalPraktekId));
//    }

//    private JadwalPraktekType CreateJadwal(JadwalPraktekCreateCmd request, 
//        PpaType dokter, LayananType layanan)
//    {
//        // validasi jam
//        TimeOnly jamMulai;
//        TimeOnly jamSelesai;
//        try
//        {
//            jamMulai = TimeOnly.ParseExact(request.JamMulai, "HH:mm", CultureInfo.InvariantCulture);
//            jamSelesai = TimeOnly.ParseExact(request.JamSelesai, "HH:mm", CultureInfo.InvariantCulture);
//        }
//        catch (FormatException)
//        {
//            throw new Exception("Format jam harus HH:mm");
//        }

//        var jadwal = _jadwalNunaFactory.Create(
//            dokter, layanan, 
//            (DayOfWeek)request.Hari, 
//            jamMulai, jamSelesai,
//            request.MaxPasien);
//        return jadwal;
//    }
//}