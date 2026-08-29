using Bilreg.Application.ApotekContext.TelaahResepFeature;
using Bilreg.Domain.ApotekContext.TelaahResepFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.TelaahResepFeature;

public record TelaahDto(
    string TelaahResepId, string ResepKerjaId, string RegId, int TelaahStatus, string PharmacistId,
    DateTime StartedAt, DateTime CompletedAt, int Version,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);

public record TelaahItemDto(
    string TelaahResepId, int ItemNo, int ResepKerjaItemNo, int Disposition, string AcceptedBrgId, string AcceptedBrgName,
    decimal AcceptedQty, string Reason, string PharmacistId);

public interface ITelaahDal : IInsert<TelaahDto>, IUpdate<TelaahDto>, IGetData<TelaahDto, ITelaahResepKey>
{
    TelaahDto? GetByResepKerja(string resepKerjaId);
}
public interface ITelaahItemDal : IInsertBulk<TelaahItemDto>, IDelete<ITelaahResepKey>, IListData<TelaahItemDto, ITelaahResepKey> { }

public class TelaahDal : ITelaahDal
{
    private readonly DatabaseOptions _opt;
    public TelaahDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(TelaahDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptTelaahResep (TelaahResepId, ResepKerjaId, RegId, TelaahStatus, PharmacistId, StartedAt, CompletedAt, Version,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@TelaahResepId, @ResepKerjaId, @RegId, @TelaahStatus, @PharmacistId, @StartedAt, @CompletedAt, @Version,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public void Update(TelaahDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptTelaahResep SET TelaahStatus=@TelaahStatus, PharmacistId=@PharmacistId, StartedAt=@StartedAt,
                CompletedAt=@CompletedAt, Version=@Version, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE TelaahResepId=@TelaahResepId
            """, model);
    }
    public TelaahDto GetData(ITelaahResepKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<TelaahDto>("SELECT * FROM BILRG_AptTelaahResep WHERE TelaahResepId=@TelaahResepId", new { key.TelaahResepId })!;
    }
    public TelaahDto? GetByResepKerja(string resepKerjaId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<TelaahDto>("SELECT * FROM BILRG_AptTelaahResep WHERE ResepKerjaId=@resepKerjaId AND VodDate='3000-01-01'", new { resepKerjaId });
    }
}

public class TelaahItemDal : ITelaahItemDal
{
    private readonly DatabaseOptions _opt;
    public TelaahItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<TelaahItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptTelaahResepItem (TelaahResepId, ItemNo, ResepKerjaItemNo, Disposition, AcceptedBrgId, AcceptedBrgName, AcceptedQty, Reason, PharmacistId)
            VALUES (@TelaahResepId, @ItemNo, @ResepKerjaItemNo, @Disposition, @AcceptedBrgId, @AcceptedBrgName, @AcceptedQty, @Reason, @PharmacistId)
            """, listModel);
    }
    public void Delete(ITelaahResepKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptTelaahResepItem WHERE TelaahResepId=@TelaahResepId", new { key.TelaahResepId });
    }
    public IEnumerable<TelaahItemDto> ListData(ITelaahResepKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<TelaahItemDto>("SELECT * FROM BILRG_AptTelaahResepItem WHERE TelaahResepId=@TelaahResepId ORDER BY ItemNo", new { filter.TelaahResepId });
    }
}

public class TelaahResepRepo : ITelaahResepRepo
{
    private readonly ITelaahDal _dal;
    private readonly ITelaahItemDal _itemDal;
    public TelaahResepRepo(ITelaahDal dal, ITelaahItemDal itemDal) { _dal = dal; _itemDal = itemDal; }

    public void SaveChanges(TelaahResepModel model)
    {
        var dto = new TelaahDto(model.TelaahResepId, model.ResepKerjaId, model.RegId, (int)model.TelaahStatus, model.PharmacistId,
            model.StartedAt, model.CompletedAt, model.Version, model.PharmacistId, DateTime.Now, model.PharmacistId, DateTime.Now, "", new DateTime(3000,1,1));
        var existing = _dal.GetData(model);
        if (existing is null) _dal.Insert(dto); else _dal.Update(dto);
        if (!model.IsTerminal)
        {
            _itemDal.Delete(model);
            _itemDal.Insert(model.Items.Select(x => new TelaahItemDto(model.TelaahResepId, x.ItemNo, x.ResepKerjaItemNo, (int)x.Disposition,
                x.AcceptedBrgId, x.AcceptedBrgName, x.AcceptedQty, x.Reason, x.PharmacistId)));
        }
    }

    public MayBe<TelaahResepModel> LoadEntity(ITelaahResepKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<TelaahResepModel>.None : MayBe.From(ToModel(dto));
    }

    public MayBe<TelaahResepModel> LoadByResepKerja(string resepKerjaId)
    {
        var dto = _dal.GetByResepKerja(resepKerjaId);
        return dto is null ? MayBe<TelaahResepModel>.None : MayBe.From(ToModel(dto));
    }

    private TelaahResepModel ToModel(TelaahDto dto)
    {
        var items = _itemDal.ListData(TelaahResepModel.Key(dto.TelaahResepId)).Select(x =>
            new TelaahResepItemModel(x.ItemNo, x.ResepKerjaItemNo, (TelaahDispositionEnum)x.Disposition,
                x.AcceptedBrgId, x.AcceptedBrgName, x.AcceptedQty, x.Reason, x.PharmacistId));
        return TelaahResepModel.Rehydrate(dto.TelaahResepId, dto.ResepKerjaId, dto.RegId, (TelaahStatusEnum)dto.TelaahStatus,
            dto.PharmacistId, dto.StartedAt, dto.CompletedAt, dto.Version, items);
    }
}
