using Bilreg.Api.Controllers.ChargeContext;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.Shared.Helpers.CommonValueObjects;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.TindakanFeature;

public class TindakanControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TindakanController _sut;

    public TindakanControllerTest()
    {
        _sut = new TindakanController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task Batal_WhenCommandSucceeds_Returns200OkWithDoneMessage()
    {
        // Arrange
        var req = new TindakanVoidRequest("TDK-01", "USER-01", "Pembatalan normal");
        _mediatorMock.Setup(x => x.Send(It.Is<TindakanVoidCmd>(c => c.TindakanId == "TDK-01"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Batal(req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");
    }

    [Fact]
    public async Task Batal_WhenInvalidOperationExceptionThrown_Returns400BadRequestWithJSendFailed()
    {
        // Arrange
        var req = new TindakanVoidRequest("TDK-01", "USER-01", "Pembatalan tagihan terkunci");
        _mediatorMock.Setup(x => x.Send(It.IsAny<TindakanVoidCmd>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Operasi bill hanya diizinkan saat TataRekening berstatus OPEN."));

        // Act
        var result = await _sut.Batal(req);

        // Assert
        var badRequestResult = result.Should().BeOfType<BadRequestObjectResult>().Subject;
        badRequestResult.StatusCode.Should().Be(StatusCodes.Status400BadRequest);
        var jsend = badRequestResult.Value.Should().BeOfType<JSendFailed>().Subject;
        jsend.status.Should().Be("failed");
    }

    [Fact]
    public async Task Create_WhenCalledWithQty_SendsCommandToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG-01", "LYN-01", "NT-01", "USER-01", [], Qty: 3);
        var expectedResponse = new TindakanCreateRespose("TDK-12345");
        _mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Create(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Create_WhenCalledWithIsDiskonDokter_SendsCommandToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new TdkCreateTindakanCmd("REG-01", "LYN-01", "NT-01", "USER-01", [], Qty: 2, IsDiskonDokter: true);
        var expectedResponse = new TindakanCreateRespose("TDK-12346");
        _mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Create(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SaveTindakan_WhenCalledWithQty_SendsCommandToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new TdkSaveTindakanCmd("TDK-12345", "REG-01", "LYN-01", "NT-01", "USER-01", [], Qty: 2);
        var expectedResponse = new TdkSaveTindakanRespose("TDK-12345");
        _mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.SaveTindakan(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task SaveTindakan_WhenCalledWithIsDiskonDokter_SendsCommandToMediatorAndReturnsOk()
    {
        // Arrange
        var cmd = new TdkSaveTindakanCmd("TDK-12345", "REG-01", "LYN-01", "NT-01", "USER-01", [], Qty: 2, IsDiskonDokter: true);
        var expectedResponse = new TdkSaveTindakanRespose("TDK-12345");
        _mediatorMock.Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.SaveTindakan(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Get_WhenCalledWithTindakanId_SendsQueryToMediatorAndReturnsOk()
    {
        // Arrange
        const string tindakanId = "TDK-001";
        var expectedResponse = new TindakanGetResponse(
            tindakanId,
            new DateTime(2026, 10, 16, 8, 30, 0),
            "ORD-01",
            new RegReff("REG-01", "MED-01", "Pasien A"),
            new LayananReff("LYN-01", "Poli Umum"),
            new KelasReff("KLS-01", "Kelas 1"),
            new TipeTarifReff("TT-01", "Umum"),
            new TarifReff("TRF-01", "Tarif A"),
            2,
            150000m,
            false,
            AuditTrailType.Default,
            []
        );

        _mediatorMock.Setup(x => x.Send(It.Is<TindakanGetQuery>(q => q.TindakanId == tindakanId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Get(tindakanId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Get_WhenTindakanHasDiskonDokter_SendsQueryToMediatorAndReturnsOkWithDiskonDokter()
    {
        // Arrange
        const string tindakanId = "TDK-002";
        var expectedResponse = new TindakanGetResponse(
            tindakanId,
            new DateTime(2026, 10, 16, 8, 30, 0),
            "ORD-02",
            new RegReff("REG-01", "MED-01", "Pasien A"),
            new LayananReff("LYN-01", "Poli Umum"),
            new KelasReff("KLS-01", "Kelas 1"),
            new TipeTarifReff("TT-01", "Umum"),
            new TarifReff("TRF-01", "Tarif A"),
            2,
            0m,
            false,
            AuditTrailType.Default,
            [new TindakanGetKomponenResponse("KOMP-01", "Jasa Dokter", 100000m, 2, 0m, "PPA-01", "dr. Smith", Diskon: 200000m)],
            IsDiskonDokter: true,
            DiskonDokter: 200000m
        );

        _mediatorMock.Setup(x => x.Send(It.Is<TindakanGetQuery>(q => q.TindakanId == tindakanId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Get(tindakanId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);
    }
}
