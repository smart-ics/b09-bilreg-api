using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.AdmisiContext.AntrianFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.AdmisiContext.AntrianFeature;

public interface IPasienTrackerDal :
    IInsert<PasienTrackerDto>,
    IUpdate<PasienTrackerDto>,
    IDelete<IPasienTrackerKey>,
    IGetData<PasienTrackerDto, IPasienTrackerKey>,
    IListData<PasienTrackerDto, Periode>
{
}

public class PasienTrackerDal : IPasienTrackerDal
{
    private readonly DatabaseOptions _opt;

    public PasienTrackerDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(PasienTrackerDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_PasienTracker(
               PasienTrackerId, PersonName, TglLahir, VisitDate)
            VALUES (
               @PasienTrackerId, @PersonName, @TglLahir, @VisitDate)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@VisitDate", dto.VisitDate, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(PasienTrackerDto dto)
    {
        const string sql = """
            UPDATE
                BILRG_PasienTracker
            SET
                PersonName = @PersonName, 
                TglLahir = @TglLahir, 
                VisitDate = @VisitDate
           WHERE
                PasienTrackerId = @PasienTrackerId
           """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", dto.PasienTrackerId, SqlDbType.VarChar);
        dp.AddParam("@PersonName", dto.PersonName, SqlDbType.VarChar);
        dp.AddParam("@TglLahir", dto.TglLahir, SqlDbType.DateTime);
        dp.AddParam("@VisitDate", dto.VisitDate, SqlDbType.DateTime);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPasienTrackerKey key)
    {
        const string sql = """
            DELETE FROM
                BILRG_PasienTracker
            WHERE
                PasienTrackerId = @PasienTrackerId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", key.PasienTrackerId, SqlDbType.VarChar);
    
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public PasienTrackerDto GetData(IPasienTrackerKey key)
    {           
        const string sql = """
            SELECT
                PasienTrackerId, PersonName, TglLahir, VisitDate
            FROM
                BILRG_PasienTracker
            WHERE
                PasienTrackerId = @PasienTrackerId
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@PasienTrackerId", key.PasienTrackerId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.ReadSingle<PasienTrackerDto>(sql, dp);
    }
    
    public IEnumerable<PasienTrackerDto> ListData(Periode filter)
    {
        const string sql = """
            SELECT
               PasienTrackerId, PersonName, TglLahir, VisitDate
            FROM
               BILRG_PasienTracker
            WHERE
               VisitDate BETWEEN @Tgl1 AND @Tgl2
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", filter.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", filter.Tgl2, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienTrackerDto>(sql, dp);
    }
}

public class PasienTrackerDalTest
{
    private readonly PasienTrackerDal _sut = new(ConnStringHelper.GetTestEnv());

    private static PasienTrackerDto Faker()
        => new PasienTrackerDto("A", "B", new DateTime(2025, 12, 1), new DateTime(2025, 2, 3));
    
    private static IPasienTrackerKey FakerKey()
        => PasienTrackerModel.Key("A");
    
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
        _sut.Delete(FakerKey());
    }
    
    [Fact]
    public void UT4_GetData()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().NotBeNull();
        actual.Should().BeEquivalentTo(Faker());
    }
    [Fact]
    public void UT5_ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.ListData(new Periode(new DateTime(2025, 12, 1), new DateTime(2025, 2, 3)));
        actual.Should().ContainEquivalentOf(Faker());
        _sut.Delete(FakerKey());
    }
}