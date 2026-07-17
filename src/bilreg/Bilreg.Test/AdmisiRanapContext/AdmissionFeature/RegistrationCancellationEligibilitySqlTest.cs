using Bilreg.Infrastructure.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Infrastructure.Shared.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Options;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class RegistrationCancellationEligibilitySqlTest
{
    [Fact]
    public void IT_RCE_01_OptInRealSqlSchema_ExecutesTheBillingItemCheck()
    {
        var server = Environment.GetEnvironmentVariable("BILREG_CANCELLATION_SQL_SERVER");
        var database = Environment.GetEnvironmentVariable("BILREG_CANCELLATION_SQL_DATABASE");
        var regId = Environment.GetEnvironmentVariable("BILREG_CANCELLATION_SQL_REG_ID");
        if (string.IsNullOrWhiteSpace(server) || string.IsNullOrWhiteSpace(database) || string.IsNullOrWhiteSpace(regId))
            return;

        var dal = new RegistrationCancellationEligibilityDal(Options.Create(new DatabaseOptions
        {
            ServerName = server,
            DbName = database,
        }));

        var act = () => dal.HasBillingItems(regId);

        act.Should().NotThrow();
    }
}
