using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PetugasMedisFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record PraktekDokterPeriodeGroupSpesialisListQuery(string TglYmdAwal, string TglYmdAkhir, string GroupSpesialisId) : 
    IRequest<IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>>, IGroupSpesialisKey;

public record PraktekDokterPeriodeGroupSpesialisListResponse(
    string Tanggal, PetugasMedisReff Dokter, LayananReff Layanan, 
    string JamMulaiPraktek, int JumlahPasien, int MaxPasien);

public class PraktekDokterPeriodeGroupSpesialisListHandler :
    IRequestHandler<PraktekDokterPeriodeGroupSpesialisListQuery, IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    public PraktekDokterPeriodeGroupSpesialisListHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
    }

    public Task<IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>> Handle(PraktekDokterPeriodeGroupSpesialisListQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.GroupSpesialisId, nameof(request.GroupSpesialisId));

        // BUILD
        var tglawal = request.TglYmdAwal.ToDate("yyyy-MM-dd");
        var tglAkhir = request.TglYmdAkhir.ToDate("yyyy-MM-dd");
        var periode = new Periode(tglawal, tglAkhir);

        var listTgl = GenTanggal(DateOnly.FromDateTime(tglawal), DateOnly.FromDateTime(tglAkhir));
        var jadwals = _jadwalPraktekRepo.ListData(request)?.ToList() ?? [];
        var antrians = new List<AntrianHeaderView>();
        foreach (var x in listTgl)
        {
            var antrian = _antrianRepo.ListData(x)?.ToList() ?? [];
            antrians.AddRange(antrian);
        }
        // PROJECTION

        var result = GenResult(listTgl, antrians, jadwals);

        // RETURN
        return Task.FromResult(result.AsEnumerable());
    }
    private List<DateOnly> GenTanggal(DateOnly tglAwal, DateOnly tglAkhir)
    {
        int jumlahHari = tglAkhir.DayNumber - tglAwal.DayNumber + 1;

        // Bentuk list
        List<DateOnly> listTanggal = Enumerable.Range(0, jumlahHari)
            .Select(i => tglAwal.AddDays(i))
            .ToList();
        return listTanggal;

    }

    private List<PraktekDokterPeriodeGroupSpesialisListResponse> GenResult(List<DateOnly> listTgl, List<AntrianHeaderView> antrians, List<JadwalPraktekType> jadwals)
    {
        var result =
            (
                from tgl in listTgl

                    // ambil antrian di tanggal tsb
                let antrianTgl = antrians
                    .Where(a => a.AntrianDate == tgl)
                    .ToList()

                // ambil jadwal yang berlaku di tanggal tsb
                let jadwalTgl = jadwals
                    .Where(j => j.Hari == tgl.DayOfWeek)
                    .ToList()

                // proyeksikan antrian → response
                let fromAntrian =
                    from a in antrianTgl
                    let dokterIdFromTag = a.SequenceTag.Split('_')[1]   
                    let jadwal = jadwalTgl.First(j =>
                        j.Dokter.PetugasMedisId == dokterIdFromTag &&
                        j.JamMulai == a.StartTime
                    )
                    select new PraktekDokterPeriodeGroupSpesialisListResponse(
                        tgl.ToString("yyyy-MM-dd"),
                        jadwal.Dokter,
                        jadwal.Layanan,
                        jadwal.JamMulai.ToString("HH:mm"),
                        JumlahPasien: antrianTgl.Count(x =>
                            x.SequenceTag.Split('_')[1] == dokterIdFromTag &&
                            x.StartTime == jadwal.JamMulai
                        ),
                        MaxPasien: jadwal.MaxPasien
                    )

                // proyeksi jadwal yg tidak punya antrian
                let fromJadwalNoQueue =
                    from j in jadwalTgl
                    where !fromAntrian.Any(a => a.Layanan.LayananId == j.Layanan.LayananId &&
                                                a.Dokter.PetugasMedisId == j.Dokter.PetugasMedisId &&
                                                a.JamMulaiPraktek == j.JamMulai.ToString("HH:mm"))
                    select new PraktekDokterPeriodeGroupSpesialisListResponse(
                        tgl.ToString("yyyy-MM-dd"),
                        j.Dokter,
                        j.Layanan,
                        j.JamMulai.ToString("HH:mm"),
                        JumlahPasien: 0,
                        MaxPasien: j.MaxPasien
                    )

                select fromAntrian.Concat(fromJadwalNoQueue)
            )
            .SelectMany(x => x)
            .ToList();
        return result;
    }

}
