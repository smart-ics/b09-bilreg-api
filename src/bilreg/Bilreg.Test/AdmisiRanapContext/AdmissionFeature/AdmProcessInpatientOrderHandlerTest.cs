using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using FluentAssertions;
using Moq;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmProcessInpatientOrderHandlerTest
{
    [Fact]
    public async Task GivenCommand_WhenHandled_ThenDelegatesToOrchestrator()
    {
        var orchestrator = new Mock<IAdmissionRegistrationOrchestrator>();
        var command = new AdmProcessInpatientOrderCmd(
            "OPN26K040001", "1", "B1", "user1", RegistrationData());
        var expected = new AdmProcessAdmissionResponse("RG00000001", AdmissionStatusEnum.Admitted);
        orchestrator.Setup(x => x.ProcessInpatientOrder(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actual = await new AdmProcessInpatientOrderHandler(orchestrator.Object)
            .Handle(command, CancellationToken.None);

        actual.Should().Be(expected);
        orchestrator.Verify(x => x.ProcessInpatientOrder(command, CancellationToken.None), Times.Once);
    }

    private static AdmissionRegistrationData RegistrationData() =>
        new("J1", "CM1", "IGD", "R1", "D1", "PESERTA1");
}
