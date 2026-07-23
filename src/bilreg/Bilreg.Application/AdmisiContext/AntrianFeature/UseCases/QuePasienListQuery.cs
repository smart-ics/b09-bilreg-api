using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Globalization;
using Bilreg.Application.AdmisiContext.AntrianFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature.UseCases;

public record QuePasienListQuery(string TglYmd) : IRequest<IEnumerable<QuePasienListResponse>>;

public record QuePasienListResponse(PpaReff Ppa, LayananReff Layanan, string PasienName,
    string StartTime, string EndTime, string ReffId, string ReffDesc);

public class QuePasienListHandler : IRequestHandler<QuePasienListQuery, IEnumerable<QuePasienListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalRepo;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;

    public QuePasienListHandler(IAntrianRepo antrianRepo, 
        IJadwalPraktekRepo jadwalRepo,
        IJadwalPraktekFeatureResolver featureResolver)
    {
        _antrianRepo = antrianRepo;
        _jadwalRepo = jadwalRepo;
        _featureResolver = featureResolver;
    }

    public Task<IEnumerable<QuePasienListResponse>> Handle(QuePasienListQuery request, CancellationToken cancellationToken)
    {
        Guard.Against.InvalidDateFormat(request.TglYmd, nameof(request.TglYmd));

        var date = request.TglYmd.ToDate("yyyy-MM-dd");
        var tgl = DateOnly.FromDateTime(date);
        var listQue = _antrianRepo.ListData(date)?.ToList() ?? [];

        List<JadwalPraktekType> listJadwalThisDay;
        if (_featureResolver.UseResolver)
        {
            listJadwalThisDay = _featureResolver.ResolveForDate(
                    new JadwalPraktekResolveForDateRequest(tgl))
                .Select(JadwalPraktekLegacyAdapter.ToTemplate)
                .ToList();
        }
        else
        {
            var listJadwal = _jadwalRepo.ListData()?.ToList() ?? [];
            listJadwalThisDay = listJadwal.Where(x => x.Hari == tgl.DayOfWeek).ToList();
        }

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