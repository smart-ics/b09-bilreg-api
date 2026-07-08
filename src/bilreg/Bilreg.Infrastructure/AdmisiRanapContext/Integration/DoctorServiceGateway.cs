using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.PpaFeature;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiRanapContext.Integration;

public class DoctorServiceGateway : IDoctorServiceGateway
{
    private readonly IPpaRepo _ppaRepo;

    public DoctorServiceGateway(IPpaRepo ppaRepo) => _ppaRepo = ppaRepo;

    public PpaReff ResolveDoctor(string dokterId)
    {
        Guard.Against.NullOrWhiteSpace(dokterId);
        return _ppaRepo.LoadEntity(PpaType.Key(dokterId))
            .GetValueOrThrow($"Dokter '{dokterId}' tidak ditemukan.")
            .ToReff();
    }
}
