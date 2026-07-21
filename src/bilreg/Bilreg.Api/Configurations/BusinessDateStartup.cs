using Bilreg.Infrastructure.Shared.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Bilreg.Api.Configurations;

public static class BusinessDateStartup
{
    public static void LogBusinessDateStatus(IHost app)
    {
        var options = app.Services.GetRequiredService<IOptions<BusinessDateOptions>>().Value;
        var env = app.Services.GetRequiredService<IHostEnvironment>();
        var logger = app.Services.GetRequiredService<ILoggerFactory>()
            .CreateLogger("Bilreg.BusinessDate");

        if (options.Mode == BusinessDateMode.System)
        {
            logger.LogInformation("Business Date initialized in System mode.");
            return;
        }

        logger.LogWarning(
            """
            WARNING

            Business Date Simulation Enabled

            Business Date : {BusinessDate}
            Environment   : {Environment}
            """,
            options.FixedDate!.Value.ToString("yyyy-MM-dd"),
            env.EnvironmentName);
    }
}
