using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.BedUsageContext.WardFeature;
using Bilreg.Application.Helpers;
using Bilreg.Application.PasienContext.PasienFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using MediatR;
using System.Globalization;
using Ardalis.GuardClauses;

namespace Bilreg.Application.AdmisiContext.JaminanSub.PolisAgg;

public record PolisCreateCommand(
    string PasienId,
    string TipeJaminanId,
    string NoPolis,
    string AtasName,
    string ExpiredDate,
    string KelasRanapId,
    bool IsCoverRajal)
    : IRequest<PolisCreateResponse>, IPasienKey, ITipeJaminanKey;

public record PolisCreateResponse(string PolisId);

public class PolisCreateHandler : IRequestHandler<PolisCreateCommand, PolisCreateResponse>
{
    private readonly IPasienRepo _pasienRepo;
    private readonly ITipeJaminanRepo _tipeJaminanRepo;
    private readonly IKelasRepo _kelasRepo;
    private readonly IPolisFactory _polisFactory;
    private readonly IPolisRepo _polisRepo;

    public PolisCreateHandler(IPasienRepo pasienRepo,
        ITipeJaminanRepo tipeJaminanRepo,
        IKelasRepo kelasRepo,
        IPolisFactory polisFactory,
        IPolisRepo polisRepo)
    {
        _pasienRepo = pasienRepo;
        _tipeJaminanRepo = tipeJaminanRepo;
        _kelasRepo = kelasRepo;
        _polisFactory = polisFactory;
        _polisRepo = polisRepo;
    }

    public Task<PolisCreateResponse> Handle(PolisCreateCommand request, CancellationToken cancellationToken)
    {
        //  GUARD
        Guard.Against.NullOrWhiteSpace(request.PasienId);
        Guard.Against.NullOrWhiteSpace(request.TipeJaminanId);
        Guard.Against.NullOrWhiteSpace(request.NoPolis);
        Guard.Against.NullOrWhiteSpace(request.AtasName);
        Guard.Against.NullOrWhiteSpace(request.ExpiredDate);
        Guard.Against.NullOrEmpty(request.KelasRanapId);

        //  BUILD
        var pasien = _pasienRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"pasien {request.PasienId} not found")
            );
        var tipeJaminan = _tipeJaminanRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"tipe jaminan {request.TipeJaminanId} not found")
            );
        var kelas = _kelasRepo.LoadEntity(KelasType.Key(request.KelasRanapId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"kelas {request.KelasRanapId} not found")
            );

        var polis = CekPeserta(pasien, request);
        if (polis is null)
        {
            polis = CreatePolis(request, pasien, kelas, tipeJaminan);
            _polisRepo.SaveChanges(polis);
        }

        return Task.FromResult(new PolisCreateResponse(polis.PolisId));
    }

    private PolisModel? CekPeserta(PasienModel pasien, ITipeJaminanKey key)
    {
        var peserta = _polisRepo.ListData(pasien)
            .FirstOrDefault(x => x.TipeJaminan.TipeJaminanId == key.TipeJaminanId);

        if (peserta is null)
            return null;

        return _polisRepo.LoadEntity(PolisModel.Key(peserta.PolisId))
            .Match(x => x, () => null!);
    }



    private PolisModel CreatePolis(PolisCreateCommand cmd, PasienModel pasien, KelasType kelas,
        TipeJaminanType tipeJaminan)
    {
        var statusPeserta = StatusPesertaType.Create("P");
        DateOnly expiredDate = DateOnly.ParseExact(cmd.ExpiredDate, "yyyy-MM-dd", CultureInfo.InvariantCulture);

        var polis = _polisFactory.Create(pasien, tipeJaminan, kelas,
            cmd.NoPolis, cmd.AtasName, statusPeserta, expiredDate,
            cmd.IsCoverRajal);

        return polis;
    }
}
