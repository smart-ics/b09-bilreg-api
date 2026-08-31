using Bilreg.Application.ApotekContext.SalesOrderFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.SalesOrderFeature;

public record SalesOrderDto(
    string SalesOrderId, int SourceKind, string SourceId, string TelaahResepId, string RegId, string PasienId, string PasienName,
    int PayerPath, int PartialReason, int SalesOrderStatus, int ResolvedReason, int Version,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);

public record SalesOrderItemDto(
    string SalesOrderId, int ItemNo, int SourceItemNo, string BrgId, string BrgName, string SatuanId,
    decimal AcceptedQty, decimal InvoicedQty, decimal DispensedQty, decimal UnfulfilledQty, int ItemStatus,
    int FornasCoverage, string SepNo, bool IsRacik);

public record SalesOrderItemComponentDto(string SalesOrderId, int ItemNo, int ComponentNo, string BrgId, string BrgName, decimal Qty);

public record UnfulfilledOutcomeDto(
    string SalesOrderId, int OutcomeNo, int SalesOrderItemNo, decimal Qty, int Reason, string CopyResepId, string ActorId, DateTime EffectiveAt);

public interface ISalesOrderDal : IInsert<SalesOrderDto>, IUpdate<SalesOrderDto>, IGetData<SalesOrderDto, ISalesOrderKey>
{
    SalesOrderDto? GetActive(int sourceKind, string sourceId, string regId, int payerPath);
    IEnumerable<SalesOrderDto> ListBySource(int sourceKind, string sourceId);
}
public interface ISalesOrderItemDal : IInsertBulk<SalesOrderItemDto>, IDelete<ISalesOrderKey>, IListData<SalesOrderItemDto, ISalesOrderKey>
{
    void UpdateQuantities(SalesOrderItemDto dto);
}
public interface ISalesOrderItemComponentDal : IInsertBulk<SalesOrderItemComponentDto>, IDelete<ISalesOrderKey>, IListData<SalesOrderItemComponentDto, ISalesOrderKey> { }
public interface IUnfulfilledOutcomeDal : IInsertBulk<UnfulfilledOutcomeDto>, IListData<UnfulfilledOutcomeDto, ISalesOrderKey> { }

