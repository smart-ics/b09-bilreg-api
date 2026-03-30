using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.BookingFeature;

public record PraktekDokterPeriodeGroupSpesialisListQuery(string TglYmdAwal, string TglYmdAkhir, string GroupSpesialisId) : 
    IRequest<IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>>, IGroupSpesialisKey;

public record PraktekDokterPeriodeGroupSpesialisListResponse(
    string Tanggal, PpaReff Dokter, LayananReff Layanan, 
    string JamMulaiPraktek, string JamSelesaiPraktek, int JumlahPasien, int MaxPasien);

public class PraktekDokterPeriodeGroupSpesialisListHandler :
    IRequestHandler<PraktekDokterPeriodeGroupSpesialisListQuery, IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    public PraktekDokterPeriodeGroupSpesialisListHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
    }

    public Task<IEnumerable<PraktekDokterPeriodeGroupSpesialisListResponse>> Handle(PraktekDokterPeriodeGroupSpesialisListQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrEmpty(request.GroupSpesialisId);
        Guard.Against.InvalidDateFormat(request.TglYmdAwal, nameof(request.TglYmdAwal));
        Guard.Against.InvalidDateFormat(request.TglYmdAkhir, nameof(request.TglYmdAkhir));

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
    private List<PraktekDokterPeriodeGroupSpesialisListResponse> GenResult(List<DateOnly> listTgl, List<AntrianModel> antrians, List<JadwalPraktekType> jadwals)
    {
        var result =
            (
                from tgl in listTgl
                let jadwalTgl = jadwals.Where(j => j.Hari == tgl.DayOfWeek)
                from j in jadwalTgl
                let dokterId = j.Dokter.PpaId
                let jamMulai = j.JamMulai
                let antrianMatch = antrians.Where(a =>
                    a.AntrianDate == tgl &&
                    a.SequenceTag.Split('_')[1].Trim() == dokterId &&
                    a.StartTime == jamMulai
                )
                select new PraktekDokterPeriodeGroupSpesialisListResponse(
                    tgl.ToString("yyyy-MM-dd"),
                    j.Dokter,
                    j.Layanan,
                    j.JamMulai.ToString("HH:mm", CultureInfo.InvariantCulture),
                    j.JamSelesai.ToString("HH:mm", CultureInfo.InvariantCulture),
                    JumlahPasien: antrianMatch.Sum(x => x.ListEntry.Count()),
                    MaxPasien: j.MaxPasien
                )
            )
            .OrderBy(x => x.Tanggal)
            .ThenBy(x => x.JamMulaiPraktek)
            .ToList();
        return result;
    }
        
}
