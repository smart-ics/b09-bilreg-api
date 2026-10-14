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

public record TelaahItemDto
{
    public string TelaahResepId { get; init; } = string.Empty;
    public int ItemNo { get; init; }
    public int ResepKerjaItemNo { get; init; }
    public string PrescribedBrgId { get; init; } = string.Empty;
    public string PrescribedBrgName { get; init; } = string.Empty;
    public decimal PrescribedQty { get; init; }
    public int Disposition { get; init; }
    public string AcceptedBrgId { get; init; } = string.Empty;
    public string AcceptedBrgName { get; init; } = string.Empty;
    public decimal AcceptedQty { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string PharmacistId { get; init; } = string.Empty;

    public TelaahItemDto() { }

    public TelaahItemDto(
        string telaahResepId,
        int itemNo,
        int resepKerjaItemNo,
        int disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId,
        string prescribedBrgId = "",
        string prescribedBrgName = "",
        decimal prescribedQty = 0)
    {
        TelaahResepId = telaahResepId;
        ItemNo = itemNo;
        ResepKerjaItemNo = resepKerjaItemNo;
        Disposition = disposition;
        AcceptedBrgId = acceptedBrgId;
        AcceptedBrgName = acceptedBrgName;
        AcceptedQty = acceptedQty;
        Reason = reason;
        PharmacistId = pharmacistId;
        PrescribedBrgId = prescribedBrgId;
        PrescribedBrgName = prescribedBrgName;
        PrescribedQty = prescribedQty;
    }

    public TelaahItemDto(
        string telaahResepId,
        int itemNo,
        int resepKerjaItemNo,
        string prescribedBrgId,
        string prescribedBrgName,
        decimal prescribedQty,
        int disposition,
        string acceptedBrgId,
        string acceptedBrgName,
        decimal acceptedQty,
        string reason,
        string pharmacistId)
    {
        TelaahResepId = telaahResepId;
        ItemNo = itemNo;
        ResepKerjaItemNo = resepKerjaItemNo;
        PrescribedBrgId = prescribedBrgId;
        PrescribedBrgName = prescribedBrgName;
        PrescribedQty = prescribedQty;
        Disposition = disposition;
        AcceptedBrgId = acceptedBrgId;
        AcceptedBrgName = acceptedBrgName;
        AcceptedQty = acceptedQty;
        Reason = reason;
        PharmacistId = pharmacistId;
    }
}

public record TelaahKajianDto
{
    public string TelaahResepId { get; init; } = string.Empty;
    public int NoUrut { get; init; }
    public string KajianResepId { get; init; } = string.Empty;
    public string KajianResepName { get; init; } = string.Empty;
    public string GrupKajianResepId { get; init; } = string.Empty;
    public string GrupKajianResepName { get; init; } = string.Empty;
    public bool IsYes { get; init; }
    public string CatatanKajian { get; init; } = string.Empty;

    public TelaahKajianDto() { }

