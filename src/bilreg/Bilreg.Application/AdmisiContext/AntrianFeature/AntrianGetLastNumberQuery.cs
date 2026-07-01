using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Domain.AdmisiContext.JadwalPraktekFeature;
using Bilreg.Application.AdmisiContext.BookingFeature;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.BookingFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers;
using MediatR;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianGetQuotaQuery(string DokterHidokId, string TglAntrianYmd, string JamMulai) 
    : IRequest<AntrianGetQuotaResponse>;


public record AntrianGetQuotaResponse(int Quota, int Used, int AvailableQuota);


public class AntrianGetQuotaHandler : IRequestHandler<AntrianGetQuotaQuery, AntrianGetQuotaResponse>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IJadwalPraktekRepo _jadwalPraktekRepo;
    private readonly IPpaRepo _ppaRepo;
    private readonly IJadwalPraktekFeatureResolver _featureResolver;

    public AntrianGetQuotaHandler(IAntrianRepo antrianRepo,
        IJadwalPraktekRepo jadwalPraktekRepo,
        IPpaRepo ppaRepo,
        IJadwalPraktekFeatureResolver featureResolver)
    {
        _antrianRepo = antrianRepo;
        _jadwalPraktekRepo = jadwalPraktekRepo;
        _ppaRepo = ppaRepo;
        _featureResolver = featureResolver;
    }


    public Task<AntrianGetQuotaResponse> Handle(AntrianGetQuotaQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrEmpty(request.DokterHidokId);
        Guard.Against.InvalidDateFormat(request.TglAntrianYmd, nameof(request.TglAntrianYmd));
        Guard.Against.InvalidTimeFormat(request.JamMulai, nameof(request.JamMulai));

        var finder = new ContactFinder(JenisContactEnum.Email, request.DokterHidokId);
        var dokter = _ppaRepo.LoadEntity(finder)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterHidokId} not found")
            );

        // BUILD
        DateOnly tglAntrian = DateOnly.ParseExact(request.TglAntrianYmd,
            "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);
        var jamPraktek = TimeOnly.Parse(request.JamMulai);

        var maxPasien = ResolveMaxPasien(dokter, tglAntrian, jamPraktek);

        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, jamPraktek, dokter);
        var listAntrianDb = _antrianRepo.ListData(tglAntrian)?.ToList() ?? [];
        var antrianHeader = listAntrianDb.FirstOrDefault(x => x.SequenceTag == sequenceTag);
        var used = antrianHeader is null
            ? 0
            : _antrianRepo.LoadEntity(antrianHeader).Value.ListEntry.Count();
        var available = maxPasien - used;

        var result = new AntrianGetQuotaResponse(maxPasien, used, available);
        return Task.FromResult(result); 
    }

    private int ResolveMaxPasien(PpaType dokter, DateOnly tglAntrian, TimeOnly jamPraktek)
    {
        if (_featureResolver.UseResolver)
        {
            var effective = _featureResolver.Resolve(new JadwalPraktekResolveRequest(
                tglAntrian, dokter, jamPraktek, new JadwalPraktekResolveOptions()));
            return effective.MaxPasien;
        }

        return GetJadwalThatDay(dokter, tglAntrian, jamPraktek).MaxPasien;
    }

    #region PRIVATE_HELPER
    
    private JadwalPraktekType GetJadwalThatDay(PpaType dokter, DateOnly tglAntrian, TimeOnly jamPraktek)
    {
        return LegacyJadwalPraktekLookup.Resolve(
            _jadwalPraktekRepo, dokter, tglAntrian, jamPraktek);
    }
    #endregion
}
