using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QueListAntrianHeaderQuery(string TglYmd): IRequest<IEnumerable<QueListAntrianHeaderResponse>>;

public record QueListAntrianHeaderResponse(string AntrianId, PpaReff Dokter, LayananReff Layanan, string JamMulai);

public class QueListAntrianHeaderHandler : IRequestHandler<QueListAntrianHeaderQuery, IEnumerable<QueListAntrianHeaderResponse>>
{
    public readonly IAntrianRepo _queRepo;
    public readonly IJadwalPraktekRepo _jadwalRepo;

    public QueListAntrianHeaderHandler(IAntrianRepo queRepo, 
        IJadwalPraktekRepo jadwalRepo)
    {
        _queRepo = queRepo;
        _jadwalRepo = jadwalRepo;
    }

    public Task<IEnumerable<QueListAntrianHeaderResponse>> Handle(QueListAntrianHeaderQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd, nameof(request.TglYmd));
        
        var date = DateOnly.Parse(request.TglYmd);
        var listAntrian = _queRepo.ListData(date)?.ToList() ?? [];
        var listQue = ConvertQue(listAntrian);
        
        var listJadwal = _jadwalRepo.ListData()?.ToList() ?? [];
        var listSchedule = listJadwal.Where(x => x.Hari == date.DayOfWeek)?.ToList() ?? [];

        var result = GenResponse(listQue, listSchedule);
        return Task.FromResult(result.AsEnumerable()); 
    }

    #region PRIVATE-HELPER
    private List<QueDokterTodayDto> ConvertQue(IEnumerable<AntrianHeaderView> listAntrian)
    {
        var result = listAntrian.Select(x => new QueDokterTodayDto(
            x.AntrianId, x.SequenceTag.Split("_")[1], x.StartTime))?.ToList() ?? [];
        return result;

    }
    private List<QueListAntrianHeaderResponse> GenResponse(List<QueDokterTodayDto> listQue, List<JadwalPraktekType> listJadwal)
    {
        var result = (
            from q in listQue
            join j in listJadwal
                on new { DokterId = q.DokterId, Jam = q.StartTime }
                equals new { DokterId = j.Dokter.PpaId, Jam = j.JamMulai }
            select new QueListAntrianHeaderResponse(
                q.AntrianId,
                j.Dokter,
                j.Layanan,
                j.JamMulai.ToString("HH:mm")
            ))?.ToList() ?? [];
        return result;
    }
    public record QueDokterTodayDto(string AntrianId, string DokterId, TimeOnly StartTime);
    #endregion
}
