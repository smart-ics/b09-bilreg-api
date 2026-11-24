using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record TransportItemAdjustmentDto(
    string DetilTarifId,
    decimal Tarif,
    decimal Diskon = 0m);

public record UpdateTransportDetailCmd(
    string TrsTransportId,
    string? DriverId = null,
    string? PerawatId = null,
    string? DokterId = null,
    IEnumerable<TransportItemAdjustmentDto>? Items = null) : IRequest<UpdateTransportDetailResponse>, ITrsTransportKey;

public record UpdateTransportDetailResponse(
    string TrsTransportId,
    string DriverId,
    string PerawatId,
    string DokterId,
    decimal SubTotal,
    decimal Diskon,
    decimal Total);

public class UpdateTransportDetailHandler : IRequestHandler<UpdateTransportDetailCmd, UpdateTransportDetailResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;

    public UpdateTransportDetailHandler(ITrsTransportRepo trsTransportRepo)
    {
        _trsTransportRepo = trsTransportRepo;
    }

    public Task<UpdateTransportDetailResponse> Handle(UpdateTransportDetailCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        if (request.DriverId is not null || request.PerawatId is not null || request.DokterId is not null)
        {
            transport.UpdateCrew(request.DriverId, request.PerawatId, request.DokterId);
        }

        if (request.Items != null)
        {
            var adjustments = request.Items.Select(x => (x.DetilTarifId, x.Tarif, x.Diskon));
            transport.AdjustItems(adjustments);
        }

        _trsTransportRepo.SaveChanges(transport);

        return Task.FromResult(new UpdateTransportDetailResponse(
            transport.TrsTransportId,
            transport.DriverId,
            transport.PerawatId,
            transport.DokterId,
            transport.SubTotal,
            transport.Diskon,
            transport.Total));
    }
}
