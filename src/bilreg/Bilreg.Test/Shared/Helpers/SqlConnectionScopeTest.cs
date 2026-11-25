using System.Data;
using System.Transactions;
using Bilreg.Application.Shared;
using Bilreg.Domain.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.BedUsageContext.BedOperationalFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Nuna.Lib.TransactionHelper;
using Xunit;

namespace Bilreg.Test.Shared.Helpers;

public class SqlConnectionScopeTest
{
    [Fact]
    public void GetConnection_ReturnsOpenConnection_AndReusesSameInstance()
    {
        using var sut = new SqlConnectionScope(ConnStringHelper.GetTestEnv());

        var conn1 = sut.GetConnection();
        var conn2 = sut.GetConnection();

        conn1.Should().NotBeNull();
        conn1.State.Should().Be(ConnectionState.Open);
        conn2.Should().BeSameAs(conn1);
    }

    [Fact]
    public void GetConnection_WithinTransactionScope_EnlistsConnectionInAmbientTransaction()
    {
        using var sut = new SqlConnectionScope(ConnStringHelper.GetTestEnv());

        using var trans = TransHelper.NewScope();
        var conn = sut.GetConnection();

        conn.Should().NotBeNull();
        conn.State.Should().Be(ConnectionState.Open);

        // Second call inside same transaction reuses connection without creating a second connection
        var conn2 = sut.GetConnection();
        conn2.Should().BeSameAs(conn);
    }

    [Fact]
    public void Dispose_ClosesConnectionAndCleansUpState()
    {
        var sut = new SqlConnectionScope(ConnStringHelper.GetTestEnv());
        var conn = sut.GetConnection();
        conn.State.Should().Be(ConnectionState.Open);

        sut.Dispose();

        conn.State.Should().Be(ConnectionState.Closed);
    }

    [Fact]
    public void MultipleDals_SharingSameSqlConnectionScope_ExecuteInSingleLocalTransactionWithoutMsdtcEscalation()
    {
        using var scope = new SqlConnectionScope(ConnStringHelper.GetTestEnv());
        var testEnv = ConnStringHelper.GetTestEnv();

        var dal1 = new BedOperationalDal(testEnv, scope);
        var dal2 = new BedReadinessTransactionDal(testEnv, scope);

        // Opening a transaction scope and accessing multiple DALs should share the same SqlConnection
        using var trans = TransHelper.NewScope();

        var connFromScope = scope.GetConnection();
        connFromScope.State.Should().Be(ConnectionState.Open);

        // Querying or executing via DALs within the transaction scope does not throw MSDTC escalation exception
        var act = () =>
        {
            dal2.ListData(BedOperationalModel.Key("NON_EXISTENT_BED_TEST"));
        };

        act.Should().NotThrow();
    }
}
