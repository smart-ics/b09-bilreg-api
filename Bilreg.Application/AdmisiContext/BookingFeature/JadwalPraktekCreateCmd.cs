using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using MediatR;
using Newtonsoft.Json.Linq;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekCreateCmd(
    string DokterId, string LayananId, int Hari,
    string JamMulai, string JamSelesai, int MaxPasien) : IRequest<JadwalPraktekCreateResponse>;

public record JadwalPraktekCreateResponse(string JadwalPraktekId);

public class JadwalPraktekCreateHandler : IRequestHandler<JadwalPraktekCreateCmd, JadwalPraktekCreateResponse>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;
    private readonly IJadwalPraktekFactory _jadwalNunaFactory;
    private readonly IPpaRepo _petugasRepo;
    private readonly ILayananRepo _layananRepo;

    public JadwalPraktekCreateHandler(IJadwalPraktekRepo jadwalRepo, 
        IJadwalPraktekFactory jadwalNunaFactory, 
        IPpaRepo petugasRepo, 
        ILayananRepo layananRepo)
    {
        _jadwalRepo = jadwalRepo;
        _jadwalNunaFactory = jadwalNunaFactory;
        _petugasRepo = petugasRepo;
        _layananRepo = layananRepo;
    }

    public Task<JadwalPraktekCreateResponse> Handle(JadwalPraktekCreateCmd request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NegativeOrZero(request.MaxPasien, nameof(request.MaxPasien));
        var dokterKey = PpaType.Key(request.DokterId);
        var dokter = _petugasRepo.LoadEntity(dokterKey)
            .GetValueOrThrow("Dokter tidak ditemukan");
        var layananKey = LayananType.Key(request.LayananId);
        var layanan = _layananRepo.LoadEntity(layananKey)
            .GetValueOrThrow("Layanan tidak ditemukan");

        
        //  BUILD
        var listJadwal = _jadwalRepo.ListData(dokterKey)?.ToList() ?? [];
        var thisJadwal = listJadwal
            .Where(x => x.Hari == (DayOfWeek)request.Hari)
            .FirstOrDefault(x => x.JamMulai == TimeOnly.Parse(request.JamMulai)) 
            ?? CreateJadwal(request, dokter, layanan);
        thisJadwal = thisJadwal with
        {
            JamSelesai = TimeOnly.Parse(request.JamSelesai),
            Layanan = layanan.ToReff()
        };
        
        //  WRITE
        _jadwalRepo.SaveChanges(thisJadwal);
        return Task.FromResult(new JadwalPraktekCreateResponse(thisJadwal.JadwalPraktekId));
    }

    private JadwalPraktekType CreateJadwal(JadwalPraktekCreateCmd request, 
        PpaType dokter, LayananType layanan)
    {
        var jadwal = _jadwalNunaFactory.Create(
            dokter, layanan, 
            (DayOfWeek)request.Hari, 
            TimeOnly.Parse(request.JamMulai),
            TimeOnly.Parse(request.JamSelesai),
            request.MaxPasien);
        return jadwal;
    }
}