using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.DigitalSignFeature.UseCases;

public record AdmGetDigitalSignQry(
    string RegId,
    string? DokumenId = null) : IRequest<AdmGetDigitalSignListResponse>;

public record AdmGetDigitalSignResponse(
    string SigningRequestId,
    string RegId,
    string HisReference,
    string DokumenId,
    string FileName);

public record AdmGetDigitalSignListResponse(IEnumerable<AdmGetDigitalSignResponse> Items);

public class AdmGetDigitalSignHandler
    : IRequestHandler<AdmGetDigitalSignQry, AdmGetDigitalSignListResponse>
{
    private readonly IRanapDigitalSignRepo _digitalSignRepo;

    public AdmGetDigitalSignHandler(IRanapDigitalSignRepo digitalSignRepo) =>
        _digitalSignRepo = digitalSignRepo;

    public Task<AdmGetDigitalSignListResponse> Handle(
        AdmGetDigitalSignQry request,
        CancellationToken cancellationToken)
    {
        // GET cukup 2 varian: tunggal (regId + dokumenId) dan list (regId).
        if (string.IsNullOrWhiteSpace(request.RegId))
            throw new ArgumentException("RegId wajib diisi.", nameof(request.RegId));

        var regId = request.RegId.Trim();

        List<AdmGetDigitalSignResponse> items;
        if (!string.IsNullOrWhiteSpace(request.DokumenId))
        {
            var dokumenId = request.DokumenId.Trim();
            var model = _digitalSignRepo.LoadByRegDokumen(regId, dokumenId);
            items = !model.HasValue
                ? []
                : [Map(model.Value)];
        }
        else
        {
            items = _digitalSignRepo.ListByRegId(regId)
                .Select(Map)
                .ToList();
        }

        return Task.FromResult(new AdmGetDigitalSignListResponse(items));
    }

    private static AdmGetDigitalSignResponse Map(Domain.AdmisiRanapContext.DigitalSignFeature.RanapDigitalSignModel m) =>
        new(
            m.SigningRequestId,
            m.RegId,
            m.HisReference,
            m.DokumenId,
            m.FileName);
}
