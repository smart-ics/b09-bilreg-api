using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.BrgContext.PricingPolicyFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.InventoryContext.StockLedgerFeature;
using Bilreg.Domain.SalesContext.PenjualanFeature;
using Bilreg.Domain.SalesContext.ResepFeature;
using MediatR;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.SalesContext.PenjualanFeature.UseCases;

public record PenjualanPoliCreateCmd(
    string RegId,
    string LayananId,
    string BrgId,
    decimal Qty,
    string UserId
) : IRequest<PenjualanPoliCreateResponse>, IRegKey, ILayananKey, IBrgKey;

public record PenjualanPoliCreateResponse(string PenjualanId, string PenjualanItemId, decimal Total);

public class PenjualanPoliCreateHandler : IRequestHandler<PenjualanPoliCreateCmd, PenjualanPoliCreateResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly IBrgPricingService _pricingService;
    private readonly ITglJamProvider _tglJamProvider;
    private readonly IMediator _mediator;
    private readonly IStockBatchRepo _batchRepo;
    private readonly IPenjualanRepo _penjualanRepo;
    private readonly IAddBillAppService _addBillService;
    private readonly IJurnalRepo _jurnalRepo;

    public PenjualanPoliCreateHandler(
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        IJaminanRepo jaminanRepo,
        IBrgRepo brgRepo,
        IGroupRekRepo groupRekRepo,
        IMapJaminanJkRepo mapJaminanJkRepo,
        IBrgPricingService pricingService,
        ITglJamProvider tglJamProvider,
        IMediator mediator,
        IStockBatchRepo batchRepo,
        IPenjualanRepo penjualanRepo,
        IAddBillAppService addBillService,
        IJurnalRepo jurnalRepo)
    {
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _jaminanRepo = jaminanRepo;
        _brgRepo = brgRepo;
        _groupRekRepo = groupRekRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _pricingService = pricingService;
        _tglJamProvider = tglJamProvider;
        _mediator = mediator;
        _batchRepo = batchRepo;
        _penjualanRepo = penjualanRepo;
        _addBillService = addBillService;
        _jurnalRepo = jurnalRepo;
    }

    public async Task<PenjualanPoliCreateResponse> Handle(
        PenjualanPoliCreateCmd request,
        CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.LayananId, nameof(request.LayananId));
        Guard.Against.NullOrWhiteSpace(request.BrgId, nameof(request.BrgId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        if (request.Qty <= 0)
            throw new ArgumentException("Qty harus lebih besar dari 0.", nameof(request.Qty));

        var reg = _regRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register '{request.RegId}' not found")
            );

        if (!reg.IsAktif)
            throw new ArgumentException($"Register '{request.RegId}' tidak aktif");

        var layanan = _layananRepo.LoadEntity(request)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan '{request.LayananId}' not found")
            );

        var jaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(jaminanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Jaminan '{jaminanId}' not found")
            );

        var brg = _brgRepo.LoadEntity(new BrgKey(request.BrgId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang '{request.BrgId}' not found")
            );

        var groupRek = _groupRekRepo.LoadEntity(GroupRekType.Key(brg.GroupRek.GroupRekId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"GroupRek '{brg.GroupRek.GroupRekId}' not found")
            );

        var mapJaminanJk = _mapJaminanJkRepo.LoadEntity(jaminan)
            .Match(
                onSome: x => x,
                onNone: () => MapJaminanJkType.Default
            );

        var pricing = _pricingService.CalculateItemPrice(
            request.BrgId,
            request.Qty,
            reg.TipeJaminan.TipeJaminanId,
            request.LayananId);

        var occurredAt = _tglJamProvider.Now;
        var penjualan = PenjualanModel.CreateDirectReg(
            reg,
            layanan,
            pricing.TipeBrg,
            request.UserId,
            occurredAt);

        penjualan.AddItem(brg, brg.SatuanKecil, request.Qty, EtiketType.Default, pricing.Nilai);

        using var trans = TransHelper.NewScope();

        var issueCmd = new PostSaleIssueConsequenceCommand(
            request.BrgId,
            request.LayananId,
            request.Qty,
            penjualan.PenjualanId,
            occurredAt,
            request.UserId,
            SaleIssueKindEnum.Du);

        var issueResult = await _mediator.Send(issueCmd, cancellationToken);
        if (issueResult.Outcome != PostSaleIssueOutcomeEnum.Success && issueResult.Outcome != PostSaleIssueOutcomeEnum.Idempotent)
        {
            throw new InvalidOperationException(
                $"Pengeluaran stok gagal ({issueResult.Outcome}). Kekurangan: {issueResult.ShortfallQty}. Alasan: {issueResult.InconsistencyReason}");
        }

        var item = penjualan.ListItem.First();
        if (issueResult.Lines != null)
        {
            foreach (var line in issueResult.Lines)
            {
                var batchModel = _batchRepo.LoadByNaturalKey(request.BrgId, line.BrgMasukReffId)
                    .Match(onSome: x => x, onNone: () => (StockBatchModel?)null);

                var noBatch = string.Empty;
                var tglEd = new DateTime(3000, 1, 1);
                var poId = string.Empty;
                var hpp = 0m;

                if (batchModel is not null)
                {
                    var lokasi = batchModel.ListLokasi.FirstOrDefault(l => l.StokLokasiId == line.StokLokasiId)
                        ?? batchModel.ListLokasi.FirstOrDefault();

                    noBatch = lokasi?.NoBatch ?? string.Empty;
                    tglEd = lokasi?.TglEd ?? new DateTime(3000, 1, 1);
                    poId = batchModel.PoReffId ?? string.Empty;
                    hpp = batchModel.Hpp;
                }

                item.AddBatch(noBatch, tglEd, poId, line.BrgMasukReffId, line.QtyOut, hpp);
            }
        }

        _penjualanRepo.SaveChanges(penjualan);

        var bill = _addBillService.FromPenjualanItem(
            penjualan,
            item,
            reg,
            jaminan,
            groupRek,
            occurredAt);

        var jurnal = JurnalType.CreateFromTrsBilling(bill, layanan, mapJaminanJk);
        _jurnalRepo.SaveChanges(jurnal);

        trans.Complete();

        return new PenjualanPoliCreateResponse(penjualan.PenjualanId, item.PenjualanItemId, item.Nilai.Total);
    }
}
