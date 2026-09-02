using Ardalis.GuardClauses;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PurchaseContext.PurchaseOrderFeature;
using MediatR;
using Nuna.Lib.DataTypeExtension;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.PurchaseContext.PurchaseOrderFeature.UseCases;

public record PurchaseOrderCreateCommand(
    string PartnerId,
    string UserId,
    string Keterangan,
    IEnumerable<PurchaseOrderCreateItemCommand> ListItem
) : IRequest<PurchaseOrderCreateResponse>, IPartnerKey;

public record PurchaseOrderCreateItemCommand(
    string BrgId,
    string SatuanId,
    decimal Harga,
    decimal Qty,
    decimal QtyStok,
    decimal DiskonPercentage,
    decimal TaxPercentage,
    decimal BiayaLain
);

public record PurchaseOrderCreateResponse(string PurchaseOrderId);

public class PurchaseOrderCreateHandler : IRequestHandler<PurchaseOrderCreateCommand, PurchaseOrderCreateResponse>
{
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IPartnerRepo _partnerRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly ISatuanRepo _satuanRepo;
    private readonly IPurchaseOrderRepo _purchaseOrderRepo;

    public PurchaseOrderCreateHandler(ITglJamProvider tglJamProvider, IPartnerRepo partnerRepo, IBrgRepo brgRepo,
        ISatuanRepo satuanRepo, IPurchaseOrderRepo purchaseOrderRepo)
    {
        _tglJamProvider = tglJamProvider;
        _partnerRepo = partnerRepo;
        _brgRepo = brgRepo;
        _satuanRepo = satuanRepo;
        _purchaseOrderRepo = purchaseOrderRepo;
    }

    public Task<PurchaseOrderCreateResponse> Handle(PurchaseOrderCreateCommand request, CancellationToken cancellationToken)
    {
        // GUARD
        Guard.Against.NullOrWhiteSpace(request.PartnerId);
        Guard.Against.NullOrWhiteSpace(request.UserId);
        if (request.ListItem.IsNullOrEmpty())
            throw new ArgumentException("List Item tidak boleh kosong");

        // BUILD
        var partner = _partnerRepo.LoadEntity(request)
            .GetValueOrThrow($"Partner with Id: {request.PartnerId} not found.");

        var purchaseOrder = PurchaseOrderModel.Create(partner, request.Keterangan, request.UserId, _tglJamProvider.Now);
        purchaseOrder = BuildItemPurchaseOrder(purchaseOrder, request.ListItem);

        // RESPONSE
        _purchaseOrderRepo.SaveChanges(purchaseOrder);
        return Task.FromResult(new PurchaseOrderCreateResponse(purchaseOrder.PurchaseOrderId));
    }

    private IBrg LoadBrg(IBrgKey brgKey)
    {
        var brg = _brgRepo.LoadEntity(brgKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang {brgKey.BrgId} not found")
            );

        return brg;
    }

    private SatuanType LoadSatuan(ISatuanKey satuanKey)
    {
        var satuan = _satuanRepo.LoadEntity(satuanKey)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Satuan Barang {satuanKey.SatuanId} not found")
            );

        return satuan;
    }

    private PurchaseOrderModel BuildItemPurchaseOrder(PurchaseOrderModel purchaseOrder,
        IEnumerable<PurchaseOrderCreateItemCommand> listItem)
    {
        foreach (var item in listItem)
        {
            var brgKey = BrgObatType.Key(item.BrgId);
            var brg = LoadBrg(brgKey);

            var satuanKey = SatuanType.Key(item.SatuanId);
            var satuan = LoadSatuan(satuanKey);

            purchaseOrder.AddItem(brg, satuan, item.Harga, item.Qty, item.QtyStok, item.DiskonPercentage, item.TaxPercentage,
                item.BiayaLain);
        }

        return purchaseOrder;
    }
}