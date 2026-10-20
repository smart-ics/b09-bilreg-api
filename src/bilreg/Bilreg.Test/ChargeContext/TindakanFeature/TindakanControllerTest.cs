using System.Net;
using Bilreg.Api.Controllers.ChargeContext;
using Bilreg.Application.ChargeContext.TindakanFeature.UseCases;
using Bilreg.Domain.AdmisiContext.LayananFeature;
using Bilreg.Domain.AdmisiContext.RegFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.ChargeContext.TarifFeature;
using Bilreg.Domain.ChargeContext.TindakanFeature;
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
        _sut = new TindakanController(_mediatorMock.Object);
    }

    [Fact]
    public async Task Create_DelegatesToTdkCreateTindakanCmd_ReturnsOk()
    {
        // Arrange
        var ppaList = new List<TdkCreateTindakanPpaCmd> { new("KOMP01", "PPA01") };
        var cmd = new TdkCreateTindakanCmd("REG001", "LAY01", "NT001", "user1", ppaList, "BED001", 3);
        var expectedResponse = new TindakanCreateRespose("TDK001");

        TdkCreateTindakanCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TdkCreateTindakanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TindakanCreateRespose>, CancellationToken>((c, _) => capturedCmd = (TdkCreateTindakanCmd)c)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Create(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.RegId.Should().Be("REG001");
        capturedCmd.LayananId.Should().Be("LAY01");
        capturedCmd.NilaiTarifId.Should().Be("NT001");
        capturedCmd.UserId.Should().Be("user1");
        capturedCmd.PakaiBedId.Should().Be("BED001");
        capturedCmd.Qty.Should().Be(3);
        capturedCmd.ListPpa.Should().HaveCount(1);
    }

    [Fact]
    public async Task SaveTindakan_DelegatesToTdkSaveTindakanCmd_ReturnsOk()
    {
        // Arrange
        var ppaList = new List<TdkSaveTindakanPpaCmd> { new("KOMP01", "PPA01") };
        var cmd = new TdkSaveTindakanCmd("TDK001", "REG001", "LAY01", "NT001", "user1", ppaList, "BED001", 2);
        var expectedResponse = new TdkSaveTindakanRespose("TDK001");

        TdkSaveTindakanCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TdkSaveTindakanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TdkSaveTindakanRespose>, CancellationToken>((c, _) => capturedCmd = (TdkSaveTindakanCmd)c)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.SaveTindakan(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TindakanId.Should().Be("TDK001");
        capturedCmd.RegId.Should().Be("REG001");
        capturedCmd.PakaiBedId.Should().Be("BED001");
        capturedCmd.Qty.Should().Be(2);
    }

    [Fact]
    public async Task Get_DelegatesToTindakanGetQuery_ReturnsOk()
    {
        // Arrange
        var expectedResponse = new TindakanGetResponse(
            "TDK001",
            "2026-10-20 10:00:00",
            "BED001",
            2,
            100000m,
            new RegReff("REG001", "PAS001", "Pasien A"),
            new LayananReff("LAY01", "Layanan A"),
            new KelasReff("KL01", "Kelas 1"),
            new TipeTarifReff("TT01", "Umum"),
            new TarifReff("TRF01", "Tarif Tindakan"),
            new List<TindakanKomponenBase>(),
            AuditTrailType.Create("user1", DateTime.Now),
            "ORD001");

        TindakanGetQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TindakanGetQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<TindakanGetResponse>, CancellationToken>((q, _) => capturedQuery = (TindakanGetQuery)q)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Get("TDK001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.TindakanId.Should().Be("TDK001");
    }

    [Fact]
    public async Task List_WithoutPakaiBedId_DelegatesToTindakanListQuery_ReturnsOk()
    {
        // Arrange
        var listResponse = new List<TindakanListResponse>
        {
            new("TDK001", "2026-10-20 10:00:00", "ORD01",
                new RegReff("REG001", "PAS001", "Pasien A"),
                new LayananReff("LAY01", "Layanan A"),
                new TarifReff("TRF01", "Tarif Tindakan"),
                "BED001")
        };

        TindakanListQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TindakanListQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<TindakanListResponse>>, CancellationToken>((q, _) => capturedQuery = (TindakanListQuery)q)
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.List("REG001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(listResponse);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RegId.Should().Be("REG001");
        capturedQuery.PakaiBedId.Should().Be("");
    }

    [Fact]
    public async Task List_WithPakaiBedId_DelegatesToTindakanListQuery_ReturnsOk()
    {
        // Arrange
        var listResponse = new List<TindakanListResponse>
        {
            new("TDK001", "2026-10-20 10:00:00", "ORD01",
                new RegReff("REG001", "PAS001", "Pasien A"),
                new LayananReff("LAY01", "Layanan A"),
                new TarifReff("TRF01", "Tarif Tindakan"),
                "BED001")
        };

        TindakanListQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TindakanListQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<TindakanListResponse>>, CancellationToken>((q, _) => capturedQuery = (TindakanListQuery)q)
            .ReturnsAsync(listResponse);

        // Act
        var result = await _sut.List("REG001", "BED001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(listResponse);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RegId.Should().Be("REG001");
        capturedQuery.PakaiBedId.Should().Be("BED001");
    }

    [Fact]
    public async Task ListTdkJual_DelegatesToTdkListTindakanJualQuery_ReturnsOk()
    {
        // Arrange
        var response = new List<TdkListTindakanJualResponse>
        {
            new("TDK001", "2026-10-20 10:00:00",
                new RegReff("REG001", "PAS001", "Pasien A"),
                new LayananReff("LAY01", "Layanan A"),
                new TdkListTdkJualDesc("TRF01", "Tarif Tindakan", "TARIF"),
                150000m,
                3,
                "BED001")
        };

        TdkListTindakanJualQuery? capturedQuery = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TdkListTindakanJualQuery>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IEnumerable<TdkListTindakanJualResponse>>, CancellationToken>((q, _) => capturedQuery = (TdkListTindakanJualQuery)q)
            .ReturnsAsync(response);

        // Act
        var result = await _sut.ListTdkJual("REG001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(response);

        capturedQuery.Should().NotBeNull();
        capturedQuery!.RegId.Should().Be("REG001");
    }

    [Fact]
    public async Task Batal_WhenSuccessful_DelegatesToTindakanVoidCmd_ReturnsOk()
    {
        // Arrange
        var httpContext = new DefaultHttpContext();
        httpContext.Connection.RemoteIpAddress = IPAddress.Parse("127.0.0.1");
        httpContext.Request.Headers.UserAgent = "TestAgent/1.0";
        _sut.ControllerContext = new ControllerContext { HttpContext = httpContext };

        var req = new TindakanVoidRequest("TDK001", "user1", "Salah input pasien");

        TindakanVoidCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TindakanVoidCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest, CancellationToken>((c, _) => capturedCmd = (TindakanVoidCmd)c)
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Batal(req);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be("Done");

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TindakanId.Should().Be("TDK001");
        capturedCmd.UserId.Should().Be("user1");
        capturedCmd.VoidReason.Should().Be("Salah input pasien");
        capturedCmd.ClientIpAddress.Should().Be("127.0.0.1");
        capturedCmd.UserAgent.Should().Be("TestAgent/1.0");
    }

    [Fact]
    public async Task Batal_WhenBillingLockedOrClosed_CatchesInvalidOperationException_Returns422UnprocessableEntity()
    {
        // Arrange
        var req = new TindakanVoidRequest("TDK001", "user1", "Salah input pasien");
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<TindakanVoidCmd>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Tagihan tidak dapat dihapus. Billing sudah lunas"));

        // Act
        var result = await _sut.Batal(req);

        // Assert
        var objResult = result.Should().BeOfType<ObjectResult>().Subject;
        objResult.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);

        var jsend = objResult.Value.Should().BeOfType<JSend>().Subject;
        jsend.code.Should().Be("422");
        jsend.status.Should().Be("FAIL");
        jsend.data.ToString().Should().Contain("Billing sudah lunas");
    }
}
