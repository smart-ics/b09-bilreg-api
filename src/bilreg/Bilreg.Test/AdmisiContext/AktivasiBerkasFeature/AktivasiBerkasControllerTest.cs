using System.Security.Claims;
using Bilreg.Api.Controllers.AdmisiContext.AktivasiBerkasFeature;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.Integration;
using Bilreg.Application.AdmisiContext.AktivasiBerkasFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiContext.AktivasiBerkasFeature;

public class AktivasiBerkasControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly AktivasiBerkasController _sut;

    public AktivasiBerkasControllerTest()
    {
        _sut = new AktivasiBerkasController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_ShouldHaveExpectedAttributes()
    {
        // ASSERT
        typeof(AktivasiBerkasController).Should().BeDecoratedWith<ApiControllerAttribute>();
        typeof(AktivasiBerkasController).Should().BeDecoratedWith<AuthorizeAttribute>();
        var routeAttr = typeof(AktivasiBerkasController)
            .GetCustomAttributes(typeof(RouteAttribute), false)
            .Cast<RouteAttribute>()
            .FirstOrDefault();

        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");
    }

    [Fact]
    public async Task List_GivenTglAwalAndTglAkhir_WhenExecuted_ThenSendsAktivasiBerkasListQueryAndReturnsOk()
    {
        // ARRANGE
        const string tglAwal = "2026-11-01";
        const string tglAkhir = "2026-11-09";
        var expectedList = new List<AktivasiBerkasListItemDto>
        {
            new()
            {
                AktifasiBerkasId = "AKT001",
                TglJam = "2026-11-09 10:00:00",
                ReqUserrId = "USR01",
                ReqUserrName = "Petugas Poli",
                AktifasiUserrId = "USR02",
                AktifasiUserrName = "Admin RM",
                TglAktif = "2026-11-09",
                LayananId = "LAY01",
                LayananName = "Poli Penyakit Dalam"
            }
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasListQuery>(q => q.TglAwal == tglAwal && q.TglAkhir == tglAkhir),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var actionResult = await _sut.List(tglAwal, tglAkhir);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedList);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasListQuery>(q => q.TglAwal == tglAwal && q.TglAkhir == tglAkhir),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetData_GivenId_WhenExecuted_ThenSendsAktivasiBerkasGetQueryAndReturnsOk()
    {
        // ARRANGE
        const string id = "AKT001";
        var expectedDetail = new AktivasiBerkasDetailDto
        {
            AktifasiBerkasId = id,
            TglJam = "2026-11-09 10:00:00",
            ReqUserrId = "USR01",
            ReqUserrName = "Petugas Poli",
            AktifasiUserrId = "USR02",
            AktifasiUserrName = "Admin RM",
            TglAktif = "2026-11-09",
            LayananId = "LAY01",
            LayananName = "Poli Penyakit Dalam"
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasGetQuery>(q => q.Id == id),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedDetail);

        // ACT
        var actionResult = await _sut.GetData(id);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedDetail);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasGetQuery>(q => q.Id == id),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_GivenValidCommand_WhenExecuted_ThenSendsAktivasiBerkasSaveCommandAndReturnsOk()
    {
        // ARRANGE
        var cmd = new AktivasiBerkasSaveCommand(
            "2026-11-09 10:00:00",
            "USR01",
            "2026-11-09",
            "USR02",
            "LAY01",
            [new AktivasiBerkasRegisterRefDto("REG001")]
        );

        var expectedResult = AktivasiBerkasMutationResult.Ok("AKT001", "Sukses menyimpan transaksi aktivasi berkas.");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Save(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(400, "fail")]
    [InlineData(502, "error")]
    public async Task Save_WhenMutationFails_ReturnsNonOkStatusWithJSend(int statusCode, string expectedJsendStatus)
    {
        // ARRANGE
        var cmd = new AktivasiBerkasSaveCommand(
            "2026-11-10 07:04:04",
            "DR00139",
            "2026-11-10",
            "SPR001",
            "2RJ03",
            [new AktivasiBerkasRegisterRefDto("RG01123803")]
        );

        var failedResult = AktivasiBerkasMutationResult.Fail("EMR Save gagal: Userr invalid (HTTP 400)", statusCode);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // ACT
        var actionResult = await _sut.Save(cmd);

        // ASSERT
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(statusCode);
        var jsend = objectResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.status.Should().Be(expectedJsendStatus);
        jsend.data.Should().Be("EMR Save gagal: Userr invalid (HTTP 400)");
    }

    [Fact]
    public async Task Update_WhenMutationFails_ReturnsBadRequestWithJSendFail()
    {
        // ARRANGE
        const string id = "AKT001";
        var body = new AktivasiBerkasUpdateBody(
            "2026-11-10 07:04:04",
            "DR00139",
            "2026-11-10",
            "SPR001",
            "2RJ03",
            [new AktivasiBerkasRegisterRefDto("RG01123803")]
        );

        var failedResult = AktivasiBerkasMutationResult.Fail("EMR Update gagal: Userr invalid (HTTP 400)", 400);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasUpdateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // ACT
        var actionResult = await _sut.Update(id, body);

        // ASSERT
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        var jsend = objectResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.status.Should().Be("fail");
        jsend.data.Should().Be("EMR Update gagal: Userr invalid (HTTP 400)");
    }

    [Fact]
    public async Task Save_WhenAktifasiUserrIdEmpty_AndUserClaimsExist_ThenSetsAktifasiUserrIdFromUserIdentity()
    {
        // ARRANGE
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "USER_LOGIN_01")
        ], "mockAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new AktivasiBerkasSaveCommand(
            "2026-11-09 10:00:00",
            "USR01",
            "2026-11-09",
            "",
            "LAY01",
            [new AktivasiBerkasRegisterRefDto("REG001")]
        );

        AktivasiBerkasSaveCommand? captured = null;
        var expectedResult = AktivasiBerkasMutationResult.Ok("AKT002");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasSaveCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AktivasiBerkasMutationResult>, CancellationToken>((req, _) => captured = req as AktivasiBerkasSaveCommand)
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Save(cmd);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        captured.Should().NotBeNull();
        captured!.AktifasiUserrId.Should().Be("USER_LOGIN_01");
    }

    [Fact]
    public async Task Update_GivenValidBody_WhenExecuted_ThenConstructsUpdateCommandAndReturnsOk()
    {
        // ARRANGE
        const string id = "AKT001";
        var body = new AktivasiBerkasUpdateBody(
            "2026-11-09 11:00:00",
            "USR01",
            "2026-11-09",
            "USR02",
            "LAY01",
            [new AktivasiBerkasRegisterRefDto("REG001")]
        );

        var expectedResult = AktivasiBerkasMutationResult.Ok(id, "Sukses memperbarui transaksi aktivasi berkas.");

        AktivasiBerkasUpdateCommand? captured = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasUpdateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AktivasiBerkasMutationResult>, CancellationToken>((req, _) => captured = req as AktivasiBerkasUpdateCommand)
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Update(id, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        captured.Should().NotBeNull();
        captured!.Id.Should().Be(id);
        captured.TglJam.Should().Be(body.TglJam);
        captured.ReqUserrId.Should().Be(body.ReqUserrId);
        captured.TglAktif.Should().Be(body.TglAktif);
        captured.AktifasiUserrId.Should().Be(body.AktifasiUserrId);
        captured.LayananId.Should().Be(body.LayananId);
        captured.ListRegister.Should().BeEquivalentTo(body.ListRegister);

        _mediatorMock.Verify(x => x.Send(It.IsAny<AktivasiBerkasUpdateCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_WhenAktifasiUserrIdEmpty_AndUserClaimsExist_ThenSetsAktifasiUserrIdFromUserIdentity()
    {
        // ARRANGE
        const string id = "AKT001";
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "USER_LOGIN_02")
        ], "mockAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var body = new AktivasiBerkasUpdateBody(
            "2026-11-09 11:00:00",
            "USR01",
            "2026-11-09",
            "",
            "LAY01",
            [new AktivasiBerkasRegisterRefDto("REG001")]
        );

        AktivasiBerkasUpdateCommand? captured = null;
        var expectedResult = AktivasiBerkasMutationResult.Ok(id);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasUpdateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AktivasiBerkasMutationResult>, CancellationToken>((req, _) => captured = req as AktivasiBerkasUpdateCommand)
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Update(id, body);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        captured.Should().NotBeNull();
        captured!.AktifasiUserrId.Should().Be("USER_LOGIN_02");
    }

    [Fact]
    public async Task FindRegister_GivenKeyword_WhenExecuted_ThenSendsAktivasiBerkasFindRegisterQueryAndReturnsOk()
    {
        // ARRANGE
        const string keyword = "12345";
        var expectedRegisters = new List<AktivasiBerkasRegisterItemDto>
        {
            new()
            {
                RegId = "REG12345",
                KodeMr = "MR001",
                NamaPasien = "Budi Santoso",
                TglReg = "2026-11-01",
                JamReg = "08:30:00",
                KodeTipeJaminan = "J01",
                NamaTipeJaminan = "BPJS",
                TglVoid = "3000-01-01",
                LayananId = "LAY01",
                LayananName = "Poli Penyakit Dalam"
            }
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasFindRegisterQuery>(q => q.Keyword == keyword),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedRegisters);

        // ACT
        var actionResult = await _sut.FindRegister(keyword);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedRegisters);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasFindRegisterQuery>(q => q.Keyword == keyword),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListReaktif_GivenLayananId_WhenExecuted_ThenSendsAktivasiBerkasListReaktifQueryAndReturnsOk()
    {
        // ARRANGE
        const string layananId = "LAY01";
        var expectedList = new List<AktivasiBerkasListReaktifDto>
        {
            new("LAY01", "Poli Penyakit Dalam",
            [
                new AktivasiBerkasItemDetilDto
                {
                    AktifasiBerkasId = "AKT001",
                    RegId = "REG001",
                    PasienId = "PX001",
                    PasienName = "Budi Santoso"
                }
            ])
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasListReaktifQuery>(q => q.LayananId == layananId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var actionResult = await _sut.ListReaktif(layananId, CancellationToken.None);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedList);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasListReaktifQuery>(q => q.LayananId == layananId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListReaktif_GivenNullLayananId_WhenExecuted_ThenSendsAktivasiBerkasListReaktifQueryAndReturnsOk()
    {
        // ARRANGE
        var expectedList = new List<AktivasiBerkasListReaktifDto>();

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasListReaktifQuery>(q => q.LayananId == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedList);

        // ACT
        var actionResult = await _sut.ListReaktif(null, CancellationToken.None);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedList);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasListReaktifQuery>(q => q.LayananId == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_GivenValidBody_WhenExecuted_ThenSendsAktivasiBerkasDeactivateCommandAndReturnsOk()
    {
        // ARRANGE
        var body = new AktivasiBerkasDeactivateBody("REG001", "USR01");
        var expectedResult = AktivasiBerkasMutationResult.Ok("REG001", "Register berhasil di nonaktifkan");

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<AktivasiBerkasDeactivateCommand>(c => c.RegId == "REG001" && c.UserrId == "USR01"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Deactivate(body, CancellationToken.None);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        _mediatorMock.Verify(x => x.Send(
            It.Is<AktivasiBerkasDeactivateCommand>(c => c.RegId == "REG001" && c.UserrId == "USR01"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Deactivate_WhenUserrIdEmpty_AndUserClaimsExist_ThenSetsUserrIdFromUserIdentity()
    {
        // ARRANGE
        var user = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.Name, "USER_LOGIN_DEACT")
        ], "mockAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var body = new AktivasiBerkasDeactivateBody("REG002", "");
        AktivasiBerkasDeactivateCommand? captured = null;
        var expectedResult = AktivasiBerkasMutationResult.Ok("REG002");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasDeactivateCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<AktivasiBerkasMutationResult>, CancellationToken>((req, _) => captured = req as AktivasiBerkasDeactivateCommand)
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.Deactivate(body, CancellationToken.None);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        captured.Should().NotBeNull();
        captured!.RegId.Should().Be("REG002");
        captured.UserrId.Should().Be("USER_LOGIN_DEACT");
    }

    [Fact]
    public async Task Deactivate_WhenMutationFails_ReturnsBadRequestWithJSendFail()
    {
        // ARRANGE
        var body = new AktivasiBerkasDeactivateBody("REG003", "USR01");
        var failedResult = AktivasiBerkasMutationResult.Fail("Register tidak ditemukan atau gagal dinonaktifkan.", 400);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<AktivasiBerkasDeactivateCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // ACT
        var actionResult = await _sut.Deactivate(body, CancellationToken.None);

        // ASSERT
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(400);
        var jsend = objectResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.status.Should().Be("fail");
        jsend.data.Should().Be("Register tidak ditemukan atau gagal dinonaktifkan.");
    }

    [Fact]
    public async Task CleanUp_WhenExecuted_ThenSendsAktivasiBerkasCleanUpCommandAndReturnsOk()
    {
        // ARRANGE
        var expectedResult = AktivasiBerkasMutationResult.Ok(null, "Seluruh register aktif berhasil dibersihkan");

        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<AktivasiBerkasCleanUpCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResult);

        // ACT
        var actionResult = await _sut.CleanUp(CancellationToken.None);

        // ASSERT
        var okResult = actionResult.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResult);

        _mediatorMock.Verify(x => x.Send(
            It.IsAny<AktivasiBerkasCleanUpCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(400, "fail", "Pembersihan register gagal (HTTP 400)")]
    [InlineData(500, "error", "EMR CleanUp internal server error (HTTP 500)")]
    public async Task CleanUp_WhenMutationFails_ReturnsNonOkStatusWithJSend(int statusCode, string expectedJsendStatus, string message)
    {
        // ARRANGE
        var failedResult = AktivasiBerkasMutationResult.Fail(message, statusCode);

        _mediatorMock
            .Setup(x => x.Send(
                It.IsAny<AktivasiBerkasCleanUpCommand>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(failedResult);

        // ACT
        var actionResult = await _sut.CleanUp(CancellationToken.None);

        // ASSERT
        var objectResult = actionResult.Should().BeOfType<ObjectResult>().Subject;
        objectResult.StatusCode.Should().Be(statusCode);
        var jsend = objectResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.status.Should().Be(expectedJsendStatus);
        jsend.data.Should().Be(message);

        _mediatorMock.Verify(x => x.Send(
            It.IsAny<AktivasiBerkasCleanUpCommand>(),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
