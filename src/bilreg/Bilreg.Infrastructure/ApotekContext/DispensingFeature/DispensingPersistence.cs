using Bilreg.Application.ApotekContext.DispensingFeature;
using Bilreg.Domain.ApotekContext.DispensingFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.DispensingFeature;

public record DispensingDto(
    string DispensingId, string SalesOrderId, int CareSetting, int DispensingStatus, string PharmacyUnitLayananId, string TemporaryUnitLayananId,
    DateTime ReleasedAt, DateTime PreparationStartedAt, DateTime PreparedAt, DateTime EducationAt, string EducationPharmacistId, string EducationNote,
    DateTime OverrideAt, string OverridePharmacistId, string OverrideReason, DateTime HandoverAt, string RecipientPhone, string RecipientRelationship,
    DateTime CancelledAt, DateTime ExpiredAt, string CancelReason, DateTime PickupCalledAt, int Version,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);
public record DispensingItemDto(string DispensingId, int ItemNo, int SalesOrderItemNo, string BrgId, decimal Qty, string ReserveMutasiReff, string RemoveStockMutasiReff, string ReturnMutasiReff, int ItemOutcome);
public record FinalReviewDto(string DispensingId, int ReviewNo, int Outcome, string Reason, string PharmacistId, DateTime EffectiveAt, decimal AffectedQty);

public interface IDispensingDal : IInsert<DispensingDto>, IUpdate<DispensingDto>, IGetData<DispensingDto, IDispensingKey>
{
    IEnumerable<DispensingDto> ListBySalesOrder(string salesOrderId);
}
public interface IDispensingItemDal : IInsertBulk<DispensingItemDto>, IDelete<IDispensingKey>, IListData<DispensingItemDto, IDispensingKey>
{
    void UpdateCorrelations(DispensingItemDto dto);
}
public interface IFinalReviewDal : IInsertBulk<FinalReviewDto>, IListData<FinalReviewDto, IDispensingKey> { }

public class DispensingDal : IDispensingDal
{
    private readonly DatabaseOptions _opt;
    public DispensingDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(DispensingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptDispensing (DispensingId, SalesOrderId, CareSetting, DispensingStatus, PharmacyUnitLayananId, TemporaryUnitLayananId,
                ReleasedAt, PreparationStartedAt, PreparedAt, EducationAt, EducationPharmacistId, EducationNote, OverrideAt, OverridePharmacistId,
                OverrideReason, HandoverAt, RecipientPhone, RecipientRelationship, CancelledAt, ExpiredAt, CancelReason, PickupCalledAt, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@DispensingId, @SalesOrderId, @CareSetting, @DispensingStatus, @PharmacyUnitLayananId, @TemporaryUnitLayananId,
                @ReleasedAt, @PreparationStartedAt, @PreparedAt, @EducationAt, @EducationPharmacistId, @EducationNote, @OverrideAt, @OverridePharmacistId,
                @OverrideReason, @HandoverAt, @RecipientPhone, @RecipientRelationship, @CancelledAt, @ExpiredAt, @CancelReason, @PickupCalledAt, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public void Update(DispensingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptDispensing SET DispensingStatus=@DispensingStatus, ReleasedAt=@ReleasedAt, PreparationStartedAt=@PreparationStartedAt,
                PreparedAt=@PreparedAt, EducationAt=@EducationAt, EducationPharmacistId=@EducationPharmacistId, EducationNote=@EducationNote,
                OverrideAt=@OverrideAt, OverridePharmacistId=@OverridePharmacistId, OverrideReason=@OverrideReason, HandoverAt=@HandoverAt,
                RecipientPhone=@RecipientPhone, RecipientRelationship=@RecipientRelationship, ExpiredAt=@ExpiredAt, CancelReason=@CancelReason,
                PickupCalledAt=@PickupCalledAt, Version=@Version, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE DispensingId=@DispensingId
            """, model);
    }
    public DispensingDto GetData(IDispensingKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<DispensingDto>("SELECT * FROM BILRG_AptDispensing WHERE DispensingId=@DispensingId", new { key.DispensingId })!;
    }
    public IEnumerable<DispensingDto> ListBySalesOrder(string salesOrderId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<DispensingDto>("SELECT * FROM BILRG_AptDispensing WHERE SalesOrderId=@salesOrderId AND VodDate='3000-01-01'", new { salesOrderId });
    }
}

public class DispensingItemDal : IDispensingItemDal
{
    private readonly DatabaseOptions _opt;
    public DispensingItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<DispensingItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptDispensingItem (DispensingId, ItemNo, SalesOrderItemNo, BrgId, Qty, ReserveMutasiReff, RemoveStockMutasiReff, ReturnMutasiReff, ItemOutcome)
            VALUES (@DispensingId, @ItemNo, @SalesOrderItemNo, @BrgId, @Qty, @ReserveMutasiReff, @RemoveStockMutasiReff, @ReturnMutasiReff, @ItemOutcome)
            """, listModel);
    }
    public void Delete(IDispensingKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptDispensingItem WHERE DispensingId=@DispensingId", new { key.DispensingId });
    }
    public IEnumerable<DispensingItemDto> ListData(IDispensingKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<DispensingItemDto>("SELECT * FROM BILRG_AptDispensingItem WHERE DispensingId=@DispensingId ORDER BY ItemNo", new { filter.DispensingId });
    }
    public void UpdateCorrelations(DispensingItemDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptDispensingItem SET ReserveMutasiReff=@ReserveMutasiReff, RemoveStockMutasiReff=@RemoveStockMutasiReff,
                ReturnMutasiReff=@ReturnMutasiReff, ItemOutcome=@ItemOutcome
            WHERE DispensingId=@DispensingId AND ItemNo=@ItemNo
            """, dto);
    }
}

public class FinalReviewDal : IFinalReviewDal
{
    private readonly DatabaseOptions _opt;
    public FinalReviewDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<FinalReviewDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptFinalReview (DispensingId, ReviewNo, Outcome, Reason, PharmacistId, EffectiveAt, AffectedQty)
            VALUES (@DispensingId, @ReviewNo, @Outcome, @Reason, @PharmacistId, @EffectiveAt, @AffectedQty)
            """, listModel);
    }
    public IEnumerable<FinalReviewDto> ListData(IDispensingKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<FinalReviewDto>("SELECT * FROM BILRG_AptFinalReview WHERE DispensingId=@DispensingId ORDER BY ReviewNo", new { filter.DispensingId });
    }
}

