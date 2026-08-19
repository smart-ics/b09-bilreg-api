using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using System.Data.SqlClient;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public record QueueMappingDto(
    int DemandKind, string DemandId, string AntrianId, int NoUrut, string PasienTrackerId, int MappingMethod,
    string MappedBy, DateTime MappedAt, string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate);
public record QueueCloseDto(
    string QueueCloseId, string AntrianId, int NoUrut, string Reason, string StaffId, DateTime EffectiveAt,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);

public interface IQueueMappingDal : IInsert<QueueMappingDto>, IUpdate<QueueMappingDto>, IGetData<QueueMappingDto, IQueueMappingKey>
{
    IEnumerable<QueueMappingDto> ListByQueue(string antrianId, int noUrut);
}
public interface IQueueCloseDal : IInsert<QueueCloseDto>, IGetData<QueueCloseDto, IQueueCloseKey>
{
    QueueCloseDto? GetByQueue(string antrianId, int noUrut);
}

public class QueueMappingDal : IQueueMappingDal
{
    private readonly DatabaseOptions _opt;
    public QueueMappingDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(QueueMappingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptQueueMapping (DemandKind, DemandId, AntrianId, NoUrut, PasienTrackerId, MappingMethod, MappedBy, MappedAt, CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES (@DemandKind, @DemandId, @AntrianId, @NoUrut, @PasienTrackerId, @MappingMethod, @MappedBy, @MappedAt, @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """, model);
    }
    public void Update(QueueMappingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptQueueMapping SET AntrianId=@AntrianId, NoUrut=@NoUrut, PasienTrackerId=@PasienTrackerId,
                MappedBy=@MappedBy, MappedAt=@MappedAt, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE DemandKind=@DemandKind AND DemandId=@DemandId
            """, model);
    }
    public QueueMappingDto GetData(IQueueMappingKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<QueueMappingDto>(
            "SELECT * FROM BILRG_AptQueueMapping WHERE DemandKind=@DemandKind AND DemandId=@DemandId",
            new { DemandKind = (int)key.DemandKind, key.DemandId })!;
    }
    public IEnumerable<QueueMappingDto> ListByQueue(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<QueueMappingDto>("SELECT * FROM BILRG_AptQueueMapping WHERE AntrianId=@antrianId AND NoUrut=@noUrut", new { antrianId, noUrut });
    }
}

public class QueueCloseDal : IQueueCloseDal
{
    private readonly DatabaseOptions _opt;
    public QueueCloseDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(QueueCloseDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptQueueClose (QueueCloseId, AntrianId, NoUrut, Reason, StaffId, EffectiveAt, CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (@QueueCloseId, @AntrianId, @NoUrut, @Reason, @StaffId, @EffectiveAt, @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }
    public QueueCloseDto GetData(IQueueCloseKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<QueueCloseDto>("SELECT * FROM BILRG_AptQueueClose WHERE QueueCloseId=@QueueCloseId", new { key.QueueCloseId })!;
    }
    public QueueCloseDto? GetByQueue(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<QueueCloseDto>("SELECT * FROM BILRG_AptQueueClose WHERE AntrianId=@antrianId AND NoUrut=@noUrut", new { antrianId, noUrut });
    }
}

public class QueueMappingRepo : IQueueMappingRepo
{
    private readonly IQueueMappingDal _dal;
    public QueueMappingRepo(IQueueMappingDal dal) => _dal = dal;
    public void SaveChanges(QueueMappingModel model)
    {
        var dto = new QueueMappingDto((int)model.DemandKind, model.DemandId, model.AntrianId, model.NoUrut, model.PasienTrackerId,
            (int)model.MappingMethod, model.MappedBy, model.MappedAt, model.MappedBy, model.MappedAt, model.MappedBy, model.MappedAt);
        if (_dal.GetData(model) is null) _dal.Insert(dto); else _dal.Update(dto);
    }
    public MayBe<QueueMappingModel> LoadEntity(IQueueMappingKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<QueueMappingModel>.None : MayBe.From(ToModel(dto));
    }
    public IReadOnlyList<QueueMappingModel> ListByQueue(string antrianId, int noUrut)
        => _dal.ListByQueue(antrianId, noUrut).Select(ToModel).ToList();
    private static QueueMappingModel ToModel(QueueMappingDto dto)
    {
        var model = QueueMappingModel.Create((QueueDemandKindEnum)dto.DemandKind, dto.DemandId, dto.AntrianId, dto.NoUrut,
            dto.PasienTrackerId, (QueueMappingMethodEnum)dto.MappingMethod, dto.MappedBy, dto.MappedAt);
        return model;
    }
}

public class QueueCloseRepo : IQueueCloseRepo
{
    private readonly IQueueCloseDal _dal;
    public QueueCloseRepo(IQueueCloseDal dal) => _dal = dal;
    public void SaveChanges(QueueCloseModel model)
    {
        if (_dal.GetData(model) is not null) return;
        _dal.Insert(new QueueCloseDto(model.QueueCloseId, model.AntrianId, model.NoUrut, model.Reason, model.StaffId, model.EffectiveAt,
            model.StaffId, model.EffectiveAt, "", new DateTime(3000,1,1), "", new DateTime(3000,1,1)));
    }
    public MayBe<QueueCloseModel> LoadEntity(IQueueCloseKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<QueueCloseModel>.None : MayBe.From(QueueCloseModel.Rehydrate(dto.QueueCloseId, dto.AntrianId, dto.NoUrut, dto.Reason, dto.StaffId, dto.EffectiveAt));
    }
    public MayBe<QueueCloseModel> LoadByQueue(string antrianId, int noUrut)
    {
        var dto = _dal.GetByQueue(antrianId, noUrut);
        return dto is null ? MayBe<QueueCloseModel>.None : MayBe.From(QueueCloseModel.Rehydrate(dto.QueueCloseId, dto.AntrianId, dto.NoUrut, dto.Reason, dto.StaffId, dto.EffectiveAt));
    }
}
