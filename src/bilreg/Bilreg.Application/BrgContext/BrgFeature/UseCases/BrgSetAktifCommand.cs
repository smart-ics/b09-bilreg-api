using Ardalis.GuardClauses;
using Bilreg.Domain.BrgContext.BrgFeature;
using MediatR;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

public record BrgSetAktifCommand(string BrgId, bool IsAktif) : IRequest, IBrgKey
{
    public string BarangId => BrgId;
}

public class BrgSetAktifHandler : IRequestHandler<BrgSetAktifCommand>
{
    private readonly IBrgRepo _brgRepo;

    public BrgSetAktifHandler(IBrgRepo brgRepo)
    {
        _brgRepo = brgRepo;
    }

    public Task Handle(BrgSetAktifCommand request, CancellationToken cancellationToken)
    {
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));

        var brg = _brgRepo.LoadEntity(new BrgKey(request.BrgId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang '{request.BrgId}' tidak ditemukan.")
            );

        var updated = brg is BarangModel bm
            ? bm.SetAktif(request.IsAktif)
            : BarangModel.Create(
                brg.BrgId,
                brg.BrgName,
                request.IsAktif,
                brg.KetBarang,
                brg.GroupRek,
                brg.GroupRekDk,
                brg.Pabrik,
                brg.SatuanKecil,
                brg.SatuanBesar,
                brg.Konversi);

        _brgRepo.SaveChanges(updated);

        return Task.CompletedTask;
    }
}
