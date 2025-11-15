using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record PraktekDokterPeriodGroupSpesialisListQuery(string TglYmdAwal, string TglYmdAkhir, string GroupSpesialisId) : 
    IRequest<IEnumerable<PraktekDokterPeriodGroupSpesialisListResponse>>, IGroupSpesialisKey;

public record PraktekDokterPeriodGroupSpesialisListResponse(
    string Tanggal, PetugasMedisReff Dokter, LayananReff Layanan, 
    string JamMulaiPraktek, int JumlahPasien, int MaxPasien);

public class PraktekDokterPeriodGroupSpesialisListHandler : 
    IRequestHandler<PraktekDokterPeriodGroupSpesialisListQuery, IEnumerable<PraktekDokterPeriodGroupSpesialisListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IBookingRepo _bookingRepo;
    public PraktekDokterPeriodGroupSpesialisListHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IBookingRepo bookingRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _bookingRepo = bookingRepo;
    }

    public Task<IEnumerable<PraktekDokterPeriodGroupSpesialisListResponse>> Handle(PraktekDokterPeriodGroupSpesialisListQuery request, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
