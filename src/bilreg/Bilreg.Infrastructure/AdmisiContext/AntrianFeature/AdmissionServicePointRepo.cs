using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.AdmisiContext.AntrianFeature;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using Dapper;
using Microsoft.Extensions.Options;
using Nuna.Lib.PatternHelper;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public sealed record AdmissionServicePointDto(string ServicePointId, string DisplayName,
    string QueuePrefix, int ServicePointStatus);

public interface IAdmissionServicePointDal
{
    AdmissionServicePointDto? GetData(IAdmissionServicePointKey key);
    void Insert(AdmissionServicePointDto dto);
    void Update(AdmissionServicePointDto dto);
    IReadOnlyList<AdmissionServicePointDto> ListAll();
}

public sealed class AdmissionServicePointDal : IAdmissionServicePointDal
{
    private readonly DatabaseOptions _opt;
    public AdmissionServicePointDal(IOptions<DatabaseOptions> opt) => _opt = opt.Value;

    public AdmissionServicePointDto? GetData(IAdmissionServicePointKey key)
    {
        const string sql = """
            SELECT ServicePointId, DisplayName, QueuePrefix, ServicePointStatus
            FROM BILRG_AdmServicePoint WHERE ServicePointId = @ServicePointId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.QuerySingleOrDefault<AdmissionServicePointDto>(sql,
            new { key.ServicePointId });
    }

    public void Insert(AdmissionServicePointDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_AdmServicePoint
                (ServicePointId, DisplayName, QueuePrefix, ServicePointStatus)
            VALUES (@ServicePointId, @DisplayName, @QueuePrefix, @ServicePointStatus)
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dto);
    }

    public void Update(AdmissionServicePointDto dto)
    {
        const string sql = """
            UPDATE BILRG_AdmServicePoint
            SET DisplayName=@DisplayName, QueuePrefix=@QueuePrefix,
                ServicePointStatus=@ServicePointStatus
            WHERE ServicePointId=@ServicePointId
            """;
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dto);
    }
    public IReadOnlyList<AdmissionServicePointDto> ListAll()
    {
        const string sql="SELECT ServicePointId,DisplayName,QueuePrefix,ServicePointStatus FROM BILRG_AdmServicePoint ORDER BY DisplayName";
        using var conn=new SqlConnection(ConnStringHelper.Get(_opt)); return conn.Query<AdmissionServicePointDto>(sql).ToList();
    }
}

public sealed class AdmissionServicePointRepo : IAdmissionServicePointRepo
{
    private readonly IAdmissionServicePointDal _dal;
    public AdmissionServicePointRepo(IAdmissionServicePointDal dal) => _dal = dal;

    public MayBe<AdmissionServicePointModel> LoadEntity(IAdmissionServicePointKey key)
    {
        var dto = _dal.GetData(key);
        return dto is null
            ? MayBe<AdmissionServicePointModel>.None
            : MayBe.From(AdmissionServicePointModel.Load(dto.ServicePointId, dto.DisplayName,
                dto.QueuePrefix, (AdmissionServicePointStatusEnum)dto.ServicePointStatus));
    }

    public void SaveChanges(AdmissionServicePointModel model)
    {
        var dto = new AdmissionServicePointDto(model.ServicePointId, model.DisplayName,
            model.QueuePrefix, (int)model.Status);
        if (_dal.GetData(model) is null) _dal.Insert(dto); else _dal.Update(dto);
    }
    public IReadOnlyList<AdmissionServicePointModel> ListAll()=>_dal.ListAll().Select(x=>
        AdmissionServicePointModel.Load(x.ServicePointId,x.DisplayName,x.QueuePrefix,
            (AdmissionServicePointStatusEnum)x.ServicePointStatus)).ToList();
}
