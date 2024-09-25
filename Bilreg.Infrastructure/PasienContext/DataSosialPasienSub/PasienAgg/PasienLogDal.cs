using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Nuna.Lib.DataAccessHelper;
using Nuna.Lib.TransactionHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Infrastructure.PasienContext.DataSosialPasienSub.PasienAgg;

public class PasienLogDal: IPasienLogDal
{
    private readonly DatabaseOptions _opt;

    public PasienLogDal(IOptions<DatabaseOptions> opt)
    {
        _opt = opt.Value;
    }

    public void Insert(IEnumerable<PasienLogModel> listModel)
    {
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        using var bcp = new SqlBulkCopy(conn);
        
        conn.Open();
        bcp.AddMap("PasienId", "PasienId");
        bcp.AddMap("LogDate", "LogDate");
        bcp.AddMap("Activity", "Activity");
        bcp.AddMap("UserId", "UserId");
        bcp.AddMap("ChangeLog", "ChangeLog");

        var fetched = listModel.ToList();
        bcp.BatchSize = fetched.Count;
        bcp.DestinationTableName = "BILREG_PasienLog";
        bcp.WriteToServer(fetched.AsDataTable());
    }

    public void Delete(IPasienKey key)
    {
        const string sql = @"
            DELETE FROM 
                BILREG_PasienLog
            WHERE
                PasienId = @PasienId";

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", key.PasienId, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
    }

    public IEnumerable<PasienLogModel> ListData(IPasienKey filter)
    {
        const string sql = @"
            SELECT
                PasienId, LogDate, Activity, UserId, ChangeLog
            FROM
                BILREG_PasienLog
            WHERE
                PasienId = @PasienId";

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", filter.PasienId, SqlDbType.VarChar);
        
        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        return conn.Read<PasienLogDto>(sql, dp);
    }
}

public class PasienLogDalTest
{
    private readonly PasienLogDal _sut;

    public PasienLogDalTest()
    {
        _sut = new PasienLogDal(ConnStringHelper.GetTestEnv());
    }
    
    [Fact]
    public void Insert_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PasienLogModel("A", "B", "C");
        _sut.Insert(new List<PasienLogModel>() {expected});
    }
    
    [Fact]
    public void DeleteTest()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PasienLogModel("A", "B", "C");
        _sut.Delete(expected);
    }

    [Fact]
    public void ListData_Test()
    {
        using var trans = TransHelper.NewScope();
        var expected = new PasienLogModel("A", "B", "C");
        _sut.Insert(new List<PasienLogModel>(){expected});
        var actual = _sut.ListData(expected);
        actual.Should().ContainEquivalentOf(expected);
    }
}