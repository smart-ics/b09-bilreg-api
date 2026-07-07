using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmLookupAdmissionQry(
    AdmissionStatusEnum? Status = null,
    string? PasienId = null) : IRequest<AdmLookupAdmissionResponse>;

public record AdmLookupAdmissionResponse(IReadOnlyList<AdmAdmissionListItem> Items);

public record AdmAdmissionListItem(
    string RegId,
    AdmissionStatusEnum AdmissionStatus,
    string PasienId,
    string PasienName,
    string KelasId,
    string KelasName,
    string BangsalId,
    string BangsalName,
    DateTime AdmissionDate);

public class AdmLookupAdmissionHandler : IRequestHandler<AdmLookupAdmissionQry, AdmLookupAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;

    public AdmLookupAdmissionHandler(IAdmissionRepo admissionRepo) =>
        _admissionRepo = admissionRepo;

    public Task<AdmLookupAdmissionResponse> Handle(
        AdmLookupAdmissionQry request,
        CancellationToken cancellationToken)
    {
        var items = _admissionRepo
            .ListData(new AdmissionListFilter(request.Status, request.PasienId))
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmLookupAdmissionResponse(items));
    }

    private static AdmAdmissionListItem Map(AdmissionModel admission) =>
        new(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien.PasienId,
            admission.Pasien.PasienName,
            admission.KelasRawat.KelasId,
            admission.KelasRawat.KelasName,
            admission.Bangsal.BangsalId,
            admission.Bangsal.BangsalName,
            admission.AdmissionDate);
}
