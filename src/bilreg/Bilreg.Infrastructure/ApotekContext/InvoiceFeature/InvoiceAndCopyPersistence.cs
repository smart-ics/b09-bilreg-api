using Bilreg.Application.ApotekContext.CopyResepFeature;
using Bilreg.Application.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.CopyResepFeature;
using Bilreg.Domain.ApotekContext.InvoiceFeature;
using Bilreg.Domain.ApotekContext.SalesOrderFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.InvoiceFeature;

public record CopyResepDto(string CopyResepId, string ResepKerjaId, string SalesOrderId, int Reason, string IssuedBy, DateTime IssuedAt,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);
public record CopyResepItemDto(string CopyResepId, int ItemNo, int ResepKerjaItemNo, string BrgId, decimal Qty, string Note);
public interface ICopyResepDal : IInsert<CopyResepDto>, IGetData<CopyResepDto, ICopyResepKey> { }
public interface ICopyResepItemDal : IInsertBulk<CopyResepItemDto>, IListData<CopyResepItemDto, ICopyResepKey> { }

public class CopyResepDal : ICopyResepDal
{
    private readonly DatabaseOptions _opt;
    public CopyResepDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(CopyResepDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptCopyResep (CopyResepId, ResepKerjaId, SalesOrderId, Reason, IssuedBy, IssuedAt, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@CopyResepId, @ResepKerjaId, @SalesOrderId, @Reason, @IssuedBy, @IssuedAt, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public CopyResepDto GetData(ICopyResepKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<CopyResepDto>("SELECT * FROM BILRG_AptCopyResep WHERE CopyResepId=@CopyResepId", new { key.CopyResepId })!;
    }
}

public class CopyResepItemDal : ICopyResepItemDal
{
    private readonly DatabaseOptions _opt;
    public CopyResepItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<CopyResepItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("INSERT INTO BILRG_AptCopyResepItem (CopyResepId, ItemNo, ResepKerjaItemNo, BrgId, Qty, Note) VALUES (@CopyResepId, @ItemNo, @ResepKerjaItemNo, @BrgId, @Qty, @Note)", listModel);
    }
    public IEnumerable<CopyResepItemDto> ListData(ICopyResepKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<CopyResepItemDto>("SELECT * FROM BILRG_AptCopyResepItem WHERE CopyResepId=@CopyResepId", new { filter.CopyResepId });
    }
}

public class CopyResepRepo : ICopyResepRepo
{
    private readonly ICopyResepDal _dal;
    private readonly ICopyResepItemDal _itemDal;
    public CopyResepRepo(ICopyResepDal dal, ICopyResepItemDal itemDal) { _dal = dal; _itemDal = itemDal; }
    public void SaveChanges(CopyResepModel model)
    {
        if (_dal.GetData(model) is not null) return;
        _dal.Insert(new CopyResepDto(model.CopyResepId, model.ResepKerjaId, model.SalesOrderId, model.Reason, model.IssuedBy, model.IssuedAt,
            model.IssuedBy, model.IssuedAt, "", new DateTime(3000,1,1), "", new DateTime(3000,1,1)));
        _itemDal.Insert(model.Items.Select(x => new CopyResepItemDto(model.CopyResepId, x.ItemNo, x.ResepKerjaItemNo, x.BrgId, x.Qty, x.Note)));
    }
    public MayBe<CopyResepModel> LoadEntity(ICopyResepKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null) return MayBe<CopyResepModel>.None;
        var items = _itemDal.ListData(key).Select(x => new CopyResepItemModel(x.ItemNo, x.ResepKerjaItemNo, x.BrgId, x.Qty, x.Note));
        return MayBe.From(CopyResepModel.Rehydrate(dto.CopyResepId, dto.ResepKerjaId, dto.SalesOrderId, dto.Reason, dto.IssuedBy, dto.IssuedAt, items));
    }
}

public record InvoiceDto(
    string InvoiceId, string SalesOrderId, int PayerPath, int InvoiceStatus, DateTime PricingSnapshotAt, string TipeJaminanId, string TipeJaminanName,
    decimal SubTotal, decimal SumBiaya, decimal SumTax, decimal DiskonLain, decimal BiayaLain, decimal Pembulatan, decimal GrandTotal,
    string PaymentClearanceReff, DateTime PaymentClearedAt, string TataRekeningChargeId, string TataRekeningCorrectionReff,
    DateTime EstablishedAt, DateTime IssuedAt, DateTime ClearedAt, int Version,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);
