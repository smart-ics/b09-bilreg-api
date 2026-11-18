using Ardalis.GuardClauses;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record DispatchTransportCmd(
    string TrsTransportId,
    string AmbulanceId,
    string DriverId,
    decimal StartOdometer,
    string? NoPolisi = null,
    string? PerawatId = null,
    string? DokterId = null,
    DateTime? DispatchTime = null) : IRequest<DispatchTransportResponse>, ITrsTransportKey, IAmbulanceKey;

public record DispatchTransportResponse(
    string TrsTransportId,
    string Status,
    DateTime? DispatchTime);

public class DispatchTransportHandler : IRequestHandler<DispatchTransportCmd, DispatchTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IAmbulanceRepo _ambulanceRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public DispatchTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        IAmbulanceRepo ambulanceRepo,
        ITglJamProvider tglJamProvider)
    {
        _trsTransportRepo = trsTransportRepo;
        _ambulanceRepo = ambulanceRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<DispatchTransportResponse> Handle(DispatchTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));
        Guard.Against.NullOrWhiteSpace(request.AmbulanceId, nameof(request.AmbulanceId));
        Guard.Against.NullOrWhiteSpace(request.DriverId, nameof(request.DriverId));

        if (request.StartOdometer < 0)
            throw new ArgumentException("Start odometer tidak boleh negatif.", nameof(request.StartOdometer));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        // TD-04: Anti-Overlapping Concurrency Guard
        if (_trsTransportRepo.HasActiveDispatchForAmbulance(request.AmbulanceId, request.TrsTransportId))
            throw new InvalidOperationException($"Ambulans '{request.AmbulanceId}' sedang bertugas pada penugasan aktif lain.");

        if (_trsTransportRepo.HasActiveDispatchForDriver(request.DriverId, request.TrsTransportId))
            throw new InvalidOperationException($"Pengemudi '{request.DriverId}' sedang bertugas pada penugasan aktif lain.");

        var ambulance = _ambulanceRepo.LoadEntity(new AmbulanceKey(request.AmbulanceId)).Match(
            onSome: a => a,
            onNone: () => throw new KeyNotFoundException($"Master ambulans dengan id '{request.AmbulanceId}' tidak ditemukan."));

        if (ambulance.StatusArmada == AmbulanceStatusEnum.Maintenance)
            throw new InvalidOperationException($"Ambulans '{request.AmbulanceId}' tidak dapat ditugaskan karena berstatus Maintenance.");

        var noPolisi = !string.IsNullOrWhiteSpace(request.NoPolisi)
            ? request.NoPolisi
            : ambulance.NoPolisi;

        var dispatchTime = request.DispatchTime ?? _tglJamProvider.Now;

        transport.Dispatch(
            request.AmbulanceId,
            request.DriverId,
            noPolisi,
            request.StartOdometer,
            request.PerawatId,
            request.DokterId,
            dispatchTime);

        _trsTransportRepo.SaveChanges(transport);

        return Task.FromResult(new DispatchTransportResponse(
            transport.TrsTransportId,
            transport.Status.ToString(),
            transport.TglDispatch));
    }
}
