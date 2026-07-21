using Bilreg.Application.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature;
using Bilreg.Application.AdmisiRanapContext.RolloutFeature.UseCases;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.RolloutFeature;

public class AdmGetRolloutStatusHandlerTest
{
    private readonly Mock<IAdmisiRanapRolloutDal> _rolloutDalMock = new();

    [Fact]
    public async Task UT01_GivenAllTablesExist_WhenGetStatus_ThenAllTablesReady()
    {
        _rolloutDalMock.Setup(x => x.TableExists(It.IsAny<string>())).Returns(true);

        var handler = new AdmGetRolloutStatusHandler(
            _rolloutDalMock.Object,
            Options.Create(new AdmisiRanapOptions { Enabled = true }));

        var status = await handler.Handle(new AdmGetRolloutStatusQry(), CancellationToken.None);

        status.Enabled.Should().BeTrue();
        status.AllTablesReady.Should().BeTrue();
        status.Tables.Should().HaveCount(4);
        status.Tables.Should().OnlyContain(t => t.Ready);
    }

    [Fact]
    public async Task UT02_GivenMissingTable_WhenGetStatus_ThenNotAllReady()
    {
        _rolloutDalMock
            .Setup(x => x.TableExists("BILRG_AdmAdmission"))
            .Returns(false);
        _rolloutDalMock
            .Setup(x => x.TableExists(It.Is<string>(n => n != "BILRG_AdmAdmission")))
            .Returns(true);

        var handler = new AdmGetRolloutStatusHandler(
            _rolloutDalMock.Object,
            Options.Create(new AdmisiRanapOptions { Enabled = false }));

        var status = await handler.Handle(new AdmGetRolloutStatusQry(), CancellationToken.None);

        status.Enabled.Should().BeFalse();
        status.AllTablesReady.Should().BeFalse();
        status.Tables.Single(t => t.Name == "BILRG_AdmAdmission").Ready.Should().BeFalse();
    }
}
