using System.Data;
using System.Data.SqlClient;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.PatternHelper;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.PasienFeature;

public interface IPasien2Dal : 
    IInsert<Pasien2Dto>,
    IUpdate<Pasien2Dto>,
    IDelete<IPasienKey>,
    IGetDataMayBe<Pasien2Dto, IPasienKey>{}

public class Pasien2Dal : IPasien2Dal
{
    private readonly DatabaseOptions _opt;

    public Pasien2Dal(IOptions<DatabaseOptions>  opt)
    {
        _opt = opt.Value;
    }

    public void Insert(Pasien2Dto model)
    {
        const string sql = @"
            INSERT INTO BILRG_Pasien(
                PasienId, AlamatKtp1, AlamatKtp2, 
                AlamatKtp3, AlamatKtpKota, AlamatKtpKodePos)
            VALUES(
                @PasienId, @AlamatKtp1, @AlamatKtp2, 
                @AlamatKtp3, @AlamatKtpKota, @AlamatKtpKodePos)";
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp1", model.AlamatKtp1, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp2", model.AlamatKtp2, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp3", model.AlamatKtp3, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtpKota", model.AlamatKtpKota, SqlDbType.VarChar);      
        dp.AddParam("@AlamatKtpKodePos", model.AlamatKtpKodePos, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Update(Pasien2Dto model)
    {
        const string sql = @"
            UPDATE
                BILRG_Pasien
            SET
                AlamatKtp1 = @AlamatKtp1, 
                AlamatKtp2 = @AlamatKtp2, 
                AlamatKtp3 = @AlamatKtp3, 
                AlamatKtpKota = @AlamatKtpKota, 
                AlamatKtpKodePos = @AlamatKtpKodePos
            WHERE
                PasienId = @PasienId ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp1", model.AlamatKtp1, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp2", model.AlamatKtp2, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtp3", model.AlamatKtp3, SqlDbType.VarChar);
        dp.AddParam("@AlamatKtpKota", model.AlamatKtpKota, SqlDbType.VarChar);      
        dp.AddParam("@AlamatKtpKodePos", model.AlamatKtpKodePos, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public void Delete(IPasienKey key)
    {
        const string sql = @"
            DELETE FROM
                BILRG_Pasien
            WHERE
                PasienId = @PasienId ";
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public MayBe<Pasien2Dto> GetData(IPasienKey key)
    {
        const string sql = @"
            SELECT
                PasienId, AlamatKtp1, AlamatKtp2, 
                AlamatKtp3, AlamatKtpKota, AlamatKtpKodePos
            FROM BILRG_Pasien
            WHERE PasienId = @PasienId";
        
        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return MayBe.From(conn.ReadSingle<Pasien2Dto>(sql, dp));
    }
}

public class Pasien2DalTest
{
    private readonly Pasien2Dal _sut;
    public Pasien2DalTest()
    {
        _sut = new Pasien2Dal(ConnStringHelper.GetTestEnv());
    }
    
    private Pasien2Dto Faker()
    => new Pasien2Dto( "A", "B", "C", "D", "E", "F" );
    
    [Fact]
    public void UT1_InserTest()
    {
        using var trans = TransHelper.NewScope();
        _sut.Insert(Faker());
    }
    
    [Fact]
    public void UT2_UpdateTest()
    {
        using var trans = TransHelper.NewScope();
        var faker = Faker();
        _sut.Update(faker);
    }
    
    [Fact]
    public void UT3_DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var faker = Faker();
        _sut.Delete(faker);
    }
    
    [Fact]
    public void UT4_GetDataTest()
    {
        using var trans = TransHelper.NewScope();
        var faker = Faker();
        _sut.Insert(faker);
        var actual = _sut.GetData(PasienModel.Key("A")).Value;
        actual.Should().BeEquivalentTo(faker);
    }
}