public class SalesOrderDal : ISalesOrderDal
{
    private readonly DatabaseOptions _opt;
    public SalesOrderDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(SalesOrderDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptSalesOrder (SalesOrderId, SourceKind, SourceId, TelaahResepId, RegId, PasienId, PasienName,
                PayerPath, PartialReason, SalesOrderStatus, ResolvedReason, Version, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@SalesOrderId, @SourceKind, @SourceId, @TelaahResepId, @RegId, @PasienId, @PasienName,
                @PayerPath, @PartialReason, @SalesOrderStatus, @ResolvedReason, @Version, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public void Update(SalesOrderDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptSalesOrder SET SalesOrderStatus=@SalesOrderStatus, ResolvedReason=@ResolvedReason, Version=@Version,
                PartialReason=@PartialReason, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE SalesOrderId=@SalesOrderId
            """, model);
    }
    public SalesOrderDto GetData(ISalesOrderKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<SalesOrderDto>("SELECT * FROM BILRG_AptSalesOrder WHERE SalesOrderId=@SalesOrderId", new { key.SalesOrderId })!;
    }
    public SalesOrderDto? GetActive(int sourceKind, string sourceId, string regId, int payerPath)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<SalesOrderDto>("""
            SELECT * FROM BILRG_AptSalesOrder
            WHERE SourceKind=@sourceKind AND SourceId=@sourceId AND RegId=@regId AND PayerPath=@payerPath
              AND SalesOrderStatus IN (0,1) AND VodDate='3000-01-01'
            """, new { sourceKind, sourceId, regId, payerPath });
    }
    public IEnumerable<SalesOrderDto> ListBySource(int sourceKind, string sourceId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<SalesOrderDto>("SELECT * FROM BILRG_AptSalesOrder WHERE SourceKind=@sourceKind AND SourceId=@sourceId AND VodDate='3000-01-01'", new { sourceKind, sourceId });
    }
}

public class SalesOrderItemDal : ISalesOrderItemDal
{
    private readonly DatabaseOptions _opt;
    public SalesOrderItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<SalesOrderItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptSalesOrderItem (SalesOrderId, ItemNo, SourceItemNo, BrgId, BrgName, SatuanId, AcceptedQty,
                InvoicedQty, DispensedQty, UnfulfilledQty, ItemStatus, FornasCoverage, SepNo, IsRacik)
            VALUES (@SalesOrderId, @ItemNo, @SourceItemNo, @BrgId, @BrgName, @SatuanId, @AcceptedQty,
                @InvoicedQty, @DispensedQty, @UnfulfilledQty, @ItemStatus, @FornasCoverage, @SepNo, @IsRacik)
            """, listModel);
    }
    public void Delete(ISalesOrderKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptSalesOrderItem WHERE SalesOrderId=@SalesOrderId", new { key.SalesOrderId });
    }
    public IEnumerable<SalesOrderItemDto> ListData(ISalesOrderKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<SalesOrderItemDto>("SELECT * FROM BILRG_AptSalesOrderItem WHERE SalesOrderId=@SalesOrderId ORDER BY ItemNo", new { filter.SalesOrderId });
    }
    public void UpdateQuantities(SalesOrderItemDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptSalesOrderItem SET InvoicedQty=@InvoicedQty, DispensedQty=@DispensedQty, UnfulfilledQty=@UnfulfilledQty,
                ItemStatus=@ItemStatus, FornasCoverage=@FornasCoverage, SepNo=@SepNo
            WHERE SalesOrderId=@SalesOrderId AND ItemNo=@ItemNo
            """, dto);
    }
}

public class SalesOrderItemComponentDal : ISalesOrderItemComponentDal
{
    private readonly DatabaseOptions _opt;
    public SalesOrderItemComponentDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<SalesOrderItemComponentDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptSalesOrderItemComponent (SalesOrderId, ItemNo, ComponentNo, BrgId, BrgName, Qty)
            VALUES (@SalesOrderId, @ItemNo, @ComponentNo, @BrgId, @BrgName, @Qty)
            """, listModel);
    }
    public void Delete(ISalesOrderKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptSalesOrderItemComponent WHERE SalesOrderId=@SalesOrderId", new { key.SalesOrderId });
    }
    public IEnumerable<SalesOrderItemComponentDto> ListData(ISalesOrderKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<SalesOrderItemComponentDto>("SELECT * FROM BILRG_AptSalesOrderItemComponent WHERE SalesOrderId=@SalesOrderId", new { filter.SalesOrderId });
    }
}

