using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Globalization;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record QuePasienListQuery(string TglYmd) : IRequest<IEnumerable<QuePasienListResponse>>;

public record QuePasienListResponse(PpaReff Ppa, LayananReff Layanan, string PasienName,
    string StartTime, string EndTime, string ReffId, string ReffDesc);

public class QuePasienListHandler : IRequestHandler<QuePasienListQuery, IEnumerable<QuePasienListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalRepo;
    public QuePasienListHandler(IAntrianRepo antrianRepo, 
        IJadwalPraktekRepo jadwalRepo)
    {
        _antrianRepo = antrianRepo;
        _jadwalRepo = jadwalRepo;
    }

    public Task<IEnumerable<QuePasienListResponse>> Handle(QuePasienListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd, nameof(request.TglYmd));

        var date = request.TglYmd.ToDate("yyyy-MM-dd");
        var listQue = _antrianRepo.ListData(date)?.ToList() ?? [];
        var listJadwal = _jadwalRepo.ListData()?.ToList() ?? [];
        var listJadwalThisDay = listJadwal.Where(x => x.Hari == date.DayOfWeek)?.ToList() ?? [];

        var result =(
            from q in listQue
            join j in listJadwalThisDay
                on new { DokterId = q.DokterId, Jam = q.StartTime }
                equals new { DokterId = j.Dokter.PpaId, Jam = j.JamMulai }
            select new QuePasienListResponse(
                j.Dokter,
                j.Layanan,
                q.PersonName,
                q.StartTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                q.EndTime.ToString("HH:mm", CultureInfo.InvariantCulture),
                q.ReffId,
                q.ReffDesc
            ))?.ToList() ?? [];

        return Task.FromResult(result.AsEnumerable());
    }
}