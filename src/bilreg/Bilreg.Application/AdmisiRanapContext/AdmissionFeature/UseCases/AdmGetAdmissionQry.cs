using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmGetAdmissionQry(string RegId) : IRequest<AdmGetAdmissionResponse>, IRegKey;

public record AdmGetAdmissionResponse(
    string RegId,
    AdmissionStatusEnum AdmissionStatus,
    PasienReff Pasien,
    string OpnameRequestId,
    string ReservationId,
    KelasDkType KelasDk,
    BangsalReff Bangsal,
    DateTime AdmissionDate,
    DateTime CrtDate,
    string CaraMasukDkId = "",
    string DokterId = "",
    string ProsedurMasukInapId = "",
    string JenisInapId = "1",
    string RegIbu = "");

public class AdmGetAdmissionHandler : IRequestHandler<AdmGetAdmissionQry, AdmGetAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;
    private readonly IRegRepo _regRepo;
    private readonly IRegInapRepo _regInapRepo;

    public AdmGetAdmissionHandler(
        IAdmissionRepo admissionRepo,
        IRegRepo regRepo,
        IRegInapRepo regInapRepo)
    {
        _admissionRepo = admissionRepo;
        _regRepo = regRepo;
        _regInapRepo = regInapRepo;
    }

    public Task<AdmGetAdmissionResponse> Handle(
        AdmGetAdmissionQry request,
        CancellationToken cancellationToken)
    {
        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");

        var reg = _regRepo.LoadEntity(request);
        var regInap = _regInapRepo.LoadEntity(request);

        var caraMasukDkId = reg.Match(
            r => Normalize(r.CaraMasukDk?.CaraMasukDkId),
            () => string.Empty);

        var dpjpId = regInap.Match(
            ri => Normalize(ri.Dpjp?.PpaId),
            () => string.Empty);

        var dokterId = !string.IsNullOrEmpty(dpjpId)
            ? dpjpId
            : reg.Match(r => Normalize(r.Dokter?.PpaId), () => string.Empty);

        var prosedurMasukInapId = regInap.Match(
            ri => Normalize(ri.ProsedurMasukInap?.ProsedurMasukInapId),
            () => string.Empty);

        var jenisInapId = reg.Match(
            r => Normalize(r.JenisInap?.JenisInapId),
            () => "1");
        if (string.IsNullOrEmpty(jenisInapId))
        {
            jenisInapId = "1";
        }

        var regIbu = reg.Match(
            r => Normalize(r.RegIbu),
            () => string.Empty);

        return Task.FromResult(new AdmGetAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien,
            admission.OpnameRequestId,
            admission.ReservationId,
            admission.KelasDk,
            admission.Bangsal,
            admission.AdmissionDate,
            admission.AuditTrail.Created.Timestamp,
            caraMasukDkId,
            dokterId,
            prosedurMasukInapId,
            jenisInapId,
            regIbu));
    }

    private static string Normalize(string? value)
    {
        var trimmed = (value ?? string.Empty).Trim();
        return trimmed == "-" ? string.Empty : trimmed;
    }
}
