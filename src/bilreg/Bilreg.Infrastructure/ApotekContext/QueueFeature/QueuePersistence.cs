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
    int DemandKind, string DemandId, string AntrianId, int NoUrut, string PasienTrackerId,
    bool PasienHadir, DateTime PasienHadirAt, int MappingMethod,
    string MappedBy, DateTime MappedAt, string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate)
{
    public QueueMappingDto() : this(0, "", "", 0, "", false, new DateTime(3000, 1, 1), 0, "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1), "", new DateTime(3000, 1, 1)) { }
}
public record QueueCloseDto(
    string QueueCloseId, string AntrianId, int NoUrut, string Reason, string StaffId, DateTime EffectiveAt,
    string CrtUser, DateTime CrtDate, string UpdUser, DateTime UpdDate, string VodUser, DateTime VodDate);

public interface IQueueMappingDal :
    IInsert<QueueMappingDto>,
    IUpdate<QueueMappingDto>,
    IDelete<IQueueMappingKey>,
    IGetData<QueueMappingDto, IQueueMappingKey>
{
    IEnumerable<QueueMappingDto> ListByQueue(string antrianId, int noUrut);
    bool SetPasienHadir(int demandKind, string demandId, string userId, DateTime at);
}
public interface IQueueCloseDal : IInsert<QueueCloseDto>, IGetData<QueueCloseDto, IQueueCloseKey>
{
    QueueCloseDto? GetByQueue(string antrianId, int noUrut);
}

public class QueueMappingDal : IQueueMappingDal
{
    private const string SelectColumns = """
        DemandKind, DemandId, AntrianId, NoUrut, PasienTrackerId,
        PasienHadir, PasienHadirAt, MappingMethod, MappedBy, MappedAt,
        CrtUser, CrtDate, UpdUser, UpdDate
        """;

    private readonly DatabaseOptions _opt;
    public QueueMappingDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;
    public void Insert(QueueMappingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptQueueMapping (DemandKind, DemandId, AntrianId, NoUrut, PasienTrackerId, PasienHadir, PasienHadirAt, MappingMethod, MappedBy, MappedAt, CrtUser, CrtDate, UpdUser, UpdDate)
            VALUES (@DemandKind, @DemandId, @AntrianId, @NoUrut, @PasienTrackerId, @PasienHadir, @PasienHadirAt, @MappingMethod, @MappedBy, @MappedAt, @CrtUser, @CrtDate, @UpdUser, @UpdDate)
            """, model);
    }
    public void Update(QueueMappingDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptQueueMapping SET AntrianId=@AntrianId, NoUrut=@NoUrut, PasienTrackerId=@PasienTrackerId,
                PasienHadir=@PasienHadir, PasienHadirAt=@PasienHadirAt,
                MappingMethod=@MappingMethod, MappedBy=@MappedBy, MappedAt=@MappedAt, UpdUser=@UpdUser, UpdDate=@UpdDate
            WHERE DemandKind=@DemandKind AND DemandId=@DemandId
            """, model);
    }
    public void Delete(IQueueMappingKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("DELETE FROM BILRG_AptQueueMapping WHERE DemandKind=@DemandKind AND DemandId=@DemandId",
            new { DemandKind = (int)key.DemandKind, key.DemandId });
    }
    public QueueMappingDto GetData(IQueueMappingKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<QueueMappingDto>(
            $"SELECT {SelectColumns} FROM BILRG_AptQueueMapping WHERE DemandKind=@DemandKind AND DemandId=@DemandId",
            new { DemandKind = (int)key.DemandKind, key.DemandId })!;
    }
    public IEnumerable<QueueMappingDto> ListByQueue(string antrianId, int noUrut)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<QueueMappingDto>(
            $"SELECT {SelectColumns} FROM BILRG_AptQueueMapping WHERE AntrianId=@antrianId AND NoUrut=@noUrut",
            new { antrianId, noUrut });
    }
    public bool SetPasienHadir(int demandKind, string demandId, string userId, DateTime at)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var rows = conn.Execute("""
            UPDATE BILRG_AptQueueMapping
            SET PasienHadir = 1,
                PasienHadirAt = @at,
                UpdUser = @userId,
                UpdDate = @at
            WHERE DemandKind = @demandKind AND DemandId = @demandId
            """, new { demandKind, demandId, userId, at });
        return rows > 0;
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

public class QueueMappingRepo : IQueueMappingRepo, IPharmacyQueueMappingRepo
{
    private readonly IQueueMappingDal _dal;
    public QueueMappingRepo(IQueueMappingDal dal) => _dal = dal;
    public void SaveChanges(QueueMappingModel model)
    {
        var dto = new QueueMappingDto(
            (int)model.DemandKind, model.DemandId, model.AntrianId, model.NoUrut, model.PasienTrackerId,
            model.PasienHadir, model.PasienHadirAt,
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
    public bool SetPasienHadir(QueueDemandKindEnum demandKind, string demandId, string userId, DateTime at)
        => _dal.SetPasienHadir((int)demandKind, demandId, userId, at);
    private static QueueMappingModel ToModel(QueueMappingDto dto)
    {
        return QueueMappingModel.Rehydrate(
            (QueueDemandKindEnum)dto.DemandKind,
            dto.DemandId,
            dto.AntrianId,
            dto.NoUrut,
            dto.PasienTrackerId,
            (QueueMappingMethodEnum)dto.MappingMethod,
            dto.MappedBy,
            dto.MappedAt,
            dto.PasienHadir,
            dto.PasienHadirAt);
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
