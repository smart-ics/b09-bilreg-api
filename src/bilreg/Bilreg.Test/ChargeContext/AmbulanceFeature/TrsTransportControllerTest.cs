using Bilreg.Api.Controllers.BillContext.TransportSub;
using Bilreg.Application.ChargeContext.AmbulanceFeature.UseCases;
using Bilreg.Domain.ChargeContext.AmbulanceFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.ChargeContext.AmbulanceFeature;

public class TrsTransportControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly TrsTransportController _sut;

    public TrsTransportControllerTest()
    {
        _sut = new TrsTransportController(_mediatorMock.Object)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }

    [Fact]
    public async Task PreviewTarif_SendsQueryToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var expected = new PreviewTransportTariffResponse(
            "AMB01",
            "TUJ01",
            3m,
            20000m,
            370000m,
            [new PreviewTransportTariffItemResponse("K-TETAP", "Administrasi", true, 50000m)]);

        _mediatorMock
            .Setup(x => x.Send(new PreviewTransportTariffQuery("AMB01", "TUJ01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.PreviewTarif("AMB01", "TUJ01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expected);
    }

    [Fact]
    public async Task RequestTransport_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new RequestTransportCmd(
            TipePerjalanan: TransportTypeEnum.OutboundReferral,
            MetodeTarif: ChargingMethodEnum.Wilayah,
            LayananId: "LYN01",
            PetugasId: "USR01",
            RegId: "REG01",
            TujuanTransportId: "TUJ01",
            Konstanta: 150000m,
            LokasiAsal: "IGD",
            LokasiTujuan: "RSUP Dr. Sardjito",
            JaminanId: "JAM01",
            Keterangan: "Rujukan darurat");

        var expected = new RequestTransportResponse("TP26110001", "Requested");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.RequestTransport(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WithBodyOnly_SendsCommandToMediator_ReturnsOk()
    {
        // Arrange
        var cmd = new DispatchTransportCmd("TP26110001", "AMB01", "DRV01", 1000m, "B 1234 CD");
        var expected = new DispatchTransportResponse("TP26110001", "Dispatched", DateTime.Now);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Dispatch(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Dispatch_WithRouteId_PopulatesTrsTransportIdAndSends_ReturnsOk()
    {
        // Arrange
        var cmd = new DispatchTransportCmd("", "AMB01", "DRV01", 1000m);
        var expected = new DispatchTransportResponse("TP26110001", "Dispatched", DateTime.Now);

        DispatchTransportCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<DispatchTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<DispatchTransportResponse>, CancellationToken>((c, _) => capturedCmd = (DispatchTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Dispatch(cmd, "TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
    }

    [Fact]
    public async Task LinkReg_WithRouteId_PopulatesTrsTransportIdAndSends_ReturnsOk()
    {
        // Arrange
        var cmd = new LinkRegTransportCmd("", "REG002");
        var expected = new LinkRegTransportResponse("TP26110001", "REG002");

        LinkRegTransportCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<LinkRegTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<LinkRegTransportResponse>, CancellationToken>((c, _) => capturedCmd = (LinkRegTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.LinkReg(cmd, "TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        capturedCmd.RegId.Should().Be("REG002");
    }

    [Fact]
    public async Task Complete_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new CompleteTransportCmd("TP26110001", 1050m);
        var expected = new CompleteTransportResponse("TP26110001", "Completed", 250000m, true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Complete(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Complete_WithRouteId_PopulatesTrsTransportId_ReturnsOk()
    {
        // Arrange
        var cmd = new CompleteTransportCmd("", 1050m);
        var expected = new CompleteTransportResponse("TP26110001", "Completed", 250000m, true);

        CompleteTransportCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CompleteTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CompleteTransportResponse>, CancellationToken>((c, _) => capturedCmd = (CompleteTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Complete(cmd, "TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        capturedCmd.EndOdometer.Should().Be(1050m);
    }

    [Fact]
    public async Task Cancel_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new CancelTransportCmd("TP26110001", "Pasien membatalkan", 50000m, "USR01");
        var expected = new CancelTransportResponse("TP26110001", "Cancelled", 50000m, true);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Cancel(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Cancel_WithRouteId_PopulatesTrsTransportId_ReturnsOk()
    {
        // Arrange
        var cmd = new CancelTransportCmd("", "Pasien menolak dirujuk", 50000m);
        var expected = new CancelTransportResponse("TP26110001", "Cancelled", 50000m, true);

        CancelTransportCmd? capturedCmd = null;
        _mediatorMock
            .Setup(x => x.Send(It.IsAny<CancelTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<CancelTransportResponse>, CancellationToken>((c, _) => capturedCmd = (CancelTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Cancel(cmd, "TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        capturedCmd.AlasanBatal.Should().Be("Pasien menolak dirujuk");
    }

    [Fact]
    public void ChargeBilling_HasExpectedHttpAttributes()
    {
        // Act
        var method = typeof(TrsTransportController).GetMethod(nameof(TrsTransportController.ChargeBilling));

        // Assert
        method.Should().NotBeNull();
        var httpPostAttrs = method!.GetCustomAttributes(typeof(HttpPostAttribute), false).Cast<HttpPostAttribute>().ToList();
        httpPostAttrs.Should().Contain(a => a.Template == "{id}/charge-billing");
        httpPostAttrs.Should().Contain(a => a.Template == "charge-billing");
    }

    [Fact]
    public async Task ChargeBilling_WithRouteIdOnly_SendsCommandToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var expected = new ChargeTransportResponse("TP26110001", "Completed", 250000m, true);
        ChargeTransportCmd? capturedCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChargeTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ChargeTransportResponse>, CancellationToken>((c, _) => capturedCmd = (ChargeTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ChargeBilling("TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        _mediatorMock.Verify(x => x.Send(It.IsAny<ChargeTransportCmd>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChargeBilling_WithRouteIdAndBody_OverridesTrsTransportId_SendsCommandToMediator_ReturnsOk()
    {
        // Arrange
        var cmd = new ChargeTransportCmd("", "USR01");
        var expected = new ChargeTransportResponse("TP26110001", "Requested", 150000m, true);
        ChargeTransportCmd? capturedCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChargeTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ChargeTransportResponse>, CancellationToken>((c, _) => capturedCmd = (ChargeTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ChargeBilling("TP26110001", cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        capturedCmd.PetugasId.Should().Be("USR01");
    }

    [Fact]
    public async Task ChargeBilling_WithBodyOnly_SendsCommandToMediator_ReturnsOk()
    {
        // Arrange
        var cmd = new ChargeTransportCmd("TP26110001", "USR02");
        var expected = new ChargeTransportResponse("TP26110001", "Dispatched", 300000m, true);
        ChargeTransportCmd? capturedCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ChargeTransportCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<ChargeTransportResponse>, CancellationToken>((c, _) => capturedCmd = (ChargeTransportCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ChargeBilling(null, cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP26110001");
        capturedCmd.PetugasId.Should().Be("USR02");
    }

    [Fact]
    public async Task Get_SendsQueryToMediator_ReturnsOkWithResponse()
    {
        // Arrange
        var expected = new TransportGetResponse(
            TrsTransportId: "TP26110001",
            RegId: "REG01",
            LayananId: "LYN01",
            TglTrs: DateTime.Today,
            PetugasId: "USR01",
            Status: "Dispatched",
            TipePerjalanan: "OutboundReferral",
            MetodeTarif: "Wilayah",
            AmbulanceId: "AMB01",
            DriverId: "DRV01",
            NoPolisi: "B 1234 CD",
            PerawatId: "PRW01",
            DokterId: "DOC01",
            LokasiAsal: "IGD",
            LokasiTujuan: "RS Lain",
            TujuanTransportId: "TUJ01",
            Konstanta: 150000m,
            TglRequest: DateTime.Today,
            TglDispatch: DateTime.Today.AddMinutes(10),
            TglArrival: null,
            TglReturn: null,
            StartOdometer: 1000m,
            EndOdometer: 0m,
            TotalJarakKm: 0m,
            SubTotal: 150000m,
            Diskon: 0m,
            Total: 150000m,
            JaminanId: "JAM01",
            IsNonReg: false,
            IsPostedBilling: false,
            AlasanBatal: "",
            BiayaBatal: 0m,
            Keterangan: "",
            ListItems: []);

        _mediatorMock
            .Setup(x => x.Send(It.Is<GetTransportQuery>(q => q.TrsTransportId == "TP26110001"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.Get("TP26110001");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        _mediatorMock.Verify(x => x.Send(It.Is<GetTransportQuery>(q => q.TrsTransportId == "TP26110001"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListByReg_SendsQueryToMediator_ReturnsOkWithList()
    {
        // Arrange
        var expected = new List<TransportGetResponse>
        {
            new(
                TrsTransportId: "TP26110001",
                RegId: "REG01",
                LayananId: "LYN01",
                TglTrs: DateTime.Today,
                PetugasId: "USR01",
                Status: "Completed",
                TipePerjalanan: "OutboundReferral",
                MetodeTarif: "Wilayah",
                AmbulanceId: "AMB01",
                DriverId: "DRV01",
                NoPolisi: "B 1234 CD",
                PerawatId: "",
                DokterId: "",
                LokasiAsal: "IGD",
                LokasiTujuan: "RS Lain",
                TujuanTransportId: "TUJ01",
                Konstanta: 150000m,
                TglRequest: DateTime.Today,
                TglDispatch: DateTime.Today.AddMinutes(10),
                TglArrival: null,
                TglReturn: DateTime.Today.AddHours(1),
                StartOdometer: 1000m,
                EndOdometer: 1025m,
                TotalJarakKm: 25m,
                SubTotal: 150000m,
                Diskon: 0m,
                Total: 150000m,
                JaminanId: "JAM01",
                IsNonReg: false,
                IsPostedBilling: true,
                AlasanBatal: "",
                BiayaBatal: 0m,
                Keterangan: "",
                ListItems: [])
        };

        _mediatorMock
            .Setup(x => x.Send(It.Is<ListTransportByRegQuery>(q => q.RegId == "REG01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ListByReg("REG01");

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(x => x.Send(It.Is<ListTransportByRegQuery>(q => q.RegId == "REG01"), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ListActiveDispatch_SendsQueryToMediator_ReturnsOkWithList()
    {
        // Arrange
        var expected = new List<TransportGetResponse>
        {
            new(
                TrsTransportId: "TP26110001",
                RegId: "REG01",
                LayananId: "LYN01",
                TglTrs: DateTime.Today,
                PetugasId: "USR01",
                Status: "Dispatched",
                TipePerjalanan: "OutboundReferral",
                MetodeTarif: "Wilayah",
                AmbulanceId: "AMB01",
                DriverId: "DRV01",
                NoPolisi: "B 1234 CD",
                PerawatId: "",
                DokterId: "",
                LokasiAsal: "IGD",
                LokasiTujuan: "RS Lain",
                TujuanTransportId: "TUJ01",
                Konstanta: 150000m,
                TglRequest: DateTime.Today,
                TglDispatch: DateTime.Today.AddMinutes(10),
                TglArrival: null,
                TglReturn: null,
                StartOdometer: 1000m,
                EndOdometer: 0m,
                TotalJarakKm: 0m,
                SubTotal: 150000m,
                Diskon: 0m,
                Total: 150000m,
                JaminanId: "JAM01",
                IsNonReg: false,
                IsPostedBilling: false,
                AlasanBatal: "",
                BiayaBatal: 0m,
                Keterangan: "",
                ListItems: [])
        };

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<ListActiveDispatchQuery>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ListActiveDispatch();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expected);

        _mediatorMock.Verify(x => x.Send(It.IsAny<ListActiveDispatchQuery>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateDetail_SendsCommandWithRouteId_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new UpdateTransportDetailCmd(
            "IGNORED",
            DriverId: "DRV-1",
            PerawatId: "PRW-1",
            DokterId: "DOK-1",
            Items: [new TransportItemAdjustmentDto("K-VAR", 80000m, 5000m)]);
        var expected = new UpdateTransportDetailResponse("TP-001", "DRV-1", "PRW-1", "DOK-1", 80000m, 5000m, 75000m);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<UpdateTransportDetailCmd>(c =>
                    c.TrsTransportId == "TP-001" &&
                    c.DriverId == "DRV-1" &&
                    c.PerawatId == "PRW-1" &&
                    c.DokterId == "DOK-1"),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.UpdateDetail("TP-001", cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);
    }

    [Fact]
    public async Task UpdateDetail_WithoutBodyTrsTransportId_PopulatesFromRouteId_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new UpdateTransportDetailCmd(
            DriverId: "DRV-1",
            PerawatId: "PRW-1",
            DokterId: "DOK-1",
            Items: [new TransportItemAdjustmentDto("K-VAR", 80000m, 5000m)]);
        cmd.TrsTransportId.Should().BeEmpty();

        var expected = new UpdateTransportDetailResponse("TP-001", "DRV-1", "PRW-1", "DOK-1", 80000m, 5000m, 75000m);
        UpdateTransportDetailCmd? capturedCmd = null;

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<UpdateTransportDetailCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<UpdateTransportDetailResponse>, CancellationToken>((c, _) => capturedCmd = (UpdateTransportDetailCmd)c)
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.UpdateDetail("TP-001", cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.TrsTransportId.Should().Be("TP-001");
        capturedCmd.DriverId.Should().Be("DRV-1");
        capturedCmd.PerawatId.Should().Be("PRW-1");
        capturedCmd.DokterId.Should().Be("DOK-1");
    }

    [Fact]
    public async Task ChargeBilling_ForwardsCrewAndItems_ReturnsOkWithResponse()
    {
        // Arrange
        var cmd = new ChargeTransportCmd(
            "",
            PetugasId: "PTG-1",
            DriverId: "DRV-1",
            PerawatId: "PRW-1",
            DokterId: "DOK-1",
            Items: [new TransportItemAdjustmentDto("K-VAR", 80000m, 5000m)]);
        var expected = new ChargeTransportResponse("TP-001", "Completed", 75000m, true);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<ChargeTransportCmd>(c =>
                    c.TrsTransportId == "TP-001" &&
                    c.DriverId == "DRV-1" &&
                    c.PerawatId == "PRW-1" &&
                    c.DokterId == "DOK-1" &&
                    c.Items != null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expected);

        // Act
        var result = await _sut.ChargeBilling("TP-001", cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expected);
    }
}
