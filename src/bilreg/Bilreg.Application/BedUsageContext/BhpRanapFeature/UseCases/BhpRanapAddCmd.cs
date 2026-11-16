using Ardalis.GuardClauses;
using Bilreg.Application.AccountingContext.JurnalFeature;
using Bilreg.Application.AccountingContext.JurnalFeature.JkAgg;
using Bilreg.Application.AdmisiContext.JaminanFeature.JaminanAgg;
using Bilreg.Application.AdmisiContext.LayananFeature;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.BedUsageContext.PakaiBedFeature;
using Bilreg.Application.BrgContext.BrgFeature;
using Bilreg.Application.InventoryContext.StockLedgerFeature.UseCases;
using Bilreg.Application.PaymentContext.TataRekeningFeature;
using Bilreg.Application.PaymentContext.TrsBillingFeature;
using Bilreg.Domain.AccountingContext.JurnalFeature;
using Bilreg.Domain.AccountingContext.UnitFeature;
using Bilreg.Domain.AdmisiContext.JaminanFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.BhpRanapFeature;
using Bilreg.Domain.BedUsageContext.PakaiBedFeature;
using Bilreg.Domain.BrgContext.BrgFeature;
using Bilreg.Domain.PaymentContext.TataRekeningFeature;
using Bilreg.Domain.PaymentContext.TrsBillFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.BedUsageContext.BhpRanapFeature.UseCases;

public record BhpRanapAddCmd(
    string PakaiBedId,
    string BhpItemId,
    string BhpItemName,
    int Qty,
    decimal Price,
    string UserId,
    string RegId = "",
    string LayananId = "")
    : IRequest<BhpRanapAddResponse>, IPakaiBed;

public record BhpRanapAddResponse(
    string BhpRanapId,
    string PakaiBedId,
    string RegId,
    decimal Subtotal);

public class BhpRanapAddHandler : IRequestHandler<BhpRanapAddCmd, BhpRanapAddResponse>
{
    private readonly IPakaiBedRepo _pakaiBedRepo;
    private readonly IBhpRanapRepo _bhpRanapRepo;
    private readonly IRegRepo _regRepo;
    private readonly ILayananRepo _layananRepo;
    private readonly IJaminanRepo _jaminanRepo;
    private readonly IBrgRepo _brgRepo;
    private readonly IGroupRekRepo _groupRekRepo;
    private readonly IMapJaminanJkRepo _mapJaminanJkRepo;
    private readonly ITataRekeningRepo _tataRekeningRepo;
    private readonly IAddBillAppService _addBillService;
    private readonly IJurnalRepo _jurnalRepo;
    private readonly IMediator _mediator;
    private readonly ITglJamProvider _tglJamProvider;

    public BhpRanapAddHandler(
        IPakaiBedRepo pakaiBedRepo,
        IBhpRanapRepo bhpRanapRepo,
        IRegRepo regRepo,
        ILayananRepo layananRepo,
        IJaminanRepo jaminanRepo,
        IBrgRepo brgRepo,
        IGroupRekRepo groupRekRepo,
        IMapJaminanJkRepo mapJaminanJkRepo,
        ITataRekeningRepo tataRekeningRepo,
        IAddBillAppService addBillService,
        IJurnalRepo jurnalRepo,
        IMediator mediator,
        ITglJamProvider tglJamProvider)
    {
        _pakaiBedRepo = pakaiBedRepo;
        _bhpRanapRepo = bhpRanapRepo;
        _regRepo = regRepo;
        _layananRepo = layananRepo;
        _jaminanRepo = jaminanRepo;
        _brgRepo = brgRepo;
        _groupRekRepo = groupRekRepo;
        _mapJaminanJkRepo = mapJaminanJkRepo;
        _tataRekeningRepo = tataRekeningRepo;
        _addBillService = addBillService;
        _jurnalRepo = jurnalRepo;
        _mediator = mediator;
        _tglJamProvider = tglJamProvider;
    }

