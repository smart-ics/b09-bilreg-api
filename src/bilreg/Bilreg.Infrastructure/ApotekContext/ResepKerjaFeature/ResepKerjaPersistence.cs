using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.ApotekContext.ResepKerjaFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ApotekContext.ResepKerjaFeature;

public record ResepKerjaDto(
    string ResepKerjaId, int SourceKind, string SourceResepId, string RegId, string PasienId, string PasienName,
    string DokterId, string DokterName, string LayananId, int Urgenitas, int IterEntitled, int IterConsumed,
    int CareSetting, string CaptureNote, string DocumentRef, int ResepKerjaStatus, bool ItemsFrozen,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);

public record ResepKerjaItemDto(
    string ResepKerjaId, int ItemNo, int SourceItemNo, string BrgId, string BrgName, string SatuanId, string SatuanName,
    decimal Qty, int Iter, string Signa, string Instruction, string Note, bool IsRacik);

public record ResepKerjaComponentDto(
    string ResepKerjaId, int ItemNo, int ComponentNo, string BrgId, string BrgName, string SatuanId, decimal Qty);

public interface IResepKerjaDal : IInsert<ResepKerjaDto>, IUpdate<ResepKerjaDto>, IGetData<ResepKerjaDto, IResepKerjaKey>
{
    ResepKerjaDto? GetBySource(int sourceKind, string sourceResepId);
}

public interface IResepKerjaItemDal : IInsertBulk<ResepKerjaItemDto>, IDelete<IResepKerjaKey>, IListData<ResepKerjaItemDto, IResepKerjaKey>
{
}

public interface IResepKerjaComponentDal : IInsertBulk<ResepKerjaComponentDto>, IDelete<IResepKerjaKey>, IListData<ResepKerjaComponentDto, IResepKerjaKey>
{
}

public class ResepKerjaDal : IResepKerjaDal
{
    private readonly DatabaseOptions _opt;
    public ResepKerjaDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(ResepKerjaDto model)
    {
        const string sql = """
            INSERT INTO BILRG_AptResepKerja (
                ResepKerjaId, SourceKind, SourceResepId, RegId, PasienId, PasienName, DokterId, DokterName,
                LayananId, Urgenitas, IterEntitled, IterConsumed, CareSetting, CaptureNote, DocumentRef,
                ResepKerjaStatus, ItemsFrozen, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @ResepKerjaId, @SourceKind, @SourceResepId, @RegId, @PasienId, @PasienName, @DokterId, @DokterName,
                @LayananId, @Urgenitas, @IterEntitled, @IterConsumed, @CareSetting, @CaptureNote, @DocumentRef,
                @ResepKerjaStatus, @ItemsFrozen, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, model);
    }

    public void Update(ResepKerjaDto model)
    {
        const string sql = """
            UPDATE BILRG_AptResepKerja SET
                SourceKind=@SourceKind, SourceResepId=@SourceResepId, RegId=@RegId, PasienId=@PasienId,
                PasienName=@PasienName, DokterId=@DokterId, DokterName=@DokterName, LayananId=@LayananId,
                Urgenitas=@Urgenitas, IterEntitled=@IterEntitled, IterConsumed=@IterConsumed, CareSetting=@CareSetting,
                CaptureNote=@CaptureNote, DocumentRef=@DocumentRef, ResepKerjaStatus=@ResepKerjaStatus,
                ItemsFrozen=@ItemsFrozen, UpdUser=@UpdUser, UpdDate=@UpdDate, VodUser=@VodUser, VodDate=@VodDate
            WHERE ResepKerjaId=@ResepKerjaId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, model);
    }

    public ResepKerjaDto GetData(IResepKerjaKey key)
    {
        const string sql = "SELECT * FROM BILRG_AptResepKerja WHERE ResepKerjaId=@ResepKerjaId";
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<ResepKerjaDto>(sql, new { key.ResepKerjaId })!;
    }

    public ResepKerjaDto? GetBySource(int sourceKind, string sourceResepId)
    {
        const string sql = """
            SELECT * FROM BILRG_AptResepKerja
            WHERE SourceKind=@sourceKind AND SourceResepId=@sourceResepId AND VodDate='3000-01-01'
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<ResepKerjaDto>(sql, new { sourceKind, sourceResepId });
    }
}

public class ResepKerjaItemDal : IResepKerjaItemDal
{
    private readonly DatabaseOptions _opt;
    public ResepKerjaItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<ResepKerjaItemDto> listModel)
    {
        const string sql = """
            INSERT INTO BILRG_AptResepKerjaItem (
                ResepKerjaId, ItemNo, SourceItemNo, BrgId, BrgName, SatuanId, SatuanName, Qty, Iter, Signa,
                Instruction, Note, IsRacik)
            VALUES (
                @ResepKerjaId, @ItemNo, @SourceItemNo, @BrgId, @BrgName, @SatuanId, @SatuanName, @Qty, @Iter, @Signa,
                @Instruction, @Note, @IsRacik)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, listModel);
    }

    public void Delete(IResepKerjaKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptResepKerjaItem WHERE ResepKerjaId=@ResepKerjaId", new { key.ResepKerjaId });
    }

    public IEnumerable<ResepKerjaItemDto> ListData(IResepKerjaKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ResepKerjaItemDto>(
            "SELECT * FROM BILRG_AptResepKerjaItem WHERE ResepKerjaId=@ResepKerjaId ORDER BY ItemNo",
            new { filter.ResepKerjaId });
    }
}

