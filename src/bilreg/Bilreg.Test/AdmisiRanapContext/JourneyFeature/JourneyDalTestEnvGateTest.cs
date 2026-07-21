using FluentAssertions;

namespace Bilreg.Test.AdmisiRanapContext.JourneyFeature;

public class JourneyDalTestEnvGateTest
{
    [Fact]
    public void Missing_Env_Fails_With_Clear_Configuration_Error()
    {
        var prevServer = Environment.GetEnvironmentVariable(JourneyDalTestEnv.EnvServer);
        var prevDb = Environment.GetEnvironmentVariable(JourneyDalTestEnv.EnvDatabase);
        try
        {
            Environment.SetEnvironmentVariable(JourneyDalTestEnv.EnvServer, null);
            Environment.SetEnvironmentVariable(JourneyDalTestEnv.EnvDatabase, null);

            var act = () => JourneyDalTestEnv.RequireConfiguredConnection();
            act.Should().Throw<InvalidOperationException>()
                .WithMessage($"*{JourneyDalTestEnv.EnvServer}*");
        }
        finally
        {
            Environment.SetEnvironmentVariable(JourneyDalTestEnv.EnvServer, prevServer);
            Environment.SetEnvironmentVariable(JourneyDalTestEnv.EnvDatabase, prevDb);
        }
    }
}
