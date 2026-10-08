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
    string FileName,
    string SignerId = "",
    string PatientSignState = "",
    string OfficerSignState = "",
    string OfficerRef = "",
    string OfficerEmail = "",
    string OfficerName = "",
    string OftaDocId = "",
    string ExternalDocumentId = "",
    string CombinedStatus = "Sebagian",
    bool IsArchived = false,
    string ArchiveId = "",
    DateTime? ArchiveDate = null);

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
            SigningRequestId: m.SigningRequestId,
            RegId: m.RegId,
            HisReference: m.HisReference,
            DokumenId: m.DokumenId,
            FileName: m.FileName,
            SignerId: m.SignerId,
            PatientSignState: m.PatientSignState,
            OfficerSignState: m.OftaSignState,
            OfficerRef: m.OfficerRef,
            OfficerEmail: m.OfficerEmail,
            OfficerName: m.OfficerName,
            OftaDocId: m.OftaDocId,
            ExternalDocumentId: m.ExternalDocumentId,
            CombinedStatus: m.CombinedStatus,
            IsArchived: m.IsArchived,
            ArchiveId: m.ArchiveId,
            ArchiveDate: m.ArchiveDate);
}