    public TelaahKajianDto(
        string telaahResepId,
        int noUrut,
        string kajianResepId,
        string kajianResepName,
        string grupKajianResepId,
        string grupKajianResepName,
        bool isYes,
        string catatanKajian)
    {
        TelaahResepId = telaahResepId;
        NoUrut = noUrut;
        KajianResepId = kajianResepId;
        KajianResepName = kajianResepName;
        GrupKajianResepId = grupKajianResepId;
        GrupKajianResepName = grupKajianResepName;
        IsYes = isYes;
        CatatanKajian = catatanKajian;
    }
}

public interface ITelaahDal : IInsert<TelaahDto>, IUpdate<TelaahDto>, IGetData<TelaahDto, ITelaahResepKey>
{
    TelaahDto? GetByResepKerja(string resepKerjaId);
}
public interface ITelaahItemDal : IInsertBulk<TelaahItemDto>, IDelete<ITelaahResepKey>, IListData<TelaahItemDto, ITelaahResepKey> { }
public interface ITelaahKajianDal : IInsertBulk<TelaahKajianDto>, IDelete<ITelaahResepKey>, IListData<TelaahKajianDto, ITelaahResepKey> { }

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
            INSERT INTO BILRG_AptTelaahResepItem (
                TelaahResepId, ItemNo, ResepKerjaItemNo,
                PrescribedBrgId, PrescribedBrgName, PrescribedQty,
                Disposition, AcceptedBrgId, AcceptedBrgName, AcceptedQty,
                Reason, PharmacistId)
            VALUES (
                @TelaahResepId, @ItemNo, @ResepKerjaItemNo,
                @PrescribedBrgId, @PrescribedBrgName, @PrescribedQty,
                @Disposition, @AcceptedBrgId, @AcceptedBrgName, @AcceptedQty,
                @Reason, @PharmacistId)
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

public class TelaahKajianDal : ITelaahKajianDal
{
    private readonly DatabaseOptions _opt;
    public TelaahKajianDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public void Insert(IEnumerable<TelaahKajianDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptTelaahResepKajian (
                TelaahResepId, NoUrut, KajianResepId, KajianResepName,
                GrupKajianResepId, GrupKajianResepName, IsYes, CatatanKajian)
            VALUES (
                @TelaahResepId, @NoUrut, @KajianResepId, @KajianResepName,
                @GrupKajianResepId, @GrupKajianResepName, @IsYes, @CatatanKajian)
            """, listModel);
    }

    public void Delete(ITelaahResepKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptTelaahResepKajian WHERE TelaahResepId = @TelaahResepId", new { key.TelaahResepId });
    }

    public IEnumerable<TelaahKajianDto> ListData(ITelaahResepKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<TelaahKajianDto>(
            "SELECT * FROM BILRG_AptTelaahResepKajian WHERE TelaahResepId = @TelaahResepId ORDER BY NoUrut",
            new { filter.TelaahResepId });
    }
}

public class TelaahResepRepo : ITelaahResepRepo
{
    private readonly ITelaahDal _dal;
    private readonly ITelaahItemDal _itemDal;
    private readonly ITelaahKajianDal _kajianDal;

    public TelaahResepRepo(ITelaahDal dal, ITelaahItemDal itemDal, ITelaahKajianDal kajianDal)
    {
        _dal = dal;
        _itemDal = itemDal;
        _kajianDal = kajianDal;
    }

    public void SaveChanges(TelaahResepModel model)
    {
        var dto = new TelaahDto(
            model.TelaahResepId,
            model.ResepKerjaId,
            model.RegId,
            (int)model.TelaahStatus,
            model.PharmacistId,
            model.StartedAt,
            model.CompletedAt,
            model.Version,
            model.PharmacistId,
            DateTime.Now,
            model.PharmacistId,
            DateTime.Now,
            "",
            new DateTime(3000, 1, 1));

        var existing = _dal.GetData(model);
        if (existing is null)
            _dal.Insert(dto);
        else
            _dal.Update(dto);

        bool existingIsTerminal = existing is not null && IsTerminalStatus(existing.TelaahStatus);
        if (!existingIsTerminal)
        {
            _itemDal.Delete(model);
            _itemDal.Insert(model.Items.Select(x => new TelaahItemDto(
                model.TelaahResepId,
                x.ItemNo,
                x.ResepKerjaItemNo,
                x.PrescribedBrgId,
                x.PrescribedBrgName,
                x.PrescribedQty,
                (int)x.Disposition,
                x.AcceptedBrgId,
                x.AcceptedBrgName,
                x.AcceptedQty,
                x.Reason,
                x.PharmacistId)));

            _kajianDal.Delete(model);
            _kajianDal.Insert(model.KajianItems.Select(k => new TelaahKajianDto(
                model.TelaahResepId,
                k.NoUrut,
                k.KajianResepId,
                k.KajianResepName,
                k.GrupKajianResepId,
                k.GrupKajianResepName,
                k.IsYes,
                k.CatatanKajian)));
        }
    }

    private static bool IsTerminalStatus(int telaahStatus) =>
        telaahStatus is (int)TelaahStatusEnum.Approved
            or (int)TelaahStatusEnum.PartiallyApproved
            or (int)TelaahStatusEnum.Rejected;

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
        var key = TelaahResepModel.Key(dto.TelaahResepId);

        var kajianDtos = _kajianDal.ListData(key).ToList();
        var kajianItems = kajianDtos.Count == 0
            ? TelaahResepKajianModel.DefaultCatalog()
            : (IReadOnlyList<TelaahResepKajianModel>)kajianDtos.Select(k => TelaahResepKajianModel.Rehydrate(
                k.NoUrut,
                k.KajianResepId,
                k.KajianResepName,
                k.GrupKajianResepId,
                k.GrupKajianResepName,
                k.IsYes,
                k.CatatanKajian)).ToList();

        var itemDtos = _itemDal.ListData(key);
        var items = itemDtos.Select(x => new TelaahResepItemModel(
            x.ItemNo,
            x.ResepKerjaItemNo,
            x.PrescribedBrgId,
            x.PrescribedBrgName,
            x.PrescribedQty,
            (TelaahDispositionEnum)x.Disposition,
            x.AcceptedBrgId,
            x.AcceptedBrgName,
            x.AcceptedQty,
            x.Reason,
            x.PharmacistId));

        return TelaahResepModel.Rehydrate(
            dto.TelaahResepId,
            dto.ResepKerjaId,
            dto.RegId,
            (TelaahStatusEnum)dto.TelaahStatus,
            dto.PharmacistId,
            dto.StartedAt,
            dto.CompletedAt,
            dto.Version,
            items,
            kajianItems);
    }
}
