using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;

public interface IOpCaseAktifDal :
    IInsert<OpCaseAktifDto>,
    IUpdate<OpCaseAktifDto>,
    IDelete<IOrderOpKey>,
    IGetData<OpCaseAktifDto, IOrderOpKey>,
    IListData<OpCaseAktifDto>
{
}

public class OpCaseAktifDal : IOpCaseAktifDal
{
    private readonly DatabaseOptions _opt;

    public OpCaseAktifDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }
    
    public void Insert(OpCaseAktifDto dto)
    {
        const string sql = """
            INSERT INTO BILRG_OpCaseAktif(
                OrderOpId, OrderOpDate, PasienId, OrderOpState)
            VALUES( 
                @OrderOpId, @OrderOpDate, @PasienId, @OrderOpState)
            """;

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpDate", dto.OrderOpDate, SqlDbType.DateTime);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpState", dto.OrderOpState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(OpCaseAktifDto dto)
    {
        const string sql = @"
           UPDATE 
               BILRG_OpCaseAktif
           SET
               OrderOpDate = @OrderOpDate,
               PasienId = @PasienId,
               OrderOpState = @OrderOpState
           WHERE
               OrderOpId = @OrderOpId";

        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", dto.OrderOpId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpDate", dto.OrderOpDate, SqlDbType.DateTime);
        dp.AddParam("@PasienId", dto.PasienId, SqlDbType.VarChar);
        dp.AddParam("@OrderOpState", dto.OrderOpState, SqlDbType.Int);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IOrderOpKey key)
    {
        const string sql = @"
           DELETE FROM 
                BILRG_OpCaseAktif
           WHERE
               OrderOpId = @OrderOpId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public OpCaseAktifDto GetData(IOrderOpKey key)
    {
        const string sql = @"
           SELECT
               aa.OrderOpId, aa.OrderOpDate, aa.PasienId, aa.OrderOpState,
               ISNULL(bb.fs_nm_pasien, '') AS PasienName,
               ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
               ISNULL(bb.fs_jns_kelamin, '') AS Gender
           FROM 
               BILRG_OpCaseAktif aa
               LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
           WHERE
               OrderOpId = @OrderOpId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@OrderOpId", key.OrderOpId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        var result = conn.ReadSingle<OpCaseAktifDto>(sql, dp);
        return result;
    }

    public IEnumerable<OpCaseAktifDto> ListData()
    {
        const string sql = """
            SELECT
                aa.OrderOpId, aa.OrderOpDate, aa.PasienId, aa.OrderOpState,
                ISNULL(bb.fs_nm_pasien, '') AS PasienName,
                ISNULL(bb.fd_tgl_lahir, '3000-01-01') AS TglLahir,
                ISNULL(bb.fs_jns_kelamin, '') AS Gender
            FROM 
                BILRG_OpCaseAktif aa
                LEFT JOIN tc_mr bb ON aa.PasienId = bb.fs_mr
            """;
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<OpCaseAktifDto>(sql);
    }
}

public class OpCaseAktifDalTest
{
    private readonly OpCaseAktifDal _sut = new(ConnStringHelper.GetTestEnv());

    private static OpCaseAktifDto Faker()
        => new OpCaseAktifDto(
            OrderOpId: "A",
            OrderOpDate: new DateTime(2024, 1, 1, 10, 0, 0),
            PasienId: "B",
            OrderOpState: 1,
            PasienName: "C",
            TglLahir: "2000-01-01",
            Gender: "D"
        );

    private static IOrderOpKey FakerKey()
        => OrderOpModel.Key("A");

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
        _sut.Insert(Faker());
        var actual = _sut.ListData();
        actual.Should().ContainEquivalentOf(Faker(),
            opt => opt.Excluding(x => x.PasienName)
                .Excluding(x => x.TglLahir)
                .Excluding(x => x.Gender));
    }
}