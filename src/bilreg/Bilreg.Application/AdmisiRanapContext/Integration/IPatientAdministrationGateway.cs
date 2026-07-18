using Bilreg.Domain.PasienContext.PasienFeature;

namespace Bilreg.Application.AdmisiRanapContext.Integration;

public interface IPatientAdministrationGateway
{
    PasienReff ResolvePatient(string pasienId);
}
