using Ardalis.GuardClauses;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Application.Shared.AuditLogFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
using Bilreg.Domain.Shared.AuditLogFeature;
using MediatR;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;

namespace Bilreg.Application.ChargeContext.TindakanFeature.UseCases;

//  M03-F01 P2-S04 — creates a follow-up order as Proposed (TD-03).
//  Own transaction (TD-08): never joins the clinical-action transaction.
//  Resolves the accountable receiving service from mapping data
//  (TujuanLanjutService, TD-04) — no per-order-type branching.
public record TdkCreateTindakanLanjutCmd(string RegId, string OrderType,
    string UserId, IEnumerable<TdkCreateTindakanLanjutItemCmd> ListItem)
    : IRequest<TdkCreateTindakanLanjutResponse>, IRegKey;

public record TdkCreateTindakanLanjutItemCmd(
    int ItemNo, string ItemCode, string ItemName, decimal Qty, string Note = "");

public record TdkCreateTindakanLanjutResponse(string TindakanLanjutId);

public class TdkCreateTindakanLanjutHandler
    : IRequestHandler<TdkCreateTindakanLanjutCmd, TdkCreateTindakanLanjutResponse>
{
    private readonly IRegRepo _regRepo;
    private readonly ITujuanLanjutRepo _tujuanLanjutRepo;
    private readonly ITindakanLanjutRepo _tindakanLanjutRepo;
    private readonly IAuditRepo _auditRepo;
    private readonly ITglJamProvider _tglJamProvider;

    public TdkCreateTindakanLanjutHandler(IRegRepo regRepo,
        ITujuanLanjutRepo tujuanLanjutRepo,
        ITindakanLanjutRepo tindakanLanjutRepo,
        IAuditRepo auditRepo,
        ITglJamProvider tglJamProvider)
    {
        _regRepo = regRepo;
        _tujuanLanjutRepo = tujuanLanjutRepo;
        _tindakanLanjutRepo = tindakanLanjutRepo;
        _auditRepo = auditRepo;
        _tglJamProvider = tglJamProvider;
    }

    public Task<TdkCreateTindakanLanjutResponse> Handle(
        TdkCreateTindakanLanjutCmd request, CancellationToken cancellationToken)
    {
        Guard.Against.NullOrWhiteSpace(request.RegId, nameof(request.RegId));
        Guard.Against.NullOrWhiteSpace(request.OrderType, nameof(request.OrderType));
        Guard.Against.NullOrWhiteSpace(request.UserId, nameof(request.UserId));

        var reg = LoadReg(request);
        var listTujuan = _tujuanLanjutRepo.ListData()?.ToList() ?? [];
        var tujuanService = new TujuanLanjutService(listTujuan);
        var tujuan = tujuanService.Resolve(request.OrderType);

        var listItem = (request.ListItem ?? [])
            .Select(x => TindakanLanjutItemModel.Create(
                x.ItemNo, x.ItemCode, x.ItemName, x.Qty, x.Note ?? string.Empty))
            .ToList();

        var occurredAt = _tglJamProvider.Now;
        var order = TindakanLanjutModel.Create(reg, tujuan, listItem, request.UserId, occurredAt);

        using var trans = TransHelper.NewScope();
        _tindakanLanjutRepo.SaveChanges(order);
        _auditRepo.SaveChanges(CreateAudit(order, request.UserId, occurredAt));
        trans.Complete();

        return Task.FromResult(new TdkCreateTindakanLanjutResponse(order.TindakanLanjutId));
    }

    private RegModel LoadReg(IRegKey key)
    {
        var result = _regRepo.LoadEntity(key)
            .Match(
                onSome: x => x,
                onNone: () => throw new KeyNotFoundException($"Register {key.RegId} not found")
            );
        return result;
    }

    private static AuditLog CreateAudit(TindakanLanjutModel order, string userId, DateTime occurredAt)
    {
        return AuditLog.Create(
            userId,
            occurredAt,
            actionType: "CREATE",
            entityName: nameof(TindakanLanjutModel),
            entityId: order.TindakanLanjutId,
            correlationId: order.Reg.RegId);
    }
}
