using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.Integration;

public class PatientAdministrationGateway : IPatientAdministrationGateway
{
    private readonly IPasienRepo _pasienRepo;

    public PatientAdministrationGateway(IPasienRepo pasienRepo) => _pasienRepo = pasienRepo;

    public PasienReff ResolvePatient(string pasienId)
    {
        Guard.Against.NullOrWhiteSpace(pasienId);
        return _pasienRepo.LoadEntity(PasienModel.Key(pasienId))
            .GetValueOrThrow($"Pasien '{pasienId}' tidak ditemukan.")
            .ToReff();
    }
}
