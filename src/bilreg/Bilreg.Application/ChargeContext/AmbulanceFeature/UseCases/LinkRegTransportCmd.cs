using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using MediatR;

namespace Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;

public record LinkRegTransportCmd(
    string TrsTransportId,
    string RegId) : IRequest<LinkRegTransportResponse>, ITrsTransportKey, IRegKey;

public record LinkRegTransportResponse(
    string TrsTransportId,
    string RegId);

public class LinkRegTransportHandler : IRequestHandler<LinkRegTransportCmd, LinkRegTransportResponse>
{
    private readonly ITrsTransportRepo _trsTransportRepo;
    private readonly IRegRepo _regRepo;

    public LinkRegTransportHandler(
        ITrsTransportRepo trsTransportRepo,
        IRegRepo regRepo)
    {
        _trsTransportRepo = trsTransportRepo;
        _regRepo = regRepo;
    }

    public Task<LinkRegTransportResponse> Handle(LinkRegTransportCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.TrsTransportId, nameof(request.TrsTransportId));
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));

        var transport = _trsTransportRepo.LoadEntity(request).Match(
            onSome: t => t,
            onNone: () => throw new KeyNotFoundException($"Transaksi transport dengan id '{request.TrsTransportId}' tidak ditemukan."));

        var reg = _regRepo.LoadEntity(request).Match(
            onSome: r => r,
            onNone: () => throw new KeyNotFoundException($"Registrasi dengan id '{request.RegId}' tidak ditemukan."));

        if (!reg.IsAktif)
            throw new ArgumentException($"Registrasi '{request.RegId}' tidak aktif.", nameof(request.RegId));

        transport.LinkRegistration(reg.RegId);
        _trsTransportRepo.SaveChanges(transport);

        return Task.FromResult(new LinkRegTransportResponse(transport.TrsTransportId, transport.RegId));
    }
}
