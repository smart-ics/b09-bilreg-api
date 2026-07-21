using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmProcessAdmissionResponse(
    string RegId,
    AdmissionStatusEnum AdmissionStatus);
