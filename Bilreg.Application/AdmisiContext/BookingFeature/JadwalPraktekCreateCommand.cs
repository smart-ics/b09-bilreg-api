using Bilreg.Application.AdmisiContext.PetugasMedisSub;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananSub;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisSub.PetugasMedisFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record JadwalPraktekCreateCommand(
    string DokterId, string LayananId, int Hari,
    string JamMulai, string JamSelesai) : IRequest<JadwalPraktekCreateResponse>;

public record JadwalPraktekCreateResponse(string JadwalPraktekId);

public class JadwalPraktekCreateHandler : IRequestHandler<JadwalPraktekCreateCommand, JadwalPraktekCreateResponse>
{
    private readonly IJadwalPraktekRepo _jadwalRepo;
    private readonly IJadwalPraktekFactory _jadwalFactory;
    private readonly IPetugasMedisRepo _petugasRepo;

    public JadwalPraktekCreateHandler(IJadwalPraktekRepo jadwalRepo, 
        IJadwalPraktekFactory jadwalFactory, 
        IPetugasMedisRepo petugasRepo)
    {
        _jadwalRepo = jadwalRepo;
        _jadwalFactory = jadwalFactory;
        _petugasRepo = petugasRepo;
    }

    public Task<JadwalPraktekCreateResponse> Handle(JadwalPraktekCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        var dokterKey = PetugasMedisType.Key(request.DokterId);
        var dokter = _petugasRepo.LoadEntity(dokterKey)
            .GetValueOrThrow("Dokter tidak ditemukan");
        
        // var layananKey = LayananType.Key(request.LayananId);
        // var layanan = _layanRepo.LoadEntity(layananKey)
        //     .GetValueOrThrow("Layanan tidak ditemukan");
        
        var listJadwal = _jadwalRepo.ListData(dokterKey)?.ToList() ?? [];
        var thisJadwal = listJadwal
            .Where(x => x.Hari == (DayOfWeek)request.Hari)
            .FirstOrDefault(x => x.JamMulai == TimeOnly.Parse(request.JamMulai));
        if (thisJadwal is null)
            thisJadwal = CreateJadwal(request);
        
        // var layanan = 
        // thisJadwal = thisJadwal with
        // {
        //     JamSelesai = TimeOnly.Parse(request.JamSelesai),
        //     Layanan = LayananType.Key(request.LayananId)
        // };
        throw new NotImplementedException();

    }

    private JadwalPraktekType? CreateJadwal(JadwalPraktekCreateCommand request)
    {
        throw new NotImplementedException();
    }
}