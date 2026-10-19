using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.KlasifikasiFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.KlasifikasiFeature;
using MediatR;

namespace Bilreg.Application.BrgContext.BrgFeature.UseCases;

public record BrgRegisterCommand(
    string BrgId,
    string BrgName,
    string KetBarang,
    string GroupRekId,
    string PabrikId,
    string SatuanKecilId,
    string SatuanBesarId,
    int Konversi
) : IRequest<BrgRegisterResponse>, IBrgKey
{
    public string BarangId => BrgId;
}

public record BrgRegisterResponse(string BrgId);

public class BrgRegisterHandler : IRequestHandler<BrgRegisterCommand, BrgRegisterResponse>
{
    private readonly IBrgRepo _brgRepo;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly IPabrikRepo _pabrikRepo;
    private readonly ISatuanRepo _satuanRepo;

    public BrgRegisterHandler(
        IBrgRepo brgRepo,
        IGroupRekRepo groupRekRepo,
        IPabrikRepo pabrikRepo,
        ISatuanRepo satuanRepo)
    {
        _brgRepo = brgRepo;
        _groupRekRepo = groupRekRepo;
        _pabrikRepo = pabrikRepo;
        _satuanRepo = satuanRepo;
    }

    public Task<BrgRegisterResponse> Handle(BrgRegisterCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        if (request.BrgId.Length > 13)
        {
            throw new ArgumentException("BarangId tidak boleh lebih dari 13 karakter.", nameof(request.BrgId));
        }

        Guard.Against.NullOrWhiteSpace(request.BrgName, nameof(request.BrgName));
        Guard.Against.NullOrWhiteSpace(request.GroupRekId, nameof(request.GroupRekId));
        Guard.Against.NullOrWhiteSpace(request.PabrikId, nameof(request.PabrikId));
        Guard.Against.NullOrWhiteSpace(request.SatuanKecilId, nameof(request.SatuanKecilId));
        Guard.Against.NullOrWhiteSpace(request.SatuanBesarId, nameof(request.SatuanBesarId));

        var existingBrg = _brgRepo.LoadEntity(new BrgKey(request.BrgId));
        if (existingBrg.HasValue)
        {
            throw new InvalidOperationException($"Barang dengan ID '{request.BrgId}' sudah terdaftar.");
        }

        // LOAD REFERENCES
        var groupRek = _groupRekRepo.LoadEntity(GroupRekType.Key(request.GroupRekId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"GroupRek '{request.GroupRekId}' tidak ditemukan.")
            );

        var pabrik = _pabrikRepo.LoadEntity(PabrikType.Key(request.PabrikId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Pabrik '{request.PabrikId}' tidak ditemukan.")
            );

        var satuanKecil = _satuanRepo.LoadEntity(SatuanType.Key(request.SatuanKecilId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"SatuanKecil '{request.SatuanKecilId}' tidak ditemukan.")
            );

        var satuanBesar = string.Equals(request.SatuanBesarId, request.SatuanKecilId, StringComparison.OrdinalIgnoreCase)
            ? satuanKecil
            : _satuanRepo.LoadEntity(SatuanType.Key(request.SatuanBesarId))
                .Match(
                    onSome: x => x,
                    onNone: () => throw new KeyNotFoundException($"SatuanBesar '{request.SatuanBesarId}' tidak ditemukan.")
                );

        // BUILD DOMAIN ENTITY
        var brg = BarangModel.Create(
            request.BrgId,
            request.BrgName,
            isAktif: true,
            request.KetBarang,
            groupRek.ToReff(),
            groupRek.GroupRekDk,
            pabrik.ToReff(),
            satuanKecil,
            satuanBesar,
            request.Konversi
        );

        // PERSIST
        _brgRepo.SaveChanges(brg);

        return Task.FromResult(new BrgRegisterResponse(brg.BrgId));
    }
}