    public async Task<BhpRanapAddResponse> Handle(BhpRanapAddCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.PakaiBedId, nameof(request.PakaiBedId));
        Guard.Against.NullOrWhiteSpace(request.BhpItemId, nameof(request.BhpItemId));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));
        Guard.Against.NegativeOrZero(request.Qty, nameof(request.Qty));
        Guard.Against.Negative(request.Price, nameof(request.Price));

        string resolvedRegId = request.RegId;
        string resolvedLayananId = request.LayananId;

        // Try load occupancy details from PakaiBedRepo if not provided
        var maybeBed = _pakaiBedRepo.LoadEntity(PakaiBedModel.Key(request.PakaiBedId));
        if (maybeBed.HasValue)
        {
            var bed = maybeBed.Value;
            if (string.IsNullOrWhiteSpace(resolvedRegId))
                resolvedRegId = bed.Reg?.RegId ?? string.Empty;
            if (string.IsNullOrWhiteSpace(resolvedLayananId))
                resolvedLayananId = bed.Layanan?.LayananId ?? string.Empty;
        }

        if (string.IsNullOrWhiteSpace(resolvedRegId))
        {
            throw new InvalidOperationException($"RegId tidak ditemukan untuk PakaiBed '{request.PakaiBedId}'.");
        }

        if (string.IsNullOrWhiteSpace(resolvedLayananId))
        {
            throw new InvalidOperationException($"LayananId tidak ditemukan untuk PakaiBed '{request.PakaiBedId}'.");
        }

        var reg = _regRepo.LoadEntity(RegModel.Key(resolvedRegId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register '{resolvedRegId}' not found")
            );

        if (!reg.IsAktif)
            throw new ArgumentException($"Register '{resolvedRegId}' tidak aktif");

        var layanan = _layananRepo.LoadEntity(LayananType.Key(resolvedLayananId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Layanan '{resolvedLayananId}' not found")
            );

        var brg = _brgRepo.LoadEntity(new BrgKey(request.BhpItemId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Barang '{request.BhpItemId}' not found")
            );

        if (!brg.IsAktif)
            throw new ArgumentException($"Barang '{request.BhpItemId}' tidak aktif");

        var jaminanId = reg.TipeJaminan.TipeJaminanId.Length >= 3
            ? reg.TipeJaminan.TipeJaminanId[..3]
            : reg.TipeJaminan.TipeJaminanId;
        var jaminan = _jaminanRepo.LoadEntity(JaminanType.Key(jaminanId))
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Jaminan '{jaminanId}' not found")
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

        var tataRekening = _tataRekeningRepo.LoadEntity(reg)
            .Match(
                onSome: x => x,
                onNone: () => (TataRekeningModel?)null
            );
        tataRekening?.EnsureCanCreateTrsBill();

        var occurredAt = _tglJamProvider.Now;
        var audit = new AuditInfoType(request.UserId, occurredAt);
        var bhp = BhpRanapModel.Create(
            pakaiBedId: request.PakaiBedId,
            regId: resolvedRegId,
            layananId: resolvedLayananId,
            bhpItemId: request.BhpItemId,
            bhpItemName: request.BhpItemName,
            qty: request.Qty,
            price: request.Price,
            audit: audit);

        using var trans = TransHelper.NewScope();

        _bhpRanapRepo.SaveChanges(bhp);

        var issueCmd = new PostSaleIssueConsequenceCommand(
            request.BhpItemId,
            resolvedLayananId,
            request.Qty,
            bhp.BhpRanapId,
            occurredAt,
            request.UserId,
            SaleIssueKindEnum.Du);

        var issueResult = await _mediator.Send(issueCmd, cancellationToken);
        if (issueResult.Outcome != PostSaleIssueOutcomeEnum.Success && issueResult.Outcome != PostSaleIssueOutcomeEnum.Idempotent)
        {
            throw new InvalidOperationException(
                $"Pengeluaran stok gagal ({issueResult.Outcome}). Kekurangan: {issueResult.ShortfallQty}. Alasan: {issueResult.InconsistencyReason}");
        }

        var bill = _addBillService.FromBhpRanap(
            bhp,
            reg,
            layanan,
            jaminan,
            groupRek,
            occurredAt);

        var jurnal = JurnalType.CreateFromTrsBilling(bill, layanan, mapJaminanJk);
        _jurnalRepo.SaveChanges(jurnal);

        trans.Complete();

        return new BhpRanapAddResponse(bhp.BhpRanapId, bhp.PakaiBedId, bhp.RegId, bhp.Subtotal);
    }
}
