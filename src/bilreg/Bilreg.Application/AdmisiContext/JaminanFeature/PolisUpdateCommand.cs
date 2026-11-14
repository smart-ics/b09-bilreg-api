using System.Globalization;
using Ardalis.GuardClauses;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;

namespace Bilreg.Application.AdmisiContext.JaminanFeature;

public record PolisUpdateCommand(
    string PolisId,
    string PasienId,
    string NoPolis,
    string AtasName,
    string ExpiredDate,
    string KelasRanapId,
    string StatusPesertaId,
    bool IsCoverRajal) : IRequest<PolisUpdateResponse>, IPolisKey, IPasienKey;

public record PolisUpdateResponse(string PolisId);

public class PolisUpdateHandler : IRequestHandler<PolisUpdateCommand, PolisUpdateResponse>
{
    private readonly IPolisRepo _polisRepo;
    private readonly IPasienRepo _pasienRepo;
    private readonly IKelasRepo _kelasRepo;

    public PolisUpdateHandler(
        IPolisRepo polisRepo,
        IPasienRepo pasienRepo,
        IKelasRepo kelasRepo)
    {
        _polisRepo = polisRepo;
        _pasienRepo = pasienRepo;
        _kelasRepo = kelasRepo;
    }

    public Task<PolisUpdateResponse> Handle(PolisUpdateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.PolisId);
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.NoPolis);
        Guard.Against.NullOrWhiteSpace(request.AtasName);
        Guard.Against.NullOrWhiteSpace(request.ExpiredDate);
        Guard.Against.NullOrWhiteSpace(request.KelasRanapId);
        Guard.Against.NullOrWhiteSpace(request.StatusPesertaId);

        var expiredDate = DateOnly.ParseExact(request.ExpiredDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var polis = _polisRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Polis {request.PolisId} not found")
            );

        var pasien = _pasienRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pasien {request.PasienId} not found")
            );

        var kelas = _kelasRepo.LoadEntity(KelasType.Key(request.KelasRanapId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Kelas {request.KelasRanapId} not found")
            );

        var statusPeserta = StatusPesertaType.Create(request.StatusPesertaId);

        polis.Update(request.NoPolis, request.AtasName, expiredDate, kelas.ToReff(), request.IsCoverRajal, pasien, statusPeserta);
        _polisRepo.SaveChanges(polis);

        return Task.FromResult(new PolisUpdateResponse(polis.PolisId));
    }
}
