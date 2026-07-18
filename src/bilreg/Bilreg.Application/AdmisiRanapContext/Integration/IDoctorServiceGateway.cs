using Bilreg.Domain.AdmisiContext.PpaFeature;

namespace Bilreg.Application.AdmisiRanapContext.Integration;

public interface IDoctorServiceGateway
{
    PpaReff ResolveDoctor(string dokterId);
}