public class ResepKerjaComponentDal : IResepKerjaComponentDal
{
    private readonly DatabaseOptions _opt;
    public ResepKerjaComponentDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<ResepKerjaComponentDto> listModel)
    {
        const string sql = """
            INSERT INTO BILRG_AptResepKerjaComponent (
                ResepKerjaId, ItemNo, ComponentNo, BrgId, BrgName, SatuanId, Qty)
            VALUES (@ResepKerjaId, @ItemNo, @ComponentNo, @BrgId, @BrgName, @SatuanId, @Qty)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, listModel);
    }

    public void Delete(IResepKerjaKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptResepKerjaComponent WHERE ResepKerjaId=@ResepKerjaId", new { key.ResepKerjaId });
    }

    public IEnumerable<ResepKerjaComponentDto> ListData(IResepKerjaKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ResepKerjaComponentDto>(
            "SELECT * FROM BILRG_AptResepKerjaComponent WHERE ResepKerjaId=@ResepKerjaId ORDER BY ItemNo, ComponentNo",
            new { filter.ResepKerjaId });
    }
}

public class ResepKerjaRepo : IResepKerjaRepo
{
    private readonly IResepKerjaDal _dal;
    private readonly IResepKerjaItemDal _itemDal;
    private readonly IResepKerjaComponentDal _componentDal;

    public ResepKerjaRepo(IResepKerjaDal dal, IResepKerjaItemDal itemDal, IResepKerjaComponentDal componentDal)
    {
        _dal = dal;
        _itemDal = itemDal;
        _componentDal = componentDal;
    }

    public void SaveChanges(ResepKerjaModel model)
    {
        var dto = ToDto(model);
        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);

        if (!model.ItemsFrozen || existing is null)
        {
            _itemDal.Delete(model);
            _itemDal.Insert(model.Items.Select(x => new ResepKerjaItemDto(
                model.ResepKerjaId, x.ItemNo, x.SourceItemNo, x.BrgId, x.BrgName, x.SatuanId, x.SatuanName,
                x.Qty, x.Iter, x.Signa, x.Instruction, x.Note, x.IsRacik)));
            _componentDal.Delete(model);
            _componentDal.Insert(model.Components.Select(x => new ResepKerjaComponentDto(
                model.ResepKerjaId, x.ItemNo, x.ComponentNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty)));
        }
    }

    public MayBe<ResepKerjaModel> LoadEntity(IResepKerjaKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<ResepKerjaModel>.None : MayBe.From(ToModel(dto));
    }

    public MayBe<ResepKerjaModel> LoadBySource(ResepKerjaSourceKindEnum sourceKind, string sourceResepId)
    {
        var dto = _dal.GetBySource((int)sourceKind, sourceResepId);
        return dto is null ? MayBe<ResepKerjaModel>.None : MayBe.From(ToModel(dto));
    }

    private ResepKerjaModel ToModel(ResepKerjaDto dto)
    {
        var key = ResepKerjaModel.Key(dto.ResepKerjaId);
        var items = _itemDal.ListData(key).Select(x => new ResepKerjaItemModel(
            x.ItemNo, x.SourceItemNo, x.BrgId, x.BrgName, x.SatuanId, x.SatuanName, x.Qty, x.Iter,
            x.Signa, x.Instruction, x.Note, x.IsRacik));
        var components = _componentDal.ListData(key).Select(x => new ResepKerjaComponentModel(
            x.ItemNo, x.ComponentNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty));
        return ResepKerjaModel.Rehydrate(
            dto.ResepKerjaId, (ResepKerjaSourceKindEnum)dto.SourceKind, dto.SourceResepId, dto.RegId, dto.PasienId,
            dto.PasienName, dto.DokterId, dto.DokterName, dto.LayananId, dto.Urgenitas, dto.IterEntitled,
            dto.IterConsumed, dto.CareSetting, dto.CaptureNote, dto.DocumentRef,
            (ResepKerjaStatusEnum)dto.ResepKerjaStatus, dto.ItemsFrozen,
            new AuditTrailType(
                new AuditInfoType(dto.CrtUser, dto.CrtDate),
                new AuditInfoType(dto.UpdUser, dto.UpdDate),
                new AuditInfoType(dto.VodUser, dto.VodDate)),
            items, components);
    }

    private static ResepKerjaDto ToDto(ResepKerjaModel model)
        => new(model.ResepKerjaId, (int)model.SourceKind, model.SourceResepId, model.RegId, model.PasienId,
            model.PasienName, model.DokterId, model.DokterName, model.LayananId, model.Urgenitas, model.IterEntitled,
            model.IterConsumed, model.CareSetting, model.CaptureNote, model.DocumentRef, (int)model.ResepKerjaStatus,
            model.ItemsFrozen, model.AuditTrail.Created.UserId, model.AuditTrail.Created.Timestamp,
            model.AuditTrail.Modified.UserId, model.AuditTrail.Modified.Timestamp,
            model.AuditTrail.Voided.UserId, model.AuditTrail.Voided.Timestamp);
}
