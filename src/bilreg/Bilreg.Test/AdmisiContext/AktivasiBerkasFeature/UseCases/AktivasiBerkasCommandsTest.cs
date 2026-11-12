using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;
using Bilreg.Application.AdmisiContext.RegFeature;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.PpaFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using Moq;
using Nuna.Lib.PatternHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AktivasiBerkasFeature.UseCases;

public class AktivasiBerkasCommandsTest
{
    private readonly Mock<IAktivasiBerkasEmrGateway> _gatewayMock;
    private readonly Mock<IRegRepo> _regRepoMock;

    public AktivasiBerkasCommandsTest()
    {
        _gatewayMock = new Mock<IAktivasiBerkasEmrGateway>();
        _regRepoMock = new Mock<IRegRepo>();
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenTglAktifIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Tanggal aktif");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenReqUserrIdIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ReqUserrId");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenLayananIdIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Layanan tujuan");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenListRegisterIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: []
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("Daftar register");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenRegisterItemIsVoidInDto()
    {
        // Arrange
        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister:
            [
                new AktivasiBerkasRegisterRefDto("REG-OK", "3000-01-01"),
                new AktivasiBerkasRegisterRefDto("REG-VOID", "2026-11-09")
            ]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("VOID");
        result.Message.Should().Contain("REG-VOID");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldFail_WhenRegisterIsVoidInLocalRepo()
    {
        // Arrange
        var voidRegModel = RegModel.Default;
        voidRegModel.BatalBerobat("USER-VOID", new DateTime(2026, 11, 8, 14, 0, 0));

        _regRepoMock
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-LOCAL-VOID")))
            .Returns(MayBe.From(voidRegModel));

        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object, _regRepoMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-LOCAL-VOID")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("VOID");
        result.Message.Should().Contain("REG-LOCAL-VOID");
        _gatewayMock.Verify(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task SaveCommand_ShouldCallGateway_WhenValid()
    {
        // Arrange
        _regRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        _gatewayMock
            .Setup(g => g.Save(It.IsAny<AktivasiBerkasSaveDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AktivasiBerkasMutationResult.Ok("AKT-NEW", "Berhasil disimpan"));

        var handler = new AktivasiBerkasSaveHandler(_gatewayMock.Object, _regRepoMock.Object);
        var cmd = new AktivasiBerkasSaveCommand(
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01", "3000-01-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AktifasiBerkasId.Should().Be("AKT-NEW");
        _gatewayMock.Verify(g => g.Save(
            It.Is<AktivasiBerkasSaveDto>(d =>
                d.ReqUserrId == "USR-01" &&
                d.TglAktif == "2026-11-09" &&
                d.LayananId == "LAY-01" &&
                d.ListRegister.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateCommand_ShouldFail_WhenIdIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasUpdateHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasUpdateCommand(
            Id: "",
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("ID transaksi");
        _gatewayMock.Verify(g => g.Update(It.IsAny<string>(), It.IsAny<AktivasiBerkasUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommand_ShouldFail_WhenRegisterItemIsVoidInDto()
    {
        // Arrange
        var handler = new AktivasiBerkasUpdateHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasUpdateCommand(
            Id: "AKT-100",
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-VOID", "2026-11-09")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("VOID");
        _gatewayMock.Verify(g => g.Update(It.IsAny<string>(), It.IsAny<AktivasiBerkasUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommand_ShouldFail_WhenRegisterIsVoidInLocalRepo()
    {
        // Arrange
        var voidRegModel = RegModel.Default;
        voidRegModel.BatalBerobat("USER-VOID", new DateTime(2026, 11, 8, 14, 0, 0));

        _regRepoMock
            .Setup(r => r.LoadEntity(It.Is<IRegKey>(k => k.RegId == "REG-LOCAL-VOID")))
            .Returns(MayBe.From(voidRegModel));

        var handler = new AktivasiBerkasUpdateHandler(_gatewayMock.Object, _regRepoMock.Object);
        var cmd = new AktivasiBerkasUpdateCommand(
            Id: "AKT-100",
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-LOCAL-VOID")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("VOID");
        result.Message.Should().Contain("REG-LOCAL-VOID");
        _gatewayMock.Verify(g => g.Update(It.IsAny<string>(), It.IsAny<AktivasiBerkasUpdateDto>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task UpdateCommand_ShouldCallGateway_WhenValid()
    {
        // Arrange
        _regRepoMock
            .Setup(r => r.LoadEntity(It.IsAny<IRegKey>()))
            .Returns(MayBe<RegModel>.None);

        _gatewayMock
            .Setup(g => g.Update("AKT-100", It.IsAny<AktivasiBerkasUpdateDto>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(AktivasiBerkasMutationResult.Ok("AKT-100", "Berhasil diperbarui"));

        var handler = new AktivasiBerkasUpdateHandler(_gatewayMock.Object, _regRepoMock.Object);
        var cmd = new AktivasiBerkasUpdateCommand(
            Id: "AKT-100",
            TglJam: "2026-11-09 10:00:00",
            ReqUserrId: "USR-01",
            TglAktif: "2026-11-09",
            AktifasiUserrId: "USR-02",
            LayananId: "LAY-01",
            ListRegister: [new AktivasiBerkasRegisterRefDto("REG-01", "3000-01-01")]
        );

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.AktifasiBerkasId.Should().Be("AKT-100");
        _gatewayMock.Verify(g => g.Update(
            "AKT-100",
            It.Is<AktivasiBerkasUpdateDto>(d =>
                d.ReqUserrId == "USR-01" &&
                d.TglAktif == "2026-11-09" &&
                d.LayananId == "LAY-01" &&
                d.ListRegister.Count() == 1),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeactivateCommand_ShouldFail_WhenRequestIsNull()
    {
        // Arrange
        var handler = new AktivasiBerkasDeactivateHandler(_gatewayMock.Object);

        // Act
        var result = await handler.Handle(null!, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("permintaan");
        _gatewayMock.Verify(g => g.Deactivate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateCommand_ShouldFail_WhenRegIdIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasDeactivateHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasDeactivateCommand("", "USR-01");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("RegId");
        _gatewayMock.Verify(g => g.Deactivate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateCommand_ShouldFail_WhenUserrIdIsEmpty()
    {
        // Arrange
        var handler = new AktivasiBerkasDeactivateHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasDeactivateCommand("REG-01", "");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.Message.Should().Contain("UserrId");
        _gatewayMock.Verify(g => g.Deactivate(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeactivateCommand_ShouldCallGateway_WhenValid()
    {
        // Arrange
        _gatewayMock
            .Setup(g => g.Deactivate("REG-01", "USR-01", It.IsAny<CancellationToken>()))
            .ReturnsAsync(AktivasiBerkasMutationResult.Ok("REG-01", "Register berhasil di nonaktifkan"));

        var handler = new AktivasiBerkasDeactivateHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasDeactivateCommand("REG-01", "USR-01");

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Register berhasil di nonaktifkan");
        _gatewayMock.Verify(g => g.Deactivate("REG-01", "USR-01", It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanUpCommand_ShouldCallGateway_WhenExecuted()
    {
        // Arrange
        _gatewayMock
            .Setup(g => g.CleanUp(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AktivasiBerkasMutationResult.Ok(null, "Seluruh register aktif berhasil dibersihkan"));

        var handler = new AktivasiBerkasCleanUpCommandHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasCleanUpCommand();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeTrue();
        result.Message.Should().Be("Seluruh register aktif berhasil dibersihkan");
        _gatewayMock.Verify(g => g.CleanUp(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CleanUpCommand_ShouldReturnFail_WhenGatewayFails()
    {
        // Arrange
        _gatewayMock
            .Setup(g => g.CleanUp(It.IsAny<CancellationToken>()))
            .ReturnsAsync(AktivasiBerkasMutationResult.Fail("EMR CleanUp internal server error (HTTP 500)", 500));

        var handler = new AktivasiBerkasCleanUpCommandHandler(_gatewayMock.Object);
        var cmd = new AktivasiBerkasCleanUpCommand();

        // Act
        var result = await handler.Handle(cmd, CancellationToken.None);

        // Assert
        result.Success.Should().BeFalse();
        result.StatusCode.Should().Be(500);
        result.Message.Should().Be("EMR CleanUp internal server error (HTTP 500)");
        _gatewayMock.Verify(g => g.CleanUp(It.IsAny<CancellationToken>()), Times.Once);
    }
}
