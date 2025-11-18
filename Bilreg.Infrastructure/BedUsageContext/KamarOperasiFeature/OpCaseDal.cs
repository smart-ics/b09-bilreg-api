using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOpCaseDal :
    IInsert<OpCaseDto>,
    IUpdate<OpCaseDto>,
    IDelete<IOrderOpKey>,
    IGetData<OpCaseDto, IOrderOpKey>,
    IListData<OpCaseDto, Periode>
{
}

public class OpCaseDal : IOpCaseDal
{
    private readonly DatabaseOptions _opt;

    public OpCaseDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(OpCaseDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OpCase(
                OrderOpId, OrderDate, NamaOperasi, PasienId, RegId, 
                ScheduleOpId, ScheduledDate, DischargeOpId, DischargedDate, 
                OrderOpState)
            VALUES( 
                @OrderOpId, @OrderDate, @NamaOperasi, @PasienId, @RegId, 
                @ScheduleOpId, @ScheduledDate, @DischargeOpId, @DischargedDate, 
                @OrderOpState)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", dto.OrderDate, SqlDbType.DateTime);
        dp.AddParam("@NamaOperasi", dto.NamaOperasi, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduledDate", dto.ScheduledDate, SqlDbType.DateTime);
        dp.AddParam("@DischargeOpId", dto.DischargeOpId, SqlDbType.VarChar);
        dp.AddParam("@DischargedDate", dto.DischargedDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpState", dto.OpState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OpCaseDto dto)
    {
        const string sql = @"
           UPDATE 
               BILRG_OpCase
           SET
               OrderDate = @OrderDate,
               NamaOperasi = @NamaOperasi,
               PasienId = @PasienId,
               RegId = @RegId,
               ScheduleOpId = @ScheduleOpId,
               ScheduledDate = @ScheduledDate,
               DischargeOpId = @DischargeOpId,
               DischargedDate = @DischargedDate,
               OrderOpState = @OrderOpState
           WHERE
               OrderOpId = @OrderOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderDate", dto.OrderDate, SqlDbType.DateTime);
        dp.AddParam("@NamaOperasi", dto.NamaOperasi, SqlDbType.VarChar);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@RegId", dto.RegId, SqlDbType.VarChar);
        dp.AddParam("@ScheduleOpId", dto.ScheduleOpId, SqlDbType.VarChar);
        dp.AddParam("@ScheduledDate", dto.ScheduledDate, SqlDbType.DateTime);
        dp.AddParam("@DischargeOpId", dto.DischargeOpId, SqlDbType.VarChar);
        dp.AddParam("@DischargedDate", dto.DischargedDate, SqlDbType.DateTime);
        dp.AddParam("@OrderOpState", dto.OpState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_OpCase
           WHERE
               OrderOpId = @OrderOpId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OpCaseDto GetData(IOrderOpKey key)
    {
        const string sql = """
           SELECT
               aa.OrderOpId, aa.OrderDate, aa.NamaOperasi, aa.PasienId, aa.RegId,
               aa.ScheduleOpId, aa.ScheduledDate, aa.DischargeOpId, aa.DischargedDate,
               aa.OrderOpState,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender
           FROM 
               BILRG_OpCase aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
           WHERE
               OrderOpId = @OrderOpId
           """;
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<OpCaseDto>(sql, dp);
        return result;
    }

    public IEnumerable<OpCaseDto> ListData(Periode periode)
    {
        const string sql = """
            SELECT
                aa.OrderOpId, aa.OrderDate, aa.NamaOperasi, aa.PasienId, aa.RegId,
                aa.ScheduleOpId, aa.ScheduledDate, aa.DischargeOpId, aa.DischargedDate,
                aa.OrderOpState,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fd_tgl_lahir, '') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender
            FROM 
                BILRG_OpCase aa
                LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            WHERE
                OrderDate BETWEEN @Tgl1 AND @Tgl2
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@Tgl1", periode.Tgl1, SqlDbType.DateTime);
        dp.AddParam("@Tgl2", periode.Tgl2, SqlDbType.DateTime);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpCaseDto>(sql, dp);
    }
}

public class OpCaseDalTest
{
    private readonly OpCaseDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseDto Faker()
        => new OpCaseDto(
            OrderOpId: "A",
            OrderDate: new DateTime(2024, 1, 1, 10, 0, 0),
            NamaOperasi: "B",
            PasienId: "C",
            RegId: "D",
            ScheduleOpId: "E",
            ScheduledDate: new DateTime(2024, 1, 2, 8, 0, 0),
            DischargeOpId: "F",
            DischargedDate: new DateTime(2024, 1, 3, 12, 0, 0),
            OpState: 1,
            PasienName: "G",
            TglLahir: "2000-01-01",
            Gender: "H"
        );

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

    private static Periode FakerPeriode()
        => new Periode(new DateTime(2024, 1, 1),
            new DateTime(2024, 1, 31));

    [Fact]
    public void InsertTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Update(Faker());
    }

    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Delete(FakerKey());
    }

    [Fact]
    public void GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
        var actual = _sut.GetData(FakerKey());
        actual.Should().BeEquivalentTo(Faker(), 
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender));
    }
    
    [Fact]
    public void ListDataTest()
    {
        using var trans = TransHelper.NewScope();
        var opCase = Faker();
        _sut.Insert(opCase);
        
        var actual = _sut.ListData(FakerPeriode());
        actual.Should().ContainEquivalentOf(opCase,
            opt => opt.Excluding(x => x.PasienName)
                      .Excluding(x => x.TglLahir)
                      .Excluding(x => x.Gender));
    }
}
