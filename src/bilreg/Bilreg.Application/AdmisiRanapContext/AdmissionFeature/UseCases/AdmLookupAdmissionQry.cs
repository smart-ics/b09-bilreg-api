using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmLookupAdmissionQry(
    AdmissionStatusEnum? Status = null,
    string? PasienId = null,
    string? Search = null) : IRequest<AdmLookupAdmissionResponse>;

public record AdmLookupAdmissionResponse(IReadOnlyList<AdmAdmissionListItem> Items);

public record AdmAdmissionListItem(
    string RegId,
    AdmissionStatusEnum AdmissionStatus,
    string PasienId,
    string PasienName,
    string KelasDkId,
    string KelasDkName,
    string BangsalId,
    string BangsalName,
    DateTime AdmissionDate,
    string JenisInapId = "",
    string JenisInapName = "",
    string CurrentKelasDkId = "",
    string CurrentKelasDkName = "",
    string CurrentBangsalId = "",
    string CurrentBangsalName = "");

public class AdmLookupAdmissionHandler : IRequestHandler<AdmLookupAdmissionQry, AdmLookupAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IAdmissionCurrentLocationResolver? _locationResolver;

    public AdmLookupAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IAdmissionCurrentLocationResolver? locationResolver = null)
    {
        _admissionRepo = admissionRepo;
        _locationResolver = locationResolver;
    }

    public Task<AdmLookupAdmissionResponse> Handle(
        AdmLookupAdmissionQry request,
        CancellationToken cancellationToken)
    {
        var items = _admissionRepo
            .ListData(new AdmissionListFilter(request.Status, request.PasienId, request.Search))
            .Select(Map)
            .ToList();

        return Task.FromResult(new AdmLookupAdmissionResponse(items));
    }

    private AdmAdmissionListItem Map(AdmissionModel admission)
    {
        var isActive = admission.AdmissionStatus is not (AdmissionStatusEnum.Completed or AdmissionStatusEnum.Cancelled);
        var currentLocation = isActive && _locationResolver is not null
            ? _locationResolver.Resolve(admission.RegId, admission.KelasDk, admission.Bangsal)
            : new ActiveLocationResult(admission.KelasDk, admission.Bangsal);

        return new(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien.PasienId,
            admission.Pasien.PasienName,
            admission.KelasDk.KelasDkId,
            admission.KelasDk.KelasDkName,
            admission.Bangsal.BangsalId,
            admission.Bangsal.BangsalName,
            admission.AdmissionDate,
            admission.JenisInap.JenisInapId,
            admission.JenisInap.JenisInapName,
            currentLocation.KelasDk.KelasDkId,
            currentLocation.KelasDk.KelasDkName,
            currentLocation.Bangsal.BangsalId,
            currentLocation.Bangsal.BangsalName);
    }
}
