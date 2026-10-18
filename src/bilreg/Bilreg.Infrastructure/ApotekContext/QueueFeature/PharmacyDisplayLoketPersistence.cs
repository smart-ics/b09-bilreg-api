using System.Data.SqlClient;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;

namespace Bilreg.Infrastructure.ApotekContext.QueueFeature;

public record PharmacyDisplayLoketDto(string DisplayId, string LoketKey, int SortOrder);

public interface IPharmacyDisplayLoketDal
{
    IEnumerable<PharmacyDisplayLoketDto> ListByDisplayId(string displayId);
    PharmacyDisplayLoketDto? GetData(string displayId, string loketKey);
    void Insert(PharmacyDisplayLoketDto dto);
    void Delete(string displayId, string loketKey);
}

public class PharmacyDisplayLoketDal : IPharmacyDisplayLoketDal
{
    private readonly DatabaseOptions _opt;

    public PharmacyDisplayLoketDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public IEnumerable<PharmacyDisplayLoketDto> ListByDisplayId(string displayId)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Query<PharmacyDisplayLoketDto>(
            "SELECT DisplayId, LoketKey, SortOrder FROM BILRG_AptDisplayLoket WHERE DisplayId = @displayId ORDER BY SortOrder ASC",
            new { displayId });
    }

    public PharmacyDisplayLoketDto? GetData(string displayId, string loketKey)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QueryFirstOrDefault<PharmacyDisplayLoketDto>(
            "SELECT DisplayId, LoketKey, SortOrder FROM BILRG_AptDisplayLoket WHERE DisplayId = @displayId AND LoketKey = @loketKey",
            new { displayId, loketKey });
    }

    public void Insert(PharmacyDisplayLoketDto dto)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            "INSERT INTO BILRG_AptDisplayLoket (DisplayId, LoketKey, SortOrder) VALUES (@DisplayId, @LoketKey, @SortOrder)",
            dto);
    }

    public void Delete(string displayId, string loketKey)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(
            "DELETE FROM BILRG_AptDisplayLoket WHERE DisplayId = @displayId AND LoketKey = @loketKey",
            new { displayId, loketKey });
    }
}

public class PharmacyDisplayLoketRepo : IPharmacyDisplayLoketRepo
{
    private readonly IPharmacyDisplayLoketDal _dal;

    public PharmacyDisplayLoketRepo(IPharmacyDisplayLoketDal dal)
    {
        _dal = dal;
    }

    public IReadOnlyList<PharmacyDisplayLoketModel> ListByDisplayId(string displayId)
    {
        return _dal.ListByDisplayId(displayId)
            .Select(d => new PharmacyDisplayLoketModel(d.DisplayId, d.LoketKey, d.SortOrder))
            .ToList();
    }

    public void SaveMapping(string displayId, string loketKey, int sortOrder)
    {
        _dal.Delete(displayId, loketKey);
        _dal.Insert(new PharmacyDisplayLoketDto(displayId, loketKey, sortOrder));
    }

    public void DeleteMapping(string displayId, string loketKey)
    {
        _dal.Delete(displayId, loketKey);
    }
}
