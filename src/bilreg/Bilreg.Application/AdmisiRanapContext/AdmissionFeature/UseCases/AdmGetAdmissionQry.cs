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
    KelasReff KelasRawat,
    BangsalReff Bangsal,
    DateTime AdmissionDate,
    DateTime CrtDate);

public class AdmGetAdmissionHandler : IRequestHandler<AdmGetAdmissionQry, AdmGetAdmissionResponse>
{
    private readonly IAdmissionRepo _admissionRepo;

    public AdmGetAdmissionHandler(IAdmissionRepo admissionRepo) =>
        _admissionRepo = admissionRepo;

    public Task<AdmGetAdmissionResponse> Handle(
        AdmGetAdmissionQry request,
        CancellationToken cancellationToken)
    {
        var admission = _admissionRepo.LoadEntity(request)
            .GetValueOrThrow($"Admission '{request.RegId}' tidak ditemukan.");

        return Task.FromResult(new AdmGetAdmissionResponse(
            admission.RegId,
            admission.AdmissionStatus,
            admission.Pasien,
            admission.OpnameRequestId,
            admission.ReservationId,
            admission.KelasRawat,
            admission.Bangsal,
            admission.AdmissionDate,
            admission.AuditTrail.Created.Timestamp));
    }
}
