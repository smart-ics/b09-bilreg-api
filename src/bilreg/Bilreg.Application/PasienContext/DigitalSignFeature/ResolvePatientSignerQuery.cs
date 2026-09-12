using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Param;
using MediatR;

namespace Bilreg.Application.PasienContext.DigitalSignFeature;

public record ResolvePatientSignerQuery(string NoMr) : IRequest<ResolvePatientSignerResponse>;

public record ResolvePatientSignerResponse(
    HiDokPatientSignerResolveStatus Status,
    string UserrId,
    string SignerId,
    string ErrorMessage,
    ResolvePatientSignerPatientInfo? Patient);

public record ResolvePatientSignerPatientInfo(
    string UserrId,
    string NoMR,
    string PasienName,
    string Alamat,
    string TglLahir,
    string NoTelp,
    string NoKTP,
    string RSID);

public class ResolvePatientSignerQueryHandler : IRequestHandler<ResolvePatientSignerQuery, ResolvePatientSignerResponse>
{
    private readonly IGetProjectIdService _projectId;
    private readonly IHiDokPatientSignerResolveClient _client;
    private readonly IPasienRepo _pasienRepo;

    public ResolvePatientSignerQueryHandler(IGetProjectIdService projectId,
        IHiDokPatientSignerResolveClient client,
        IPasienRepo pasienRepo)
    {
        _projectId = projectId;
        _client = client;
        _pasienRepo = pasienRepo;
    }

    public Task<ResolvePatientSignerResponse> Handle(ResolvePatientSignerQuery request, CancellationToken cancellationToken)
    {
        var hospitalId = _projectId.Execute();

        var result = _client.Execute(new HiDokPatientSignerResolveRequest(hospitalId, request.NoMr));

        ResolvePatientSignerPatientInfo? patient = null;
        if (result.Status is HiDokPatientSignerResolveStatus.NotFound
            or HiDokPatientSignerResolveStatus.NotVerified
            or HiDokPatientSignerResolveStatus.ProvisionFailed)
        {
            patient = TryLoadPatient(request.NoMr, hospitalId, result.UserrId);
        }

        var response = new ResolvePatientSignerResponse(
            result.Status, result.UserrId, result.SignerId, result.ErrorMessage, patient);
        return Task.FromResult(response);
    }

    private ResolvePatientSignerPatientInfo? TryLoadPatient(string mr, string hospitalId, string userrId)
    {
        var pasien = _pasienRepo.LoadEntity(PasienModel.Key(mr));
        if (!pasien.HasValue)
            return null;

        var model = pasien.Value;

        var noTelp = model.ListContact
            .FirstOrDefault(x => x.JenisContact == JenisContactEnum.Mobile)
            ?.ContactDetail;
        if (string.IsNullOrWhiteSpace(noTelp) || noTelp == "-")
            noTelp = model.Person.Contact.ContactDetail;

        return new ResolvePatientSignerPatientInfo(
            userrId,
            mr,
            model.Person.PersonName,
            BuildAlamat(model.Person.Alamat),
            model.Person.TglLahir.ToString("dd-MM-yyyy"),
            NormalizeEmpty(noTelp),
            NormalizeEmpty(model.Ktp.Nik),
            hospitalId);
    }

    private static string BuildAlamat(AlamatType alamat)
    {
        var parts = alamat.Alamat
            .Where(x => !string.IsNullOrWhiteSpace(x) && x != "-")
            .ToList();

        return parts.Count == 0 ? string.Empty : string.Join(", ", parts);
    }

    private static string NormalizeEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) || value == "-" ? string.Empty : value.Trim();
}