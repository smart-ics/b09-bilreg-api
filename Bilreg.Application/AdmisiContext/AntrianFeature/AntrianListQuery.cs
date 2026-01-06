using Bilreg.Domain.AdmisiContext.AntrianFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;
using System.Data.SqlTypes;
using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiContext.AntrianFeature;

public record AntrianListQuery(string TglAntrian, string DokterId) : IRequest<IEnumerable<AntrianListResponse>>;

public record AntrianListResponse(string DokterId, string DokterName, 
    string JamMulai, string JamSelesai, string Diskripsi, 
    IEnumerable<AntrianListDtlResponse>ListPasien);

public record AntrianListDtlResponse(
    string AntrianId, string PasienName, int NoUrut, int Status, string StatusString, string ReffId, string ReffDesc);

public class AntrianListHandler : IRequestHandler<AntrianListQuery, IEnumerable<AntrianListResponse>>
{
    private readonly IAntrianRepo _antrianRepo;
    private readonly IPpaRepo _ptgMedisRepo;
    private const string FORMAT_TGL_YMD = "yyyy-MM-dd";
    public AntrianListHandler(IAntrianRepo antrianRepo, 
        IPpaRepo ptgMedisRepo)
    {
        _antrianRepo = antrianRepo;
        _ptgMedisRepo = ptgMedisRepo;
    }

    public Task<IEnumerable<AntrianListResponse>> Handle(AntrianListQuery request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrEmpty(request.TglAntrian);
        Guard.Against.NullOrEmpty(request.DokterId);
        Guard.Against.NullOrEmpty(request.TglAntrian);

        // BUILD
        DateOnly tglAntrian = DateOnly.ParseExact(request.TglAntrian, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture);

        var dokter = _ptgMedisRepo.LoadEntity(PpaType.Key(request.DokterId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Dokter {request.DokterId} not found") 
            );

        var sequenceTag = AntrianModel.GenSequenceTag(tglAntrian, dokter);
        var listAntrianDb = _antrianRepo.ListData(tglAntrian)?.ToList() 
            ?? throw new ArgumentException($"Antrian at {request.TglAntrian} not foud");

        var antrianHeaders = listAntrianDb.Where(x => x.SequenceTag == sequenceTag)?.ToList() ?? [];
        
        var listAntrian = antrianHeaders.Select(x => _antrianRepo.LoadEntity(x));

        var result = listAntrian
            .Select(a => new AntrianListResponse(
                DokterId: dokter.PpaId, 
                DokterName: dokter.PpaName, 
                JamMulai: a.Value.StartTime.ToString("HH:mm"),
                JamSelesai: a.Value.EndTime.ToString("HH:mm"),
                Diskripsi: a.Value.AntrianDescription,
                ListPasien: a.Value.ListEntry
                    .Select(e => new AntrianListDtlResponse(
                        AntrianId: a.Value.AntrianId,
                        PasienName: e.Visitor.PersonName, 
                        NoUrut: e.NoUrut,
                        Status: (int)e.AntrianStatus,
                        StatusString: e.AntrianStatus.ToString(),
                        ReffId : e.ReffId,
                        ReffDesc : e.ReffDesc
                ))
            ))
            .ToList();

        return Task.FromResult(result.AsEnumerable());

    }
}
