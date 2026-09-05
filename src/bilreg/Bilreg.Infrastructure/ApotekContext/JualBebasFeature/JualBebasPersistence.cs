using Bilreg.Application.ApotekContext.JualBebasFeature;
using Bilreg.Domain.ApotekContext.JualBebasFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.JualBebasFeature;

public record JualBebasDto(
    string JualBebasId, string RegId, string PasienId, string PasienName, string AcceptedBy, DateTime AcceptedAt,
    int RequestStatus, int Version, string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate,
    string VodUser, DateTime VodDate);

public record JualBebasItemDto(string JualBebasId, int ItemNo, string BrgId, string BrgName, string SatuanId, decimal Qty, string Signa);

public interface IJualBebasDal : IInsert<JualBebasDto>, IUpdate<JualBebasDto>, IGetData<JualBebasDto, IJualBebasKey> { }
public interface IJualBebasItemDal : IInsertBulk<JualBebasItemDto>, IDelete<IJualBebasKey>, IListData<JualBebasItemDto, IJualBebasKey> { }

public class JualBebasDal : IJualBebasDal
{
    private readonly DatabaseOptions _opt;
    public JualBebasDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(JualBebasDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptJualBebas (JualBebasId, RegId, PasienId, PasienName, AcceptedBy, AcceptedAt, RequestStatus,
                Version, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@JualBebasId, @RegId, @PasienId, @PasienName, @AcceptedBy, @AcceptedAt, @RequestStatus,
                @Version, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public void Update(JualBebasDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptJualBebas SET RequestStatus=@RequestStatus, Version=@Version,
                UpdUser=@UpdUser, UpdDate=@UpdDate, VodUser=@VodUser, VodDate=@VodDate
            WHERE JualBebasId=@JualBebasId
            """, model);
    }
    public JualBebasDto GetData(IJualBebasKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<JualBebasDto>("SELECT * FROM BILRG_AptJualBebas WHERE JualBebasId=@JualBebasId", new { key.JualBebasId })!;
    }
}

public class JualBebasItemDal : IJualBebasItemDal
{
    private readonly DatabaseOptions _opt;
    public JualBebasItemDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(IEnumerable<JualBebasItemDto> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptJualBebasItem (JualBebasId, ItemNo, BrgId, BrgName, SatuanId, Qty, Signa)
            VALUES (@JualBebasId, @ItemNo, @BrgId, @BrgName, @SatuanId, @Qty, @Signa)
            """, listModel);
    }
    public void Delete(IJualBebasKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptJualBebasItem WHERE JualBebasId=@JualBebasId", new { key.JualBebasId });
    }
    public IEnumerable<JualBebasItemDto> ListData(IJualBebasKey filter)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<JualBebasItemDto>("SELECT * FROM BILRG_AptJualBebasItem WHERE JualBebasId=@JualBebasId ORDER BY ItemNo", new { filter.JualBebasId });
    }
}

public class JualBebasRepo : IJualBebasRepo
{
    private readonly IJualBebasDal _dal;
    private readonly IJualBebasItemDal _itemDal;
    public JualBebasRepo(IJualBebasDal dal, IJualBebasItemDal itemDal) { _dal = dal; _itemDal = itemDal; }

    public void SaveChanges(JualBebasModel model)
    {
        var dto = new JualBebasDto(model.JualBebasId, model.RegId, model.PasienId, model.PasienName, model.AcceptedBy,
            model.AcceptedAt, (int)model.RequestStatus, model.Version, model.AcceptedBy, model.AcceptedAt,
            "", new DateTime(3000, 1, 1), model.DeclinedBy, model.DeclinedAt);
        if (_dal.GetData(model) is null)
        {
            _dal.Insert(dto);
            _itemDal.Insert(model.Items.Select(x => new JualBebasItemDto(model.JualBebasId, x.ItemNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty, x.Signa)));
        }
        else
            _dal.Update(dto);
    }

    public MayBe<JualBebasModel> LoadEntity(IJualBebasKey key)
    {
        var dto = _dal.GetData(key);
        if (dto is null) return MayBe<JualBebasModel>.None;
        var items = _itemDal.ListData(key).Select(x => new JualBebasItemModel(x.ItemNo, x.BrgId, x.BrgName, x.SatuanId, x.Qty, x.Signa));
        return MayBe.From(JualBebasModel.Rehydrate(dto.JualBebasId, dto.RegId, dto.PasienId, dto.PasienName,
            dto.AcceptedBy, dto.AcceptedAt, (JualBebasRequestStatusEnum)dto.RequestStatus, dto.Version,
            dto.VodUser, dto.VodDate, items));
    }
}
