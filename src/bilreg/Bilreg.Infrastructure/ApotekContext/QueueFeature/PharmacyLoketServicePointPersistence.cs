using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public record ApotekLoketServicePointDto(
    string LoketKey,
    string ServicePointId,
    string CrtUser,
    DateTime CrtDate);

public interface IPharmacyLoketServicePointDal
{
    IEnumerable<ApotekLoketServicePointDto> ListByLoket(string loketKey);
    IEnumerable<ApotekLoketServicePointDto> ListByServicePoint(string servicePointId);
    void Insert(ApotekLoketServicePointDto dto);
    void Delete(string loketKey, string servicePointId);
    void DeleteByLoket(string loketKey);
    void Replace(string loketKey, IEnumerable<string> servicePointIds, string actor, DateTime at);
}

public class PharmacyLoketServicePointDal : IPharmacyLoketServicePointDal
{
    private readonly DatabaseOptions _opt;

    public PharmacyLoketServicePointDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<ApotekLoketServicePointDto> ListByLoket(string loketKey)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ApotekLoketServicePointDto>(
            """
            SELECT LoketKey, ServicePointId, CrtUser, CrtDate
            FROM BILRG_AptLoketServicePoint
            WHERE LoketKey = @loketKey
            ORDER BY ServicePointId ASC
            """,
            new { loketKey });
    }

    public IEnumerable<ApotekLoketServicePointDto> ListByServicePoint(string servicePointId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<ApotekLoketServicePointDto>(
            """
            SELECT LoketKey, ServicePointId, CrtUser, CrtDate
            FROM BILRG_AptLoketServicePoint
            WHERE ServicePointId = @servicePointId
            ORDER BY LoketKey ASC
            """,
            new { servicePointId });
    }

    public void Insert(ApotekLoketServicePointDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            """
            INSERT INTO BILRG_AptLoketServicePoint (LoketKey, ServicePointId, CrtUser, CrtDate)
            VALUES (@LoketKey, @ServicePointId, @CrtUser, @CrtDate)
            """,
            dto);
    }

    public void Delete(string loketKey, string servicePointId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            """
            DELETE FROM BILRG_AptLoketServicePoint
            WHERE LoketKey = @loketKey AND ServicePointId = @servicePointId
            """,
            new { loketKey, servicePointId });
    }

    public void DeleteByLoket(string loketKey)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            """
            DELETE FROM BILRG_AptLoketServicePoint
            WHERE LoketKey = @loketKey
            """,
            new { loketKey });
    }

    public void Replace(string loketKey, IEnumerable<string> servicePointIds, string actor, DateTime at)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Open();
        using var tx = conn.BeginTransaction();
        try
        {
            conn.Execute(
                """
                DELETE FROM BILRG_AptLoketServicePoint
                WHERE LoketKey = @loketKey
                """,
                new { loketKey },
                tx);

            var user = string.IsNullOrWhiteSpace(actor) ? "SYSTEM" : actor.Trim();
            var dtos = servicePointIds?
                .Where(x => !string.IsNullOrWhiteSpace(x))
                .Select(x => x.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Select(spId => new ApotekLoketServicePointDto(loketKey, spId, user, at))
                .ToList();

            if (dtos is { Count: > 0 })
            {
                conn.Execute(
                    """
                    INSERT INTO BILRG_AptLoketServicePoint (LoketKey, ServicePointId, CrtUser, CrtDate)
                    VALUES (@LoketKey, @ServicePointId, @CrtUser, @CrtDate)
                    """,
                    dtos,
                    tx);
            }

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }
    }
}

public class PharmacyLoketServicePointRepo : IPharmacyLoketServicePointRepo
{
    private readonly IPharmacyLoketServicePointDal _dal;

    public PharmacyLoketServicePointRepo(IPharmacyLoketServicePointDal dal)
    {
        _dal = dal;
    }

    public IReadOnlyList<string> ListServicePointsByLoket(string loketKey)
    {
        if (string.IsNullOrWhiteSpace(loketKey))
            return Array.Empty<string>();

        return _dal.ListByLoket(loketKey.Trim())
            .Select(x => x.ServicePointId)
            .ToList();
    }

    public IReadOnlyList<ApotekLoketServicePointModel> ListMappingsByLoket(string loketKey)
    {
        if (string.IsNullOrWhiteSpace(loketKey))
            return Array.Empty<ApotekLoketServicePointModel>();

        return _dal.ListByLoket(loketKey.Trim())
            .Select(ToModel)
            .ToList();
    }

    public void ReplaceServicePoints(string loketKey, IEnumerable<string> servicePointIds, string actor, DateTime at)
    {
        if (string.IsNullOrWhiteSpace(loketKey))
            throw new ArgumentException("LoketKey is required.", nameof(loketKey));

        _dal.Replace(loketKey.Trim(), servicePointIds, actor, at);
    }

    private static ApotekLoketServicePointModel ToModel(ApotekLoketServicePointDto dto)
        => ApotekLoketServicePointModel.Rehydrate(dto.LoketKey, dto.ServicePointId, dto.CrtUser, dto.CrtDate);
}