public class DispensingRepo : IDispensingRepo
{
    private readonly IDispensingDal _dal;
    private readonly IDispensingItemDal _itemDal;
    private readonly IFinalReviewDal _reviewDal;
    public DispensingRepo(IDispensingDal dal, IDispensingItemDal itemDal, IFinalReviewDal reviewDal)
    { _dal = dal; _itemDal = itemDal; _reviewDal = reviewDal; }

    public void SaveChanges(DispensingModel model)
    {
        var dto = ToDto(model);
        var existing = _dal.GetData(model);
        if (existing is null)
        {
            _dal.Insert(dto);
            _itemDal.Insert(model.Items.Select(x => ToItem(model.DispensingId, x)));
            _reviewDal.Insert(model.Reviews.Select(x => ToReview(model.DispensingId, x)));
            return;
        }
        _dal.Update(dto);
        foreach (var item in model.Items)
            _itemDal.UpdateCorrelations(ToItem(model.DispensingId, item));
        var persisted = _reviewDal.ListData(model).Select(x => x.ReviewNo).ToHashSet();
        _reviewDal.Insert(model.Reviews.Where(x => !persisted.Contains(x.ReviewNo)).Select(x => ToReview(model.DispensingId, x)));
    }

    public MayBe<DispensingModel> LoadEntity(IDispensingKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<DispensingModel>.None : MayBe.From(ToModel(dto));
    }

    public IReadOnlyList<DispensingModel> ListBySalesOrder(string salesOrderId)
        => _dal.ListBySalesOrder(salesOrderId).Select(ToModel).ToList();

    private DispensingModel ToModel(DispensingDto dto)
    {
        var key = DispensingModel.Key(dto.DispensingId);
        var items = _itemDal.ListData(key).Select(x => new DispensingItemModel(x.ItemNo, x.SalesOrderItemNo, x.BrgId, x.Qty, x.ReserveMutasiReff, x.RemoveStockMutasiReff, x.ReturnMutasiReff, (DispensingItemOutcomeEnum)x.ItemOutcome));
        var reviews = _reviewDal.ListData(key).Select(x => new FinalReviewModel(x.ReviewNo, (FinalReviewOutcomeEnum)x.Outcome, x.Reason, x.PharmacistId, x.EffectiveAt, x.AffectedQty));
        return DispensingModel.Rehydrate(dto.DispensingId, dto.SalesOrderId, dto.CareSetting, (DispensingStatusEnum)dto.DispensingStatus,
            dto.PharmacyUnitLayananId, dto.TemporaryUnitLayananId, dto.ReleasedAt, dto.PreparationStartedAt, dto.PreparedAt, dto.EducationAt,
            dto.EducationPharmacistId, dto.EducationNote, dto.OverrideAt, dto.OverridePharmacistId, dto.OverrideReason, dto.HandoverAt,
            dto.RecipientPhone, dto.RecipientRelationship, dto.CancelledAt, dto.ExpiredAt, dto.CancelReason, dto.PickupCalledAt, dto.Version,
            items, reviews);
    }

    private static DispensingDto ToDto(DispensingModel m)
        => new(m.DispensingId, m.SalesOrderId, m.CareSetting, (int)m.DispensingStatus, m.PharmacyUnitLayananId, m.TemporaryUnitLayananId,
            m.ReleasedAt, m.PreparationStartedAt, m.PreparedAt, m.EducationAt, m.EducationPharmacistId, m.EducationNote, m.OverrideAt,
            m.OverridePharmacistId, m.OverrideReason, m.HandoverAt, m.RecipientPhone, m.RecipientRelationship, m.CancelledAt, m.ExpiredAt,
            m.CancelReason, m.PickupCalledAt, m.Version, "", DateTime.Now, "", DateTime.Now, "", new DateTime(3000,1,1));

    private static DispensingItemDto ToItem(string id, DispensingItemModel x)
        => new(id, x.ItemNo, x.SalesOrderItemNo, x.BrgId, x.Qty, x.ReserveMutasiReff, x.RemoveStockMutasiReff, x.ReturnMutasiReff, (int)x.ItemOutcome);

    private static FinalReviewDto ToReview(string id, FinalReviewModel x)
        => new(id, x.ReviewNo, (int)x.Outcome, x.Reason, x.PharmacistId, x.EffectiveAt, x.AffectedQty);
}
