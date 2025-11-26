using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.Helpers;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using CommunityToolkit.Diagnostics;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Linq;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record PraktekDokterPeriodeDokterListQuery(string TglYmdAwal, string TglYmdAkhir, string DokterId) : 
    IRequest<IEnumerable<PraktekDokterPeriodeDokterListResponse>>;

public record PraktekDokterPeriodeDokterListResponse(
    string Tanggal, PpaReff Dokter, LayananReff Layanan,
    string JamMulaiPraktek, int JumlahPasien, int MaxPasien);

public class PraktekDokterPeriodeDokterListHandler : IRequestHandler<PraktekDokterPeriodeDokterListQuery, 
    IEnumerable<PraktekDokterPeriodeDokterListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ptgMedRepo;
    public PraktekDokterPeriodeDokterListHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ptgMedRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ptgMedRepo = ptgMedRepo;
    }

    public Task<IEnumerable<PraktekDokterPeriodeDokterListResponse>> Handle(PraktekDokterPeriodeDokterListQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.IsNotNull(request);
        Guard.IsNotEmpty(request.DokterId);
        request.TglYmdAwal.IsValidDateYmd();
        request.TglYmdAkhir.IsValidDateYmd();

        // BUILD
        var tglawal = request.TglYmdAwal.ToDate("yyyy-MM-dd");
        var tglAkhir = request.TglYmdAkhir.ToDate("yyyy-MM-dd");
        var periode = new Periode(tglawal, tglAkhir);

        var listTgl = GenTanggal(DateOnly.FromDateTime(tglawal), DateOnly.FromDateTime(tglAkhir));

        var jadwals = _jadwalPraktekRepo.ListData(PpaType.Key(request.DokterId))?.ToList() ?? [];

        var listAntrian = new List<AntrianHeaderView>();
        foreach (var x in listTgl)
        {
            var antrian = _antrianRepo.ListData(x)?.ToList() ?? [];
            listAntrian.AddRange(antrian);
        }
        var listAntreanDokter = listAntrian.Where(x => x.SequenceTag.Split('_')[1].Trim() == request.DokterId);
        var antrians = listAntreanDokter
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
    private PpaType GetPetugasMedis(IPpaKey key)
    {
        return _ptgMedRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => PpaType.Default
            );
    }

    private List<PraktekDokterPeriodeDokterListResponse> GenResult(List<DateOnly> listTgl, List<AntrianModel> antrians, List<JadwalPraktekType> jadwals)
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
                        j.Dokter.PpaId == dokterIdFromTag &&
                        j.JamMulai == a.StartTime)
                select new PraktekDokterPeriodeDokterListResponse(
                    tgl.ToString("yyyy-MM-dd"),
                    jadwal?.Dokter
                        ?? new PpaReff(dokterIdFromTag,
                        GetPetugasMedis(PpaType.Key(dokterIdFromTag)).PpaName),
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
                    a.Dokter.PpaId == j.Dokter.PpaId &&
                    a.JamMulaiPraktek == j.JamMulai.ToString("HH:mm") &&
                    a.Tanggal == tgl.ToString("yyyy-MM-dd"))
                select new PraktekDokterPeriodeDokterListResponse(
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
