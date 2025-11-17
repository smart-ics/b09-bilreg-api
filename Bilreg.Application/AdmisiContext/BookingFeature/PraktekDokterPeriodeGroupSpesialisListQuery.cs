using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PetugasMedisFeature;
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
    private readonly IPetugasMedisRepo _ptgMedRepo;
    public PraktekDokterPeriodeGroupSpesialisListHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPetugasMedisRepo ptgMedRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ptgMedRepo = ptgMedRepo;
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
        
        var listAntrian = new List<AntrianHeaderView>();
        foreach (var x in listTgl)
        {
            var antrian = _antrianRepo.ListData(x)?.ToList() ?? [];
            listAntrian.AddRange(antrian);
        }
        var antrians = listAntrian
            .Select(x => 
            _antrianRepo.LoadEntity(x)
                .Match(
                    onSome: j => j,
                    onNone: () => AntrianModel.Default
                )
            )?.ToList() ?? [];
        
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
    private PetugasMedisType GetPetugasMedis(IPetugasMedisKey key)
    {
        return _ptgMedRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => PetugasMedisType.Default
            );
    }

    private List<PraktekDokterPeriodeGroupSpesialisListResponse> GenResult(List<DateOnly> listTgl, List<AntrianModel> antrians, List<JadwalPraktekType> jadwals)
    {
        var fromAntrian =
            (
                from tgl in listTgl
                let antrianTgl = antrians
                    .Where(a => a.AntrianDate == tgl).ToList()
                let jadwalTgl = jadwals
                    .Where(j => j.Hari == tgl.DayOfWeek).ToList()
                from a in antrianTgl
                let dokterIdFromTag = a.SequenceTag.Split('_')[1].Trim()
                let jadwal = jadwalTgl
                    .FirstOrDefault(j =>
                        j.Dokter.PetugasMedisId == dokterIdFromTag &&
                        j.JamMulai == a.StartTime)
                select new PraktekDokterPeriodeGroupSpesialisListResponse(
                    tgl.ToString("yyyy-MM-dd"),
                    jadwal?.Dokter
                        ?? new PetugasMedisReff(dokterIdFromTag, 
                        GetPetugasMedis(PetugasMedisType.Key(dokterIdFromTag)).PetugasMedisName),
                    jadwal?.Layanan
                        ?? new LayananReff("-", "TANPA JADWAL"),
                    (jadwal?.JamMulai ?? a.StartTime).ToString("HH:mm"),
                    JumlahPasien:
                        antrianTgl
                            .Where(x =>
                                x.SequenceTag.Split('_')[1].Trim() == dokterIdFromTag &&
                                x.StartTime == (jadwal?.JamMulai ?? a.StartTime)
                            )
                            .Sum(x => x.ListEntry
                                .Where(y => y.AntrianStatus == AntrianStatusEnum.Waiting).Count()),
                    MaxPasien: jadwal?.MaxPasien ?? 0
                )
            ).ToList() ?? [];

        var jadwalNoQueue = (
                from tgl in listTgl
                let jadwalTgl = jadwals.Where(j => j.Hari == tgl.DayOfWeek).ToList()
                from j in jadwalTgl
                where !fromAntrian.Any(a =>
                    a.Layanan.LayananId == j.Layanan.LayananId &&
                    a.Dokter.PetugasMedisId == j.Dokter.PetugasMedisId &&
                    a.JamMulaiPraktek == j.JamMulai.ToString("HH:mm") &&
                    a.Tanggal == tgl.ToString("yyyy-MM-dd"))
                select new PraktekDokterPeriodeGroupSpesialisListResponse(
                    tgl.ToString("yyyy-MM-dd"),
                    j.Dokter,
                    j.Layanan,
                    j.JamMulai.ToString("HH:mm"),
                    JumlahPasien: 0,
                    MaxPasien: j.MaxPasien
                )).ToList() ?? [];

        var result = fromAntrian
            .Concat(jadwalNoQueue)
            .OrderBy(x => x.Tanggal)
            .ThenBy(x => x.JamMulaiPraktek)
            .ToList();

        return result;
        
    }
    
}
