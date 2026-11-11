using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public record PharmacyServicePointDto(
    string ServicePointId,
    string DepoFarmasiId,
    string DisplayName,
    string QueuePrefix,
    bool Active,
    string CrtUser,
    DateTime CrtDate,
    string UpdUser,
    DateTime UpdDate,
    string VodUser,
    DateTime VodDate);

public interface IPharmacyServicePointDal :
    IInsert<PharmacyServicePointDto>,
    IUpdate<PharmacyServicePointDto>,
    IDelete<IPharmacyServicePointKey>,
    IGetData<PharmacyServicePointDto, IPharmacyServicePointKey>
{
    PharmacyServicePointDto? Get(string servicePointId);
    IEnumerable<PharmacyServicePointDto> ListActive();
    IEnumerable<PharmacyServicePointDto> ListByDepo(string depoFarmasiId);
}

public class PharmacyServicePointDal : IPharmacyServicePointDal
{
    private readonly DatabaseOptions _opt;

    public PharmacyServicePointDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PharmacyServicePointDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            INSERT INTO BILRG_AptServicePoint (
                ServicePointId, DepoFarmasiId, DisplayName, QueuePrefix, Active,
                CrtUser, CrtDate, UpdUser, UpdDate, VodUser, VodDate)
            VALUES (
                @ServicePointId, @DepoFarmasiId, @DisplayName, @QueuePrefix, @Active,
                @CrtUser, @CrtDate, @UpdUser, @UpdDate, @VodUser, @VodDate)
            """, model);
    }

    public void Update(PharmacyServicePointDto model)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute("""
            UPDATE BILRG_AptServicePoint
            SET DepoFarmasiId = @DepoFarmasiId,
                DisplayName = @DisplayName,
                QueuePrefix = @QueuePrefix,
                Active = @Active,
                UpdUser = @UpdUser,
                UpdDate = @UpdDate,
                VodUser = @VodUser,
                VodDate = @VodDate
            WHERE ServicePointId = @ServicePointId
            """, model);
    }

    public void Delete(IPharmacyServicePointKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            "DELETE FROM BILRG_AptServicePoint WHERE ServicePointId = @ServicePointId",
            new { key.ServicePointId });
    }

    public PharmacyServicePointDto GetData(IPharmacyServicePointKey key)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<PharmacyServicePointDto>(
            "SELECT * FROM BILRG_AptServicePoint WHERE ServicePointId = @ServicePointId AND VodDate = '3000-01-01'",
            new { key.ServicePointId })!;
    }

    public PharmacyServicePointDto? Get(string servicePointId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<PharmacyServicePointDto>(
            "SELECT * FROM BILRG_AptServicePoint WHERE ServicePointId = @servicePointId AND VodDate = '3000-01-01'",
            new { servicePointId });
    }

    public IEnumerable<PharmacyServicePointDto> ListActive()
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PharmacyServicePointDto>(
            "SELECT * FROM BILRG_AptServicePoint WHERE Active = 1 AND VodDate = '3000-01-01' ORDER BY ServicePointId");
    }

    public IEnumerable<PharmacyServicePointDto> ListByDepo(string depoFarmasiId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PharmacyServicePointDto>(
            "SELECT * FROM BILRG_AptServicePoint WHERE DepoFarmasiId = @depoFarmasiId AND VodDate = '3000-01-01' ORDER BY ServicePointId",
            new { depoFarmasiId });
    }
}

public class PharmacyServicePointRepo : IPharmacyServicePointRepo
{
    private readonly IPharmacyServicePointDal _dal;

    public PharmacyServicePointRepo(IPharmacyServicePointDal dal)
    {
        _dal = dal;
    }

    public void SaveChanges(PharmacyServicePointModel model)
    {
        var existing = _dal.GetData(model);
        var now = DateTime.Now;

        if (existing is null)
        {
            var dto = new PharmacyServicePointDto(
                model.ServicePointId,
                model.DepoFarmasiId,
                model.DisplayName,
                model.QueuePrefix,
                model.Active,
                CrtUser: "SYSTEM",
                CrtDate: now,
                UpdUser: "SYSTEM",
                UpdDate: now,
                VodUser: "",
                VodDate: new DateTime(3000, 1, 1));
            _dal.Insert(dto);
        }
        else
        {
            var dto = existing with
            {
                DepoFarmasiId = model.DepoFarmasiId,
                DisplayName = model.DisplayName,
                QueuePrefix = model.QueuePrefix,
                Active = model.Active,
                UpdUser = "SYSTEM",
                UpdDate = now
            };
            _dal.Update(dto);
        }
    }

    public MayBe<PharmacyServicePointModel> LoadEntity(IPharmacyServicePointKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null ? MayBe<PharmacyServicePointModel>.None : MayBe.From(ToModel(dto));
    }

    public IReadOnlyList<PharmacyServicePointModel> ListActive()
        => _dal.ListActive().Select(ToModel).ToList();

    public IReadOnlyList<PharmacyServicePointModel> ListByDepo(string depoFarmasiId)
        => _dal.ListByDepo(depoFarmasiId).Select(ToModel).ToList();

    private static PharmacyServicePointModel ToModel(PharmacyServicePointDto dto)
        => PharmacyServicePointModel.Rehydrate(dto.ServicePointId, dto.DepoFarmasiId, dto.DisplayName, dto.QueuePrefix, dto.Active);
}
