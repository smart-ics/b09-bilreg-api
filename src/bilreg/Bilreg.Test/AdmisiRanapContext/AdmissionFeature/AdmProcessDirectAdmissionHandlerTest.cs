using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using FluentAssertions;
using FluentValidation.TestHelper;
using Moq;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmProcessDirectAdmissionHandlerTest
{
    [Fact]
    public async Task GivenCommand_WhenHandled_ThenDelegatesToOrchestrator()
    {
        var orchestrator = new Mock<IAdmissionRegistrationOrchestrator>();
        var command = new AdmProcessDirectAdmissionCmd(
            "P001", "1", "B1", "user1", RegistrationData());
        var expected = new AdmProcessAdmissionResponse("RG00000001", AdmissionStatusEnum.Admitted);
        orchestrator.Setup(x => x.ProcessDirectAdmission(command, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        var actual = await new AdmProcessDirectAdmissionHandler(orchestrator.Object)
            .Handle(command, CancellationToken.None);

        actual.Should().Be(expected);
        orchestrator.Verify(x => x.ProcessDirectAdmission(command, CancellationToken.None), Times.Once);
    }

    [Fact]
    public void GivenEmptyRequiredFields_WhenValidate_ThenHasValidationErrors()
    {
        var validator = new AdmProcessDirectAdmissionValidator();
        var invalidCmd = new AdmProcessDirectAdmissionCmd(
            "", "", "", "",
            new AdmissionRegistrationData("", "", "", null, "", ""));

        var result = validator.TestValidate(invalidCmd);

        result.ShouldHaveValidationErrorFor(x => x.PasienId);
        result.ShouldHaveValidationErrorFor(x => x.KelasDkId);
        result.ShouldHaveValidationErrorFor(x => x.BangsalId);
        result.ShouldHaveValidationErrorFor(x => x.UserId);
        result.ShouldHaveValidationErrorFor(x => x.Registration.DokterId);
        result.ShouldHaveValidationErrorFor(x => x.Registration.TipeJaminanId);
        result.ShouldHaveValidationErrorFor(x => x.Registration.CaraMasukDkId);
        result.ShouldHaveValidationErrorFor(x => x.Registration.ProsedurMasukInapId);
    }

    [Fact]
    public void GivenValidCommand_WhenValidate_ThenNoValidationErrors()
    {
        var validator = new AdmProcessDirectAdmissionValidator();
        var validCmd = new AdmProcessDirectAdmissionCmd(
            "P001", "1", "B1", "user1", RegistrationData());

        var result = validator.TestValidate(validCmd);

        result.ShouldNotHaveAnyValidationErrors();
    }

    private static AdmissionRegistrationData RegistrationData() =>
        new("J1", "CM1", "IGD", "R1", "D1", "PESERTA1");
}
