using Bilreg.Domain.AdmisiContext.RegFeature;
using FluentAssertions;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AntrianFeature;

public class RegistrationOutcomeTest
{
    [Fact]
    public void EstablishedOutcome_RequiresRegistrationAndHasNoReason()
    {
        var outcome = RegistrationOutcomeModel.Established("Q", 1, "RG1", "u", DateTime.Now);

        outcome.ReasonCode.Should().BeEmpty();
        FluentActions.Invoking(() =>
                RegistrationOutcomeModel.Established("Q", 1, "", "u", DateTime.Now))
            .Should().Throw<ArgumentException>();
    }
}
