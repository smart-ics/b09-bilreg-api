using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using MediatR;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

public record BrgUpdateCommand(
    string BrgId,
    string BrgName,
    string KetBarang,
    string GroupRekId,
    string PabrikId,
    string SatuanKecilId,
    string SatuanBesarId,
    int Konversi
) : IRequest, IBrgKey;

public class BrgUpdateHandler : IRequestHandler<BrgUpdateCommand>
{
    private readonly IBrgRepo _brgRepo;
    private readonly IBrgStockMovementCheck _stockMovementCheck;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly IPabrikRepo _pabrikRepo;
    private readonly ISatuanRepo _satuanRepo;

    public BrgUpdateHandler(
        IBrgRepo brgRepo,
        IBrgStockMovementCheck stockMovementCheck,
        IGroupRekRepo groupRekRepo,
        IPabrikRepo pabrikRepo,
        ISatuanRepo satuanRepo)
    {
        _brgRepo = brgRepo;
        _stockMovementCheck = stockMovementCheck;
        _groupRekRepo = groupRekRepo;
        _pabrikRepo = pabrikRepo;
        _satuanRepo = satuanRepo;
    }

    public Task Handle(BrgUpdateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.BrgName, nameof(request.BrgName));
        Guard.Against.NullOrWhiteSpace(request.GroupRekId, nameof(request.GroupRekId));
        Guard.Against.NullOrWhiteSpace(request.PabrikId, nameof(request.PabrikId));
        Guard.Against.NullOrWhiteSpace(request.SatuanKecilId, nameof(request.SatuanKecilId));
        Guard.Against.NullOrWhiteSpace(request.SatuanBesarId, nameof(request.SatuanBesarId));
        if (request.Konversi < 1)
            throw new ArgumentException("Konversi harus >= 1.", nameof(request.Konversi));

        // LOAD EXISTING
        var existingBrg = _brgRepo.LoadEntity(new BrgKey(request.BrgId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang '{request.BrgId}' not found.")
            );

        // IMMUTABILITY GUARD (AC-04)
        bool isSatuanKecilChanged = !string.Equals(
            existingBrg.SatuanKecil.SatuanId,
            request.SatuanKecilId,
            StringComparison.OrdinalIgnoreCase);

        bool isKonversiChanged = existingBrg.Konversi != request.Konversi;

        if (isSatuanKecilChanged || isKonversiChanged)
        {
            if (_stockMovementCheck.HasStockMovementOrBalance(request.BrgId))
            {
                throw new InvalidOperationException(
                    $"Satuan kecil atau rasio konversi untuk barang '{existingBrg.BrgName}' ({existingBrg.BrgId}) tidak dapat diubah karena barang sudah memiliki riwayat mutasi atau saldo stok.");
            }
        }

        // RESOLVE REFERENCES
        var groupRek = _groupRekRepo.LoadEntity(GroupRekType.Key(request.GroupRekId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"GroupRek '{request.GroupRekId}' not found.")
            );

        var pabrik = _pabrikRepo.LoadEntity(PabrikType.Key(request.PabrikId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pabrik '{request.PabrikId}' not found.")
            );

        var satuanKecil = _satuanRepo.LoadEntity(SatuanType.Key(request.SatuanKecilId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"SatuanKecil '{request.SatuanKecilId}' not found.")
            );

        var satuanBesar = string.Equals(request.SatuanKecilId, request.SatuanBesarId, StringComparison.OrdinalIgnoreCase)
            ? satuanKecil
            : _satuanRepo.LoadEntity(SatuanType.Key(request.SatuanBesarId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"SatuanBesar '{request.SatuanBesarId}' not found.")
                );

        // BUILD UPDATED MODEL
        var updatedBrg = BarangModel.Create(
            brgId: existingBrg.BrgId,
            brgName: request.BrgName,
            isAktif: existingBrg.IsAktif,
            ketBarang: request.KetBarang ?? string.Empty,
            groupRek: groupRek.ToReff(),
            groupRekDk: groupRek.GroupRekDk,
            pabrik: pabrik.ToReff(),
            satuanKecil: satuanKecil,
            satuanBesar: satuanBesar,
            konversi: request.Konversi);

        // PERSIST
        _brgRepo.SaveChanges(updatedBrg);

        return Task.CompletedTask;
    }
}
