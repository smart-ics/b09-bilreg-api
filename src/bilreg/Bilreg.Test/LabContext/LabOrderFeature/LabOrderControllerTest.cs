using System.Reflection;
using Bilreg.Api.Controllers.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature;
using Bilreg.Application.LabContext.LabOrderFeature.UseCases;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.LabContext.LabOrderFeature;

public class LabOrderControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly LabOrderController _sut;

    public LabOrderControllerTest()
    {
        _sut = new LabOrderController(_mediatorMock.Object);
    }

    #region Controller Attribute & Route Structure Tests

    [Fact]
    public void Controller_HasExpectedAttributesAndRoute()
    {
        var type = typeof(LabOrderController);

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/LabContext/LabOrderFeature");
    }

    [Fact]
    public void CreateFromEmrAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabOrderController).GetMethod(nameof(LabOrderController.CreateFromEmr));
        method.Should().NotBeNull();

        var postAttrs = method!.GetCustomAttributes<HttpPostAttribute>().ToList();
        postAttrs.Should().NotBeEmpty();
        postAttrs.Select(a => a.Template).Should().Contain(["from-emr", "fromEmr"]);
    }

    [Fact]
    public void CreateExternalAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabOrderController).GetMethod(nameof(LabOrderController.CreateExternal));
        method.Should().NotBeNull();

        var postAttrs = method!.GetCustomAttributes<HttpPostAttribute>().ToList();
        postAttrs.Should().NotBeEmpty();
        postAttrs.Select(a => a.Template).Should().Contain(["external-patient", "external"]);
    }

    [Fact]
    public void CancelAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabOrderController).GetMethod(nameof(LabOrderController.Cancel));
        method.Should().NotBeNull();

        var postAttr = method!.GetCustomAttribute<HttpPostAttribute>();
        postAttr.Should().NotBeNull();
        postAttr!.Template.Should().Be("cancel");

        var patchAttr = method!.GetCustomAttribute<HttpPatchAttribute>();
        patchAttr.Should().NotBeNull();
        patchAttr!.Template.Should().Be("cancel");
    }

    [Fact]
    public void GetAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabOrderController).GetMethod(nameof(LabOrderController.Get));
        method.Should().NotBeNull();

        var getAttr = method!.GetCustomAttribute<HttpGetAttribute>();
        getAttr.Should().NotBeNull();
        getAttr!.Template.Should().Be("{orderId}");
    }

    [Fact]
    public void WorklistAction_HasExpectedHttpAttributes()
    {
        var method = typeof(LabOrderController).GetMethod(nameof(LabOrderController.Worklist));
        method.Should().NotBeNull();

        var getAttr = method!.GetCustomAttribute<HttpGetAttribute>();
        getAttr.Should().NotBeNull();
        getAttr!.Template.Should().Be("worklist");
    }

    #endregion

    #region CreateFromEmr Endpoint Tests

    [Fact]
    public async Task CreateFromEmr_GivenValidCommand_DispatchesAndReturnsOkWithJSendOk()
    {
        // Arrange
        var cmd = new LabOrderCreateFromEmrCmd(
            UserId: "DOKTER01",
            EmrOrderId: "EMR-20261015-001",
            RegId: "REG001",
            PatientId: "MR001",
            PatientName: "Budi Santoso",
            BirthDateYmd: "1985-05-20",
            Gender: "L",
            Items: [new LabOrderTarifItemInput("TRF001", "Tarif CK-MB", Urgency: 2)],
            DokterPerujukId: "PPA001",
            DokterPerujukName: "dr. Andi, Sp.PK",
            LayananPerujukId: "LAY001",
            LayananPerujukName: "IGD",
            ClinicalNote: "Nyeri dada akut",
            Urgency: 2,
            AllowDuplicateConfirmation: true);

        var expectedResponse = new LabOrderCreateFromEmrResponse(
            EmrOrderId: "EMR-20261015-001",
            LabOrderStatus: 1,
            OrderNo: "LAB20261015001");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateFromEmr(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CreateExternal Endpoint Tests

    [Fact]
    public async Task CreateExternal_GivenValidCommand_DispatchesAndReturnsOkWithJSendOk()
    {
        // Arrange
        var cmd = new LabOrderCreateExternalPatientCmd(
            UserId: "PETUGAS01",
            EmrOrderId: null,
            RegId: null,
            PatientId: "MR-EXT-001",
            PatientName: "Siti Rahma",
            BirthDateYmd: "1992-08-15",
            Gender: "P",
            Items: [new LabOrderTarifItemInput("TRF002", "Tarif Darah Lengkap", Urgency: 1)],
            RegExternalId: "LEX20261015001",
            ExternalMrNo: "MR-EXT-001",
            DokterPerujukId: "",
            DokterPerujukName: "dr. Eksternal",
            LayananPerujukId: "",
            LayananPerujukName: "Klinik Pratama",
            ClinicalNote: "Rutin tahunan",
            Urgency: 1,
            AllowDuplicateConfirmation: false);

        var expectedResponse = new LabOrderCreateResponse(
            OrderId: "LBO20261015002",
            OrderNo: "LAB20261015002");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CreateExternal(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Cancel Endpoint Tests

    [Fact]
    public async Task Cancel_GivenValidCommand_DispatchesAndReturnsOkWithJSendOk()
    {
        // Arrange
        var cmd = new LabOrderCancelCmd("LBO20261015001", "DOKTER01", "Pasien menolak tes");

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _sut.Cancel(cmd);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be("Done");

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Get Endpoint Tests

    [Fact]
    public async Task Get_GivenExistingOrderId_DispatchesQueryAndReturnsDetailWithClinicalContext()
    {
        // Arrange
        const string orderId = "LBO20261015001";
        var expectedResponse = new LabOrderGetResponse(
            OrderId: orderId,
            EmrOrderId: "EMR001",
            OrderNo: "LAB001",
            OrderSource: 1,
            LabOrderStatus: 1,
            LastBillingReleaseStatus: 0,
            OwareStatus: 0,
            RegId: "REG001",
            PatientId: "MR001",
            PatientName: "Budi Santoso",
            BirthDate: new DateTime(1985, 5, 20),
            Gender: "L",
            AgeAtOrder: 41,
            ExecutionRegId: "",
            DeferredReason: "",
            DeferredUntil: new DateTime(3000, 1, 1),
            BillingTindakanId: "",
            BillingLastError: "",
            CollectedDate: new DateTime(3000, 1, 1),
            CollectedUserId: "",
            CollectionNote: "",
            LastBillingReleaseCheckAt: new DateTime(3000, 1, 1),
            LastBillingReleaseCheckUserId: "",
            LastBillingReleaseMessage: "",
            ReleasedDate: new DateTime(3000, 1, 1),
            ReleasedUserId: "",
            ReleaseNote: "",
            CancelledReason: "",
            CancelledDate: new DateTime(3000, 1, 1),
            CancelledUserId: "",
            TerminationReason: "",
            TerminationDate: new DateTime(3000, 1, 1),
            TerminationUserId: "",
            IsVoided: false,
            Items:
            [
                new LabOrderItemResponse(1, "DEF01", "CKMB", "CK-MB", "TRF01", "CKMB01", "Tarif CK-MB", 1, "Serum", 1, Urgency: 2, IsCito: true)
            ],
            ItemComponents: [],
            DokterPerujukId: "PPA001",
            DokterPerujukName: "dr. Andi, Sp.PK",
            LayananPerujukId: "LAY001",
            LayananPerujukName: "IGD",
            ClinicalNote: "Nyeri dada akut",
            Urgency: 2,
            IsCito: true);

        _mediatorMock
            .Setup(x => x.Send(It.Is<LabOrderGetQuery>(q => q.OrderId == orderId), It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Get(orderId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<LabOrderGetQuery>(q => q.OrderId == orderId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region Worklist Endpoint Tests

    [Fact]
    public async Task Worklist_GivenParametersWithIsCitoOnly_DispatchesQueryAndReturnsWorklist()
    {
        // Arrange
        var expectedResponse = new List<LabOrderWorklistView>
        {
            new(
                OrderId: "LBO20261015001",
                OrderNo: "LAB20261015001",
                LabOrderStatus: 1,
                OrderSource: 1,
                PatientId: "MR001",
                PatientName: "Budi Santoso",
                Gender: "L",
                AgeAtOrder: 41,
                ItemCount: 1,
                LastBillingReleaseStatus: 0,
                OwareStatus: 0,
                CrtDate: new DateTime(2026, 10, 15, 14, 0, 0),
                BillingTindakanId: "",
                IsCito: true,
                DokterPerujukName: "dr. Andi, Sp.PK",
                LayananPerujukName: "IGD",
                DeferredReason: "",
                TestNames: ["CK-MB"])
        };

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<LabOrderWorklistQuery>(q =>
                    q.LabOrderStatus == 1 &&
                    q.SearchTerm == "CKMB" &&
                    q.Date1 == new DateTime(2026, 10, 1) &&
                    q.Date2 == new DateTime(2026, 10, 15) &&
                    q.IsCitoOnly == true),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Worklist(
            labOrderStatus: 1,
            searchTerm: "CKMB",
            date1: new DateTime(2026, 10, 1),
            date2: new DateTime(2026, 10, 15),
            isCitoOnly: true);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.status.Should().Be("success");
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<LabOrderWorklistQuery>(q =>
                q.LabOrderStatus == 1 &&
                q.SearchTerm == "CKMB" &&
                q.Date1 == new DateTime(2026, 10, 1) &&
                q.Date2 == new DateTime(2026, 10, 15) &&
                q.IsCitoOnly == true),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion
}