public record InvoiceItemDto(string InvoiceId, int ItemNo, int SalesOrderItemNo, string BrgId, string BrgName, int ItemKind, decimal Qty, decimal HargaSatuan, decimal Diskon, decimal Biaya, decimal Tax, decimal Total);
public record InvoiceChargeDto(string InvoiceId, int ItemNo, int ChargeNo, string ChargeName, decimal Amount);

public interface IInvoiceDal : IInsert<InvoiceDto>, IUpdate<InvoiceDto>, IGetData<InvoiceDto, IInvoiceKey>
{
    IEnumerable<InvoiceDto> ListBySalesOrder(string salesOrderId);
}
public interface IInvoiceItemDal : IInsertBulk<InvoiceItemDto>, IDelete<IInvoiceKey>, IListData<InvoiceItemDto, IInvoiceKey> { }
public interface IInvoiceChargeDal : IInsertBulk<InvoiceChargeDto>, IDelete<IInvoiceKey>, IListData<InvoiceChargeDto, IInvoiceKey> { }

public class InvoiceDal : IInvoiceDal
{
    private readonly DatabaseOptions _opt;
    public InvoiceDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(InvoiceDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptInvoice (InvoiceId, SalesOrderId, PayerPath, InvoiceStatus, PricingSnapshotAt, TipeJaminanId, TipeJaminanName,
                SubTotal, SumBiaya, SumTax, DiskonLain, BiayaLain, Pembulatan, GrandTotal, PaymentClearanceReff, PaymentClearedAt,
                TataRekeningChargeId, TataRekeningCorrectionReff, EstablishedAt, IssuedAt, ClearedAt, Version, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@InvoiceId, @SalesOrderId, @PayerPath, @InvoiceStatus, @PricingSnapshotAt, @TipeJaminanId, @TipeJaminanName,
                @SubTotal, @SumBiaya, @SumTax, @DiskonLain, @BiayaLain, @Pembulatan, @GrandTotal, @PaymentClearanceReff, @PaymentClearedAt,
                @TataRekeningChargeId, @TataRekeningCorrectionReff, @EstablishedAt, @IssuedAt, @ClearedAt, @Version, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public void Update(InvoiceDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptInvoice SET InvoiceStatus=@InvoiceStatus, SubTotal=@SubTotal, SumBiaya=@SumBiaya, SumTax=@SumTax,
                DiskonLain=@DiskonLain, BiayaLain=@BiayaLain, Pembulatan=@Pembulatan, GrandTotal=@GrandTotal,
                PaymentClearanceReff=@PaymentClearanceReff, PaymentClearedAt=@PaymentClearedAt, TataRekeningChargeId=@TataRekeningChargeId,
                TataRekeningCorrectionReff=@TataRekeningCorrectionReff, IssuedAt=@IssuedAt, ClearedAt=@ClearedAt, Version=@Version,
                UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE InvoiceId=@InvoiceId
            """, model);
    }
    public InvoiceDto GetData(IInvoiceKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<InvoiceDto>("SELECT * FROM BILRG_AptInvoice WHERE InvoiceId=@InvoiceId", new { key.InvoiceId })!;
    }
    public IEnumerable<InvoiceDto> ListBySalesOrder(string salesOrderId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<InvoiceDto>("SELECT * FROM BILRG_AptInvoice WHERE SalesOrderId=@salesOrderId AND VodDate='3000-01-01'", new { salesOrderId });
    }
}

public class InvoiceItemDal : IInvoiceItemDal
{
    private readonly DatabaseOptions _opt;
    public InvoiceItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<InvoiceItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptInvoiceItem (InvoiceId, ItemNo, SalesOrderItemNo, BrgId, BrgName, ItemKind, Qty, HargaSatuan, Diskon, Biaya, Tax, Total)
            VALUES (@InvoiceId, @ItemNo, @SalesOrderItemNo, @BrgId, @BrgName, @ItemKind, @Qty, @HargaSatuan, @Diskon, @Biaya, @Tax, @Total)
            """, listModel);
    }
    public void Delete(IInvoiceKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptInvoiceItem WHERE InvoiceId=@InvoiceId", new { key.InvoiceId });
    }
    public IEnumerable<InvoiceItemDto> ListData(IInvoiceKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<InvoiceItemDto>("SELECT * FROM BILRG_AptInvoiceItem WHERE InvoiceId=@InvoiceId ORDER BY ItemNo", new { filter.InvoiceId });
    }
}

public class InvoiceChargeDal : IInvoiceChargeDal
{
    private readonly DatabaseOptions _opt;
    public InvoiceChargeDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<InvoiceChargeDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("INSERT INTO BILRG_AptInvoiceItemCharge (InvoiceId, ItemNo, ChargeNo, ChargeName, Amount) VALUES (@InvoiceId, @ItemNo, @ChargeNo, @ChargeName, @Amount)", listModel);
    }
    public void Delete(IInvoiceKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptInvoiceItemCharge WHERE InvoiceId=@InvoiceId", new { key.InvoiceId });
    }
    public IEnumerable<InvoiceChargeDto> ListData(IInvoiceKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<InvoiceChargeDto>("SELECT * FROM BILRG_AptInvoiceItemCharge WHERE InvoiceId=@InvoiceId", new { filter.InvoiceId });
    }
}

