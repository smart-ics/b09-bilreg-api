using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record AmbulanceSaveCommand(
    string AmbulanceId,
    string AmbulanceName,
    string NoPolisi = "",
    string TipeArmada = "Transport",
    string StatusArmada = "Available",
    decimal Abonement = 0) : IRequest<AmbulanceSaveResponse>, IAmbulanceKey;

public record AmbulanceSaveResponse(string AmbulanceId);

public class AmbulanceSaveHandler : IRequestHandler<AmbulanceSaveCommand, AmbulanceSaveResponse>
{
    private readonly IAmbulanceRepo _ambulanceRepo;

    public AmbulanceSaveHandler(IAmbulanceRepo ambulanceRepo)
    {
        _ambulanceRepo = ambulanceRepo;
    }

    public Task<AmbulanceSaveResponse> Handle(AmbulanceSaveCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.AmbulanceName, nameof(request.AmbulanceName));

        var ambulance = _ambulanceRepo.LoadEntity(request).Match(
            onSome: existing =>
            {
                existing.SetName(request.AmbulanceName);
                existing.SetNoPolisi(request.NoPolisi);
                existing.SetTipeArmada(request.TipeArmada);
                existing.SetStatusArmada(request.StatusArmada);
                existing.SetAbonement(request.Abonement);
                return existing;
            },
            onNone: () =>
            {
                var created = new AmbulanceModel(
                    request.AmbulanceId,
                    request.AmbulanceName,
                    request.NoPolisi,
                    request.TipeArmada,
                    request.StatusArmada);
                created.SetAbonement(request.Abonement);
                return created;
            });

        _ambulanceRepo.SaveChanges(ambulance);
        return Task.FromResult(new AmbulanceSaveResponse(ambulance.AmbulanceId));
    }
}
