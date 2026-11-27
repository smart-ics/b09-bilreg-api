using System.Reflection;
using Bilreg.Api.Controllers.RadContext;
using Bilreg.Application.RadContext.RadOrderFeature;
using Bilreg.Application.RadContext.RadOrderFeature.UseCases;
using Bilreg.Domain.RadContext.RadOrderFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.RadContext.RadOrderFeature;

public class RadOrderControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly RadOrderController _sut;

    public RadOrderControllerTest()
    {
        _sut = new RadOrderController(_mediatorMock.Object);
    }

    #region Controller Attribute & Route Structure Tests

    [Fact]
    public void Controller_HasExpectedAttributesAndRoutes()
    {
        var type = typeof(RadOrderController);

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttrs = type.GetCustomAttributes<RouteAttribute>().ToList();
        routeAttrs.Should().NotBeEmpty();
        routeAttrs.Select(r => r.Template).Should().Contain(["api/rad/order", "api/RadContext/RadOrderFeature"]);
    }

    #endregion

    #region CreateFromEmr Tests

    [Fact]
    public async Task CreateFromEmr_WhenSuccess_Returns200OkWithJSendOk()
    {
        // Arrange
        var cmd = new RadOrderCreateFromEmrCmd(
            UserId: "USER01",
            EmrOrderId: "EMR-RAD-001",
            RegId: "REG001",
            PatientId: "PAS001",
            PatientName: "Pasien Radiologi",
            BirthDateYmd: "1990-01-01",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF001", 0)],
            DokterPengorderId: "DOK001",
            DokterPengorderName: "dr. Radiolog",
            LayananPengorderId: "LAY001",
            LayananPengorderName: "Rawat Jalan",
            ClinicalNote: "Foto Thorax",
            Urgency: 0,
            AllowDuplicateConfirmation: false);

        var expectedResponse = new RadOrderCreateResponse(
            OrderId: "RDO2611000001",
            OrderNo: "RAD2611000001",
            EmrOrderId: "EMR-RAD-001",
            RadOrderStatus: 1,
            Modality: "CR",
            ItemCount: 1,
            IsCito: false);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateFromEmr(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateFromEmr_WhenDuplicateRadOrderWarningException_Returns409ConflictWithJSendFail()
    {
        // Arrange
        var cmd = new RadOrderCreateFromEmrCmd(
            UserId: "USER01",
            EmrOrderId: "EMR-RAD-002",
            RegId: "REG001",
            PatientId: "PAS001",
            PatientName: "Pasien Radiologi",
            BirthDateYmd: "1990-01-01",
            Gender: "L",
            Modality: "CR",
            Items: [new RadOrderItemInputDto("TRF001", 0)],
            AllowDuplicateConfirmation: false);

        var duplicateEx = new DuplicateRadOrderWarningException(
            patientId: "PAS001",
            modality: "CR",
            duplicateTarifIds: ["TRF001"],
            message: "EX-06: Peringatan duplikasi pemeriksaan radiologi dalam 24 jam terakhir.",
            existingOrderNos: ["RAD2611000001"]);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ThrowsAsync(duplicateEx);

        // Act
        var result = await _sut.CreateFromEmr(cmd);

        // Assert
        var conflictResult = result.Should().BeOfType<ConflictObjectResult>().Subject;
        conflictResult.StatusCode.Should().Be(409);

        var jsend = conflictResult.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.status.Should().Be("fail");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancel Tests

    [Fact]
    public async Task Cancel_WhenSuccess_Returns200OkWithJSendOk()
    {
        // Arrange
        const string orderId = "RDO2611000001";
        var request = new CancelRadOrderRequest("USER01", "Permintaan dokter batal");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<RadOrderCancelCmd>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Cancel(orderId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderCancelCmd>(c => c.OrderId == orderId && c.UserId == "USER01" && c.Reason == "Permintaan dokter batal"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Charge Tests

    [Fact]
    public async Task Charge_WhenRouteIdProvided_UsesRouteIdAndReturns200OkWithJSendOk()
    {
        // Arrange
        const string orderId = "RDO2611000001";
        var request = new ChargeRadOrderRequest("USER01");
        var expectedResponse = new RadOrderChargeResponse(true, "TND261100001", null);

        _mediatorMock
            .Setup(x => x.Send(It.Is<RadOrderChargeCmd>(c => c.OrderId == orderId && c.UserId == "USER01"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Charge(orderId, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderChargeCmd>(c => c.OrderId == orderId && c.UserId == "USER01"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Charge_WhenRouteIdEmpty_UsesBodyOrderIdAndReturns200OkWithJSendOk()
    {
        // Arrange
        const string orderIdFromBody = "RDO2611000002";
        var request = new ChargeRadOrderRequest("USER02", orderIdFromBody);
        var expectedResponse = new RadOrderChargeResponse(true, "TND261100002", null);

        _mediatorMock
            .Setup(x => x.Send(It.Is<RadOrderChargeCmd>(c => c.OrderId == orderIdFromBody && c.UserId == "USER02"), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Charge(string.Empty, request);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderChargeCmd>(c => c.OrderId == orderIdFromBody && c.UserId == "USER02"),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public void Charge_HasExpectedActionAttributes()
    {
        var method = typeof(RadOrderController).GetMethod(nameof(RadOrderController.Charge));
        method.Should().NotBeNull();

        var httpPost = method!.GetCustomAttribute<HttpPostAttribute>();
        httpPost.Should().NotBeNull();
        httpPost!.Template.Should().Be("{id}/charge");

        var httpPatch = method!.GetCustomAttribute<HttpPatchAttribute>();
        httpPatch.Should().NotBeNull();
        httpPatch!.Template.Should().Be("charge");
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_WhenOrderFound_Returns200OkWithJSendOk()
    {
        // Arrange
        const string orderId = "RDO2611000001";
        var header = new RadOrderDto() with
        {
            OrderId = orderId,
            OrderNo = "RAD2611000001",
            EmrOrderId = "EMR-RAD-001",
            Modality = "CR",
            RadOrderStatus = 1,
            PatientId = "PAS001",
            PatientName = "Pasien Radiologi",
            DokterPengorderId = "DOK001",
            DokterPengorderName = "dr. Radiolog",
            LayananPengorderId = "LAY001",
            LayananPengorderName = "Rawat Jalan",
            ClinicalNote = "Thorax PA"
        };

        var items = new List<RadOrderItemDto>
        {
            new(orderId, 1, "TRF001", "Thorax PA", "CR", 0)
        };

        var detailResponse = new RadOrderDetailResponse(header, items);

        _mediatorMock
            .Setup(x => x.Send(It.Is<RadOrderGetQuery>(q => q.OrderId == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(detailResponse);

        // Act
        var result = await _sut.Get(orderId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(detailResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderGetQuery>(q => q.OrderId == orderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Get_WhenOrderNotFound_Returns404NotFoundWithJSendFail()
    {
        // Arrange
        const string orderId = "RDO9999999999";

        _mediatorMock
            .Setup(x => x.Send(It.Is<RadOrderGetQuery>(q => q.OrderId == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync((RadOrderDetailResponse?)null);

        // Act
        var result = await _sut.Get(orderId);

        // Assert
        var notFoundResult = result.Should().BeOfType<NotFoundObjectResult>().Subject;
        notFoundResult.StatusCode.Should().Be(404);

        var jsend = notFoundResult.Value.Should().BeOfType<JSendFail>().Subject;
        jsend.status.Should().Be("fail");

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderGetQuery>(q => q.OrderId == orderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Worklist Tests

    [Fact]
    public async Task Worklist_WhenCalledWithParameters_Returns200OkWithList()
    {
        // Arrange
        var testDate = new DateTime(2026, 11, 24);
        const string modality = "CR";
        const int status = 1;

        var worklistItems = new List<RadOrderWorklistView>
        {
            new RadOrderWorklistView() with
            {
                OrderId = "RDO2611000001",
                OrderNo = "RAD2611000001",
                CrtDate = testDate,
                PatientId = "PAS001",
                PatientName = "Pasien Test",
                Modality = "CR",
                RadOrderStatus = 1,
                Urgency = 0,
                IsCito = false,
                ItemCount = 1,
                DokterPengorderName = "dr. Pengorder",
                LayananPengorderName = "Poli Umum"
            }
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<RadOrderWorklistQuery>(q => q.Date == testDate && q.Modality == modality && q.Status == status),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(worklistItems);

        // Act
        var result = await _sut.Worklist(testDate, modality, status);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(worklistItems);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderWorklistQuery>(q => q.Date == testDate && q.Modality == modality && q.Status == status),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Worklist_WhenParametersNull_UsesDefaultDateAndEmptyModality()
    {
        // Arrange
        var worklistItems = new List<RadOrderWorklistView>();

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<RadOrderWorklistQuery>(q => q.Date == DateTime.Today && q.Modality == string.Empty && q.Status == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(worklistItems);

        // Act
        var result = await _sut.Worklist(null, null, null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadOrderWorklistQuery>(q => q.Date == DateTime.Today && q.Modality == string.Empty && q.Status == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region TarifModalityLookup Tests

    [Fact]
    public async Task TarifModalityLookup_WhenModalitySpecified_Returns200OkWithFilteredKatalog()
    {
        // Arrange
        const string modality = "CT";
        var catalog = new List<RadTarifModalityDto>
        {
            new RadTarifModalityDto() with
            {
                TarifId = "TRF003",
                TarifName = "CT Scan Kepala",
                Modality = "CT",
                IsActive = true
            }
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<RadTarifModalityLookupQuery>(q => q.Modality == modality),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        // Act
        var result = await _sut.TarifModalityLookup(modality);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(catalog);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadTarifModalityLookupQuery>(q => q.Modality == modality),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task TarifModalityLookup_WhenModalityNull_Returns200OkWithAllActiveKatalog()
    {
        // Arrange
        var catalog = new List<RadTarifModalityDto>
        {
            new RadTarifModalityDto() with { TarifId = "TRF001", TarifName = "Thorax PA", Modality = "CR", IsActive = true },
            new RadTarifModalityDto() with { TarifId = "TRF003", TarifName = "CT Scan Kepala", Modality = "CT", IsActive = true }
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<RadTarifModalityLookupQuery>(q => q.Modality == null),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(catalog);

        // Act
        var result = await _sut.TarifModalityLookup(null);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(200);

        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(catalog);

        _mediatorMock.Verify(x => x.Send(
            It.Is<RadTarifModalityLookupQuery>(q => q.Modality == null),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