public class InvoiceRepo : IInvoiceRepo
{
    private readonly IInvoiceDal _dal;
    private readonly IInvoiceItemDal _itemDal;
    private readonly IInvoiceChargeDal _chargeDal;
    public InvoiceRepo(IInvoiceDal dal, IInvoiceItemDal itemDal, IInvoiceChargeDal chargeDal)
    { _dal = dal; _itemDal = itemDal; _chargeDal = chargeDal; }

    public void SaveChanges(InvoiceModel model)
    {
        var dto = ToDto(model);
        var existing = _dal.GetData(model);
        if (existing is null) _dal.Insert(dto); else _dal.Update(dto);
        _itemDal.Delete(model);
        _itemDal.Insert(model.Items.Select(x => new InvoiceItemDto(model.InvoiceId, x.ItemNo, x.SalesOrderItemNo, x.BrgId, x.BrgName, (int)x.ItemKind, x.Qty, x.HargaSatuan, x.Diskon, x.Biaya, x.Tax, x.Total)));
        _chargeDal.Delete(model);
        _chargeDal.Insert(model.Charges.Select(x => new InvoiceChargeDto(model.InvoiceId, x.ItemNo, x.ChargeNo, x.ChargeName, x.Amount)));
    }

    public MayBe<InvoiceModel> LoadEntity(IInvoiceKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<InvoiceModel>.None : MayBe.From(ToModel(dto));
    }

    public IReadOnlyList<InvoiceModel> ListBySalesOrder(string salesOrderId)
        => _dal.ListBySalesOrder(salesOrderId).Select(ToModel).ToList();

    public MayBe<InvoiceModel> LoadActiveBySalesOrder(string salesOrderId)
    {
        var list = ListBySalesOrder(salesOrderId);
        var active = list.LastOrDefault(x => x.InvoiceStatus != InvoiceStatusEnum.Cancelled);
        return active is null ? MayBe<InvoiceModel>.None : MayBe.From(active);
    }

    private InvoiceModel ToModel(InvoiceDto dto)
    {
        var key = InvoiceModel.Key(dto.InvoiceId);
        var items = _itemDal.ListData(key).Select(x => new InvoiceItemModel(x.ItemNo, x.SalesOrderItemNo, x.BrgId, x.BrgName, (InvoiceItemKindEnum)x.ItemKind, x.Qty, x.HargaSatuan, x.Diskon, x.Biaya, x.Tax, x.Total));
        var charges = _chargeDal.ListData(key).Select(x => new InvoiceItemChargeModel(x.ItemNo, x.ChargeNo, x.ChargeName, x.Amount));
        return InvoiceModel.Rehydrate(dto.InvoiceId, dto.SalesOrderId, (PayerPathEnum)dto.PayerPath, (InvoiceStatusEnum)dto.InvoiceStatus,
            dto.PricingSnapshotAt, dto.TipeJaminanId, dto.TipeJaminanName, dto.SubTotal, dto.SumBiaya, dto.SumTax, dto.DiskonLain, dto.BiayaLain,
            dto.Pembulatan, dto.GrandTotal, dto.PaymentClearanceReff, dto.PaymentClearedAt, dto.TataRekeningChargeId, dto.TataRekeningCorrectionReff,
            dto.EstablishedAt, dto.IssuedAt, dto.ClearedAt, dto.Version, items, charges);
    }

    private static InvoiceDto ToDto(InvoiceModel m)
        => new(m.InvoiceId, m.SalesOrderId, (int)m.PayerPath, (int)m.InvoiceStatus, m.PricingSnapshotAt, m.TipeJaminanId, m.TipeJaminanName,
            m.SubTotal, m.SumBiaya, m.SumTax, m.DiskonLain, m.BiayaLain, m.Pembulatan, m.GrandTotal, m.PaymentClearanceReff, m.PaymentClearedAt,
            m.TataRekeningChargeId, m.TataRekeningCorrectionReff, m.EstablishedAt, m.IssuedAt, m.ClearedAt, m.Version,
            "", DateTime.Now, "", DateTime.Now, "", new DateTime(3000,1,1));
}
