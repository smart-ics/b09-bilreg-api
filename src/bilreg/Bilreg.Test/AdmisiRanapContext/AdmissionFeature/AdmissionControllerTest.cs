using Bilreg.Api.Controllers.AdmisiRanapContext;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Application.AdmisiRanapContext.AdmissionFeature.UseCases;
using Bilreg.Domain.AdmisiRanapContext.AdmissionFeature;
using Bilreg.Domain.BedUsageContext.WardFeature;
using Bilreg.Domain.PasienContext.PasienFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.AdmisiRanapContext.AdmissionFeature;

public class AdmissionControllerTest
{
    private readonly Mock<IMediator> _mediatorMock;
    private readonly AdmissionController _sut;

    public AdmissionControllerTest()
    {
        _mediatorMock = new Mock<IMediator>();
        _sut = new AdmissionController(_mediatorMock.Object);
    }

    private static AdmissionRegistrationData CreateSampleRegistration() => new(
        TipeJaminanId: "TJ01",
        CaraMasukDkId: "8",
        ProsedurMasukInapId: "1",
        RujukanId: "RUJ01",
        DokterId: "DOK01",
        PesertaJaminanId: "PJ01",
        JenisInapId: "1",
        RegIbuId: null);

    [Fact]
    public async Task ProcessFromDirect_SendsCmdAndReturnsJSendOk()
    {
        // Arrange
        var registration = CreateSampleRegistration();
        var body = new AdmProcessDirectAdmissionBody(
            PasienId: "PAS001",
            KelasDkId: "KDK01",
            BangsalId: "B01",
            UserId: "USER01",
            Registration: registration,
            KelasId: "K01",
            Priority: 2);

        var expectedResponse = new AdmProcessAdmissionResponse("REG001", AdmissionStatusEnum.Admitted, "WL001");

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmProcessDirectAdmissionCmd>(cmd =>
                cmd.PasienId == "PAS001" &&
                cmd.KelasDkId == "KDK01" &&
                cmd.BangsalId == "B01" &&
                cmd.UserId == "USER01" &&
                cmd.Registration == registration &&
                cmd.KelasId == "K01" &&
                cmd.Priority == 2), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.ProcessFromDirect(body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(m => m.Send(It.Is<AdmProcessDirectAdmissionCmd>(cmd =>
            cmd.PasienId == "PAS001" &&
            cmd.KelasDkId == "KDK01" &&
            cmd.BangsalId == "B01" &&
            cmd.UserId == "USER01" &&
            cmd.Registration == registration &&
            cmd.KelasId == "K01" &&
            cmd.Priority == 2), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessFromDirect_WithDefaultParameters_SendsCmdWithDefaults()
    {
        // Arrange
        var registration = CreateSampleRegistration();
        var body = new AdmProcessDirectAdmissionBody(
            PasienId: "PAS002",
            KelasDkId: "KDK02",
            BangsalId: "B02",
            UserId: "USER02",
            Registration: registration);

        var expectedResponse = new AdmProcessAdmissionResponse("REG002", AdmissionStatusEnum.Admitted, "WL002");

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmProcessDirectAdmissionCmd>(cmd =>
                cmd.PasienId == "PAS002" &&
                cmd.KelasDkId == "KDK02" &&
                cmd.BangsalId == "B02" &&
                cmd.UserId == "USER02" &&
                cmd.Registration == registration &&
                cmd.KelasId == null &&
                cmd.Priority == 3), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.ProcessFromDirect(body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task ProcessFromOpnameRequest_SendsCmdAndReturnsJSendOk()
    {
        // Arrange
        var registration = CreateSampleRegistration();
        var body = new AdmProcessOpnameRequestBody("OPN01", "KDK01", "B01", "USER01", registration);
        var expectedResponse = new AdmProcessAdmissionResponse("REG001", AdmissionStatusEnum.Admitted, "WL001");

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmProcessOpnameRequestCmd>(cmd =>
                cmd.OpnameRequestId == "OPN01" &&
                cmd.KelasDkId == "KDK01" &&
                cmd.BangsalId == "B01" &&
                cmd.UserId == "USER01" &&
                cmd.Registration == registration), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.ProcessFromOpnameRequest(body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task ProcessFromReservation_SendsCmdAndReturnsJSendOk()
    {
        // Arrange
        var registration = CreateSampleRegistration();
        var body = new AdmProcessReservationBody("RSV01", "KDK01", "B01", "USER01", registration);
        var expectedResponse = new AdmProcessAdmissionResponse("REG001", AdmissionStatusEnum.Admitted, "WL001");

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmProcessReservationCmd>(cmd =>
                cmd.ReservationId == "RSV01" &&
                cmd.KelasDkId == "KDK01" &&
                cmd.BangsalId == "B01" &&
                cmd.UserId == "USER01" &&
                cmd.Registration == registration), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.ProcessFromReservation(body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);
    }

    [Fact]
    public async Task Update_SendsCmdAndReturnsJSendOkDone()
    {
        // Arrange
        var body = new AdmUpdateAdmissionBody("KDK02", "B02", "USER02");
        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmUpdateAdmissionCmd>(cmd =>
                cmd.RegId == "ADM01" &&
                cmd.KelasDkId == "KDK02" &&
                cmd.BangsalId == "B02" &&
                cmd.UserId == "USER02"), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Update("ADM01", body);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");
    }

    [Fact]
    public async Task Get_SendsQryAndReturnsJSendOk()
    {
        // Arrange
        var response = new AdmGetAdmissionResponse(
            "ADM01",
            AdmissionStatusEnum.Admitted,
            new PasienReff("PAS01", "Pasien A", new DateOnly(1990, 1, 1), "L"),
            "-",
            "-",
            new KelasDkType("KDK01", "Kelas 1"),
            new BangsalReff("B01", "Bangsal A"),
            DateTime.UtcNow,
            DateTime.UtcNow,
            "8",
            "DOK01",
            "1",
            "3",
            "REG_IBU_01");

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmGetAdmissionQry>(q => q.RegId == "ADM01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.Get("ADM01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(response);
        var resData = jsend.data.Should().BeOfType<AdmGetAdmissionResponse>().Subject;
        resData.JenisInapId.Should().Be("3");
        resData.RegIbu.Should().Be("REG_IBU_01");
    }

    [Fact]
    public async Task Lookup_SendsQryAndReturnsJSendOk()
    {
        // Arrange
        var items = new List<AdmAdmissionListItem>
        {
            new("ADM01", AdmissionStatusEnum.Admitted, "PAS01", "Pasien A", "KDK01", "Kelas 1", "B01", "Bangsal A", DateTime.UtcNow)
        };
        var response = new AdmLookupAdmissionResponse(items);

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmLookupAdmissionQry>(q => q.Status == AdmissionStatusEnum.Admitted && q.PasienId == "PAS01" && q.Search == null), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.Lookup(AdmissionStatusEnum.Admitted, "PAS01", null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(response);
    }

    [Fact]
    public async Task Lookup_WithSearch_SendsQryWithSearchAndReturnsJSendOk()
    {
        // Arrange
        var items = new List<AdmAdmissionListItem>
        {
            new("ADM01", AdmissionStatusEnum.Admitted, "PAS01", "Pasien Ibu Siti", "KDK01", "Kelas 1", "B01", "Bangsal A", DateTime.UtcNow)
        };
        var response = new AdmLookupAdmissionResponse(items);

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmLookupAdmissionQry>(q =>
                q.Status == AdmissionStatusEnum.Admitted &&
                q.PasienId == null &&
                q.Search == "Siti"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.Lookup(AdmissionStatusEnum.Admitted, null, "Siti");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(response);

        _mediatorMock.Verify(m => m.Send(It.Is<AdmLookupAdmissionQry>(q =>
            q.Status == AdmissionStatusEnum.Admitted &&
            q.PasienId == null &&
            q.Search == "Siti"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListEligibleBangsal_SendsQryAndReturnsJSendOk()
    {
        // Arrange
        var items = new List<AdmEligibleBangsalItem>
        {
            new("B01", "Bangsal Mawar")
        };
        var response = new AdmListEligibleBangsalResponse(items);

        _mediatorMock
            .Setup(m => m.Send(It.Is<AdmListEligibleBangsalQry>(q => q.KelasDkId == "KDK01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(response);

        // Act
        var result = await _sut.ListEligibleBangsal("KDK01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(response);
    }
}
