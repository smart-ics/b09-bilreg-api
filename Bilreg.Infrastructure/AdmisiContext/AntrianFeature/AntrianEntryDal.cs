using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IAntrianEntryDal :
    IInsert<AntrianEntryDto>,
    IUpdate<AntrianEntryDto>,
    IListData<AntrianEntryDto, string>
{
    void Delete(string key, int noUrut);
    AntrianEntryDto GetData(string key, int noUrut);
}

public class AntrianEntryDal : IAntrianEntryDal
{
    private readonly DatabaseOptions _opt;

    public AntrianEntryDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(AntrianEntryDto model)
    {
        const string sql = """
            INSERT INTO BILRG_AntrianEntry(
                AntrianId, NoUrut, PersonName, AntrianStatus,
                CreatedAt, ServedAt, DoneAt) 
            VALUES(
                @AntrianId, @NoUrut, @PersonName, @AntrianStatus,
                @CreatedAt, @ServedAt, @DoneAt)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", model.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", model.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", model.PersonName, SqlDbType.VarChar);	 
        dp.AddParam("@AntrianStatus", model.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", model.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", model.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", model.DoneAt, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(AntrianEntryDto model)
    {
        const string sql = """
           UPDATE
                BILRG_AntrianEntry
           SET
               PersonName = @PersonName, 
               AntrianStatus = @AntrianStatus,
               CreatedAt = @CreatedAt, 
               ServedAt = @ServedAt, 
               DoneAt = @DoneAt 
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", model.AntrianId, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", model.NoUrut, SqlDbType.Int);	 
        dp.AddParam("@PersonName", model.PersonName, SqlDbType.VarChar);	 
        dp.AddParam("@AntrianStatus", model.AntrianStatus, SqlDbType.Int);	
        dp.AddParam("@CreatedAt", model.CreatedAt, SqlDbType.DateTime);	 
        dp.AddParam("@ServedAt", model.ServedAt, SqlDbType.DateTime);	 
        dp.AddParam("@DoneAt", model.DoneAt, SqlDbType.DateTime);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(string key, int noUrut)
    {
        const string sql = """
           DELETE FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public AntrianEntryDto GetData(string key, int noUrut)
    {
        const string sql = """
           SELECT
               AntrianId, NoUrut, PersonName, AntrianStatus,
               CreatedAt, ServedAt, DoneAt
           FROM
                BILRG_AntrianEntry
           WHERE
               AntrianId = @AntrianId 
               AND NoUrut = @NoUrut
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", key, SqlDbType.VarChar); 
        dp.AddParam("@NoUrut", noUrut, SqlDbType.Int);	 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<AntrianEntryDto>(sql, dp);
    }

    public IEnumerable<AntrianEntryDto> ListData(string antrianId)
    {
        const string sql = """
            SELECT
               AntrianId, NoUrut, PersonName, AntrianStatus,
               CreatedAt, ServedAt, DoneAt
            FROM
                BILRG_AntrianEntry
            WHERE
               AntrianId = @AntrianId 
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@AntrianId", antrianId, SqlDbType.VarChar); 

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<AntrianEntryDto>(sql, dp);
    }
}

public class AntrianEntryDalTest
{
    private readonly AntrianEntryDal _sut = new(ConnStringHelper.GetTestEnv());

    private static AntrianEntryDto Faker()
        => new AntrianEntryDto("A", 1, "B", 2,
            new DateTime(2025, 10, 1),
            new DateTime(2025, 10, 2),
            new DateTime(2025, 10, 3));

    [Fact]
    public void UT1_InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }

    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }
    
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete("A", 1);
    }

    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData("A", 1);
        actual.Should().BeEquivalentTo(Faker());
    }

    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData("A");
        actual.Should().ContainEquivalentOf(Faker());
    }
}