public class UnfulfilledOutcomeDal : IUnfulfilledOutcomeDal
{
    private readonly DatabaseOptions _opt;
    public UnfulfilledOutcomeDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<UnfulfilledOutcomeDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptUnfulfilledOutcome (SalesOrderId, OutcomeNo, SalesOrderItemNo, Qty, Reason, CopyResepId, ActorId, EffectiveAt)
            VALUES (@SalesOrderId, @OutcomeNo, @SalesOrderItemNo, @Qty, @Reason, @CopyResepId, @ActorId, @EffectiveAt)
            """, listModel);
    }
    public IEnumerable<UnfulfilledOutcomeDto> ListData(ISalesOrderKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<UnfulfilledOutcomeDto>("SELECT * FROM BILRG_AptUnfulfilledOutcome WHERE SalesOrderId=@SalesOrderId ORDER BY OutcomeNo", new { filter.SalesOrderId });
    }
}

public class SalesOrderRepo : ISalesOrderRepo
{
    private readonly ISalesOrderDal _dal;
    private readonly ISalesOrderItemDal _itemDal;
    private readonly ISalesOrderItemComponentDal _componentDal;
    private readonly IUnfulfilledOutcomeDal _outcomeDal;

    public SalesOrderRepo(ISalesOrderDal dal, ISalesOrderItemDal itemDal, ISalesOrderItemComponentDal componentDal, IUnfulfilledOutcomeDal outcomeDal)
    {
        _dal = dal; _itemDal = itemDal; _componentDal = componentDal; _outcomeDal = outcomeDal;
    }

    public void SaveChanges(SalesOrderModel model)
    {
        var dto = ToDto(model);
        var existing = _dal.GetData(model);
        if (existing is null)
        {
            _dal.Insert(dto);
            _itemDal.Insert(model.Items.Select(x => ToItemDto(model.SalesOrderId, x)));
            _componentDal.Insert(model.Components.Select(x => new SalesOrderItemComponentDto(model.SalesOrderId, x.ItemNo, x.ComponentNo, x.BrgId, x.BrgName, x.Qty)));
            _outcomeDal.Insert(model.Outcomes.Select(x => ToOutcomeDto(model.SalesOrderId, x)));
            return;
        }
        _dal.Update(dto);
        foreach (var item in model.Items)
            _itemDal.UpdateQuantities(ToItemDto(model.SalesOrderId, item));
        var persisted = _outcomeDal.ListData(model).Select(x => x.OutcomeNo).ToHashSet();
        _outcomeDal.Insert(model.Outcomes.Where(x => !persisted.Contains(x.OutcomeNo)).Select(x => ToOutcomeDto(model.SalesOrderId, x)));
    }

    public MayBe<SalesOrderModel> LoadEntity(ISalesOrderKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<SalesOrderModel>.None : MayBe.From(ToModel(dto));
    }

    public MayBe<SalesOrderModel> LoadActive(SalesOrderSourceKindEnum sourceKind, string sourceId, string regId, PayerPathEnum payerPath)
    {
        var dto = _dal.GetActive((int)sourceKind, sourceId, regId, (int)payerPath);
        return dto is null ? MayBe<SalesOrderModel>.None : MayBe.From(ToModel(dto));
    }

    public IReadOnlyList<SalesOrderModel> ListBySource(SalesOrderSourceKindEnum sourceKind, string sourceId)
        => _dal.ListBySource((int)sourceKind, sourceId).Select(ToModel).ToList();

    private SalesOrderModel ToModel(SalesOrderDto dto)
    {
        var key = SalesOrderModel.Key(dto.SalesOrderId);
        var items = _itemDal.ListData(key).Select(x => new SalesOrderItemModel(
            x.ItemNo, x.SourceItemNo, x.BrgId, x.BrgName, x.SatuanId, x.AcceptedQty, x.InvoicedQty, x.DispensedQty,
            x.UnfulfilledQty, (SalesOrderItemStatusEnum)x.ItemStatus, (FornasCoverageEnum)x.FornasCoverage, x.SepNo, x.IsRacik));
        var components = _componentDal.ListData(key).Select(x => new SalesOrderItemComponentModel(x.ItemNo, x.ComponentNo, x.BrgId, x.BrgName, x.Qty));
        var outcomes = _outcomeDal.ListData(key).Select(x => new UnfulfilledOutcomeModel(x.OutcomeNo, x.SalesOrderItemNo, x.Qty, (UnfulfilledReasonEnum)x.Reason, x.CopyResepId, x.ActorId, x.EffectiveAt));
        return SalesOrderModel.Rehydrate(dto.SalesOrderId, (SalesOrderSourceKindEnum)dto.SourceKind, dto.SourceId, dto.TelaahResepId,
            dto.RegId, dto.PasienId, dto.PasienName, (PayerPathEnum)dto.PayerPath, (PartialReasonEnum)dto.PartialReason,
            (SalesOrderStatusEnum)dto.SalesOrderStatus, (SalesOrderResolvedReasonEnum)dto.ResolvedReason, dto.Version, items, components, outcomes);
    }

    private static SalesOrderDto ToDto(SalesOrderModel model)
        => new(model.SalesOrderId, (int)model.SourceKind, model.SourceId, model.TelaahResepId, model.RegId, model.PasienId, model.PasienName,
            (int)model.PayerPath, (int)model.PartialReason, (int)model.SalesOrderStatus, (int)model.ResolvedReason, model.Version,
            "", DateTime.Now, "", DateTime.Now, "", new DateTime(3000,1,1));

    private static SalesOrderItemDto ToItemDto(string id, SalesOrderItemModel x)
        => new(id, x.ItemNo, x.SourceItemNo, x.BrgId, x.BrgName, x.SatuanId, x.AcceptedQty, x.InvoicedQty, x.DispensedQty,
            x.UnfulfilledQty, (int)x.ItemStatus, (int)x.FornasCoverage, x.SepNo, x.IsRacik);

    private static UnfulfilledOutcomeDto ToOutcomeDto(string id, UnfulfilledOutcomeModel x)
        => new(id, x.OutcomeNo, x.SalesOrderItemNo, x.Qty, (int)x.Reason, x.CopyResepId, x.ActorId, x.EffectiveAt);
}
