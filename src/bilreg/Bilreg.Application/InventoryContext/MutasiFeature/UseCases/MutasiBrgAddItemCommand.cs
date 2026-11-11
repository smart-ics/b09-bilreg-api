using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.MutasiFeature;
using MediatR;

namespace Bilreg.Application.InventoryContext.MutasiFeature.UseCases;

public record MutasiBrgAddItemCommand(
    string MutasiBrgId,
    string BrgId,
    string BatchNo,
    string ExpiredDate,
    decimal QtyKirim,
    string SatuanId,
    string UserId = ""
) : IRequest, IMutasiBrgKey, IBrgKey, ISatuanKey;

public class MutasiBrgAddItemHandler : IRequestHandler<MutasiBrgAddItemCommand>
{
    private readonly IMutasiBrgRepo _mutasiBrgRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;

    public MutasiBrgAddItemHandler(
        IMutasiBrgRepo mutasiBrgRepo,
        IBrgRepo brgRepo,
        ISatuanRepo satuanRepo)
    {
        _mutasiBrgRepo = mutasiBrgRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
    }

    public Task Handle(MutasiBrgAddItemCommand request, CancellationToken cancellationToken)
    {
        // 1. Guard input
        Guard.Against.Null(request, nameof(request));
        Guard.Against.NullOrWhiteSpace(request.MutasiBrgId, nameof(request.MutasiBrgId));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.SatuanId, nameof(request.SatuanId));

        if (request.QtyKirim <= 0)
            throw new ArgumentException("QtyKirim harus lebih dari 0", nameof(request.QtyKirim));

        // 2. Validate MutasiBrg existence and Draft state
        var mutasiMaybe = _mutasiBrgRepo.LoadEntity(MutasiBrgModel.Key(request.MutasiBrgId));
        if (!mutasiMaybe.HasValue)
            throw new KeyNotFoundException($"MutasiBrg with Id '{request.MutasiBrgId}' not found");

        var mutasi = mutasiMaybe.Value;
        if (mutasi.State != MutasiBrgStateEnum.Draft)
            throw new InvalidOperationException($"MutasiBrg '{request.MutasiBrgId}' is not in Draft state (Current: {mutasi.State})");

        // 3. Validate Barang existence and active status
        var brgMaybe = _brgRepo.LoadEntity(BarangModel.Key(request.BrgId));
        if (!brgMaybe.HasValue)
            throw new KeyNotFoundException($"BrgId '{request.BrgId}' not found");

        var brg = brgMaybe.Value;
        if (!brg.IsAktif)
            throw new InvalidOperationException($"Barang '{brg.BrgName}' ({brg.BrgId}) berstatus Non-Aktif.");

        // 4. Validate Satuan existence
        var satuanMaybe = _satuanRepo.LoadEntity(SatuanType.Key(request.SatuanId));
        if (!satuanMaybe.HasValue)
            throw new KeyNotFoundException($"SatuanId '{request.SatuanId}' not found");

        var satuan = satuanMaybe.Value;

        // 5. Validate pharmacy batch and expiration requirements
        var groupRekDkId = brg.GroupRekDk?.GroupRekDkId ?? string.Empty;
        var isPharmacy = string.Equals(groupRekDkId, "OBT", StringComparison.OrdinalIgnoreCase)
                      || string.Equals(groupRekDkId, "BHP", StringComparison.OrdinalIgnoreCase);

        if (isPharmacy)
        {
            if (string.IsNullOrWhiteSpace(request.BatchNo))
                throw new ArgumentException($"BatchNo wajib diisi untuk barang farmasi ({groupRekDkId})", nameof(request.BatchNo));

            if (string.IsNullOrWhiteSpace(request.ExpiredDate) || !DateTime.TryParse(request.ExpiredDate, out _))
                throw new ArgumentException($"ExpiredDate wajib diisi dengan format tanggal yang valid untuk barang farmasi ({groupRekDkId})", nameof(request.ExpiredDate));
        }

        // 6. Add item and save
        var item = MutasiBrgItemModel.Create(
            brg: brg.ToReff(),
            batchNo: request.BatchNo ?? string.Empty,
            expiredDate: request.ExpiredDate ?? string.Empty,
            qtyKirim: request.QtyKirim,
            satuan: satuan);

        mutasi.AddItem(item);
        _mutasiBrgRepo.SaveChanges(mutasi);

        return Task.CompletedTask;
    }
}
