using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiRanapContext.Integration;
using Bilreg.Domain.BedUsageContext.WardFeature;
using MediatR;

namespace Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;

public record AdmListEligibleBangsalQry(string KelasDkId) : IRequest<AdmListEligibleBangsalResponse>;

public record AdmListEligibleBangsalResponse(IReadOnlyList<AdmEligibleBangsalItem> Items);

public record AdmEligibleBangsalItem(string BangsalId, string BangsalName);

public class AdmListEligibleBangsalHandler : IRequestHandler<AdmListEligibleBangsalQry, AdmListEligibleBangsalResponse>
{
    private readonly IWardAccommodationGateway _wardGateway;

    public AdmListEligibleBangsalHandler(IWardAccommodationGateway wardGateway) =>
        _wardGateway = wardGateway;

    public Task<AdmListEligibleBangsalResponse> Handle(
        AdmListEligibleBangsalQry request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.KelasDkId);

        _wardGateway.ResolveKelasDk(request.KelasDkId);

        var items = _wardGateway
            .ListEligibleBangsal(request.KelasDkId)
            .Select(b => new AdmEligibleBangsalItem(b.BangsalId, b.BangsalName))
            .ToList();

        return Task.FromResult(new AdmListEligibleBangsalResponse(items));
    }
}
