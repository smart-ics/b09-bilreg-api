using System.Data;
using System.Data.SqlClient;
using Bilreg.Application.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Domain.PasienContext.DataSosialPasienSub.PasienAgg;
using Bilreg.Infrastructure.Helpers;
using Dapper;
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

    public void Insert(PasienLogModel model)
    {
        const string sql = @"
            INSERT INTO BILREG_PasienLog (PasienId, LogDate, Activity, UserId, ChangeLog)
            VALUES (@PasienId, @LogDate, @Activity, @UserId, @ChangeLog)";

        var dp = new DynamicParameters();
        dp.AddParam("@PasienId", model.PasienId, SqlDbType.VarChar);
        dp.AddParam("@LogDate", model.LogDate.ToString(DateFormatEnum.YMD_HMS), SqlDbType.VarChar);
        dp.AddParam("@Activity", model.Activity, SqlDbType.VarChar);
        dp.AddParam("@UserId", model.UserId, SqlDbType.VarChar);
        dp.AddParam("@ChangeLog", model.ChangeLog, SqlDbType.VarChar);

        using var conn = new SqlConnection(ConnStringHelper.Get(_opt));
        conn.Execute(sql, dp);
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
        _sut.Insert(expected);
    }
}