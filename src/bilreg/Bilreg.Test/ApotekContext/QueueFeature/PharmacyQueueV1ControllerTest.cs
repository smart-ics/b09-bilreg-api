using System.Reflection;
using System.Security.Claims;
using Bilreg.Api.Controllers.ApotekContext;
using Bilreg.Application.ApotekContext.QueueFeature;
using Bilreg.Application.ApotekContext.QueueFeature.UseCases;
using Bilreg.Application.Shared;
using Bilreg.Domain.ApotekContext.QueueFeature;
using Bilreg.Domain.ApotekContext.Shared;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Nuna.Lib.ValidationHelper;
using Xunit;

namespace Bilreg.Test.ApotekContext.QueueFeature;

public class PharmacyQueueV1ControllerTest
{
    private readonly Mock<IMediator> _mediator = new();
    private readonly Mock<ICurrentUserContext> _user = new();
    private readonly Mock<ITglJamProvider> _clock = new();
    private readonly PharmacyQueueV1Controller _sut;

    public PharmacyQueueV1ControllerTest()
    {
        _clock.Setup(x => x.Now).Returns(new DateTime(2026, 10, 18, 9, 30, 0));
        _user.Setup(x => x.GetActorUserId()).Returns("PETUGAS_01");

        _sut = new PharmacyQueueV1Controller(_mediator.Object, _user.Object, _clock.Object);

        var httpContext = new DefaultHttpContext();
        httpContext.User = new ClaimsPrincipal(new ClaimsIdentity(
        [
            new Claim(ClaimTypes.NameIdentifier, "PETUGAS_01"),
            new Claim(ClaimTypes.Name, "Petugas Apotek 1")
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = httpContext
        };
    }

    #region 1. Controller Metadata & Decoration Contract Tests

    [Fact]
    public void Controller_HasExpectedRouteAndSecurityAttributes()
    {
        var type = typeof(PharmacyQueueV1Controller);

        var routeAttr = type.GetCustomAttribute<RouteAttribute>();
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/v1/pharmacy-queue");

        type.GetCustomAttribute<ApiControllerAttribute>().Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>().Should().NotBeNull();

        var serviceFilter = type.GetCustomAttribute<ServiceFilterAttribute>();
        serviceFilter.Should().NotBeNull();
        serviceFilter!.ServiceType.Should().Be(typeof(ApotekExceptionFilter));
    }

    [Fact]
    public void Controller_ExposesAllTwelveDocumentedEndpoints()
    {
        var methods = typeof(PharmacyQueueV1Controller)
            .GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly);

        var postTemplates = methods
            .Select(m => m.GetCustomAttribute<HttpPostAttribute>()?.Template)
            .Where(t => t is not null)
            .ToList();

        var getTemplates = methods
            .Select(m => m.GetCustomAttribute<HttpGetAttribute>()?.Template)
            .Where(t => t is not null)
            .ToList();

        postTemplates.Should().Contain([
            "intake",
            "call-next",
            "recall",
            "skip",
            "start-service",
            "release",
            "map-demand",
            "flag-pasien-hadir",
            "call-serah",
            "complete-handover"
        ]);

        getTemplates.Should().Contain([
            "worklist",
            "service-points"
        ]);
    }

    [Fact]
    public void IntakeEndpoint_AllowsAnonymousKioskAccess()
    {
        var intakeMethod = typeof(PharmacyQueueV1Controller)
            .GetMethod(nameof(PharmacyQueueV1Controller.Intake));

        intakeMethod.Should().NotBeNull();
        intakeMethod!.GetCustomAttribute<AllowAnonymousAttribute>().Should().NotBeNull();
    }

    #endregion

    #region 2. Functional Endpoint Delegation Tests

    [Fact]
    public async Task Intake_WhenValid_DispatchesCommandAndReturnsJSendOk()
    {
        // Arrange
        PharmacyQueueIntakeCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueIntakeResponse(
            AntrianId: "ANT001",
            NoUrut: 5,
            QueueLabel: "A005",
            ServicePointId: "SP01",
            ServicePointName: "Non Racikan",
            QueuePrefix: "A",
            ApotekPhase: ApotekPhaseEnum.TAKEN,
            TakenAt: DateTime.Now,
            WaitingAheadCount: 2);

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueIntakeCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueIntakeResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueIntakeCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Intake(new PharmacyQueueIntakeRequest("SP01", "DEPO01"));

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = ok.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.ServicePointId.Should().Be("SP01");
        capturedCmd.DepoFarmasiId.Should().Be("DEPO01");
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task Intake_WhenServicePointIdEmpty_ThrowsApotekDomainException()
    {
        var act = () => _sut.Intake(new PharmacyQueueIntakeRequest(""));
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Service point ID wajib diisi*");
    }

    [Fact]
    public async Task Worklist_WhenValid_DispatchesQueryAndReturnsJSendOk()
    {
        // Arrange
        PharmacyQueueWorklistQry? capturedQry = null;
        var expectedResponse = new PharmacyQueueWorklistResponse(
            DepoFarmasiId: "DEPO01",
            Items: [],
            TotalCount: 0,
            WaitingCount: 0,
            PreparedCount: 0);

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueWorklistQry>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueWorklistResponse>, CancellationToken>((req, _) =>
                capturedQry = (PharmacyQueueWorklistQry)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Worklist("DEPO01", null, "Menunggu", ApotekPhaseEnum.TAKEN);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = ok.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedQry.Should().NotBeNull();
        capturedQry!.DepoFarmasiId.Should().Be("DEPO01");
        capturedQry.FilterTab.Should().Be("Menunggu");
        capturedQry.Phase.Should().Be(ApotekPhaseEnum.TAKEN);
    }

    [Fact]
    public async Task Worklist_WhenDepoMissing_ThrowsApotekDomainException()
    {
        var act = () => _sut.Worklist(null, "", null, null);
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Depo Farmasi ID wajib diisi*");
    }

    [Fact]
    public async Task CallNext_WhenValid_DispatchesCommandWithResolvedRowVersion()
    {
        // Arrange
        var rowVersionBytes = new byte[] { 0, 0, 0, 0, 0, 0, 1, 10 };
        var base64RowVersion = Convert.ToBase64String(rowVersionBytes);

        PharmacyQueueCallPenerimaanCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            LoketKey: "APT-RCV-01",
            DisplayName: "Loket 1",
            LoketType: LoketTypeEnum.PenerimaanResep,
            AntrianId: "ANT001",
            NoUrut: 5,
            QueueLabel: "A005",
            ClaimState: LoketClaimStateEnum.Called,
            AnnouncementVersion: 2,
            CalledAt: DateTime.Now,
            RowVersion: rowVersionBytes,
            Success: true,
            Message: "Panggilan sukses");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueCallPenerimaanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueCallPenerimaanCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CallNext(new PharmacyQueueCallRequest(
            LoketKey: "APT-RCV-01",
            AntrianId: "ANT001",
            NoUrut: 5,
            ExpectedRowVersion: base64RowVersion));

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = ok.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-RCV-01");
        capturedCmd.AntrianId.Should().Be("ANT001");
        capturedCmd.NoUrut.Should().Be(5);
        capturedCmd.ExpectedRowVersion.Should().Equal(rowVersionBytes);
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task CallNext_WhenLoketKeyEmpty_ThrowsApotekDomainException()
    {
        var act = () => _sut.CallNext(new PharmacyQueueCallRequest(""));
        await act.Should().ThrowAsync<ApotekDomainException>()
            .WithMessage("*Loket Key wajib diisi*");
    }

    [Fact]
    public async Task Recall_WhenValid_DispatchesCommand()
    {
        // Arrange
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        PharmacyQueueRecallPenerimaanCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            "APT-RCV-01", "Loket 1", LoketTypeEnum.PenerimaanResep, "ANT1", 1, "A001",
            LoketClaimStateEnum.Called, 3, DateTime.Now, rowVersion, true, "Recall sukses");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueRecallPenerimaanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueRecallPenerimaanCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Recall(new PharmacyQueueRecallRequest("APT-RCV-01", Convert.ToBase64String(rowVersion)));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-RCV-01");
        capturedCmd.RowVersion.Should().Equal(rowVersion);
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task Skip_WhenValid_DispatchesCommand()
    {
        // Arrange
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        PharmacyQueueSkipPenerimaanCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            "APT-RCV-01", "Loket 1", LoketTypeEnum.PenerimaanResep, "ANT1", 1, "A001",
            LoketClaimStateEnum.Idle, 3, DateTime.Now, rowVersion, true, "Skip sukses");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueSkipPenerimaanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueSkipPenerimaanCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Skip(new PharmacyQueueSkipRequest("APT-RCV-01", "Pasien tidak hadir", rowVersion));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-RCV-01");
        capturedCmd.Reason.Should().Be("Pasien tidak hadir");
        capturedCmd.RowVersion.Should().Equal(rowVersion);
    }

    [Fact]
    public async Task StartService_WhenValid_DispatchesCommand()
    {
        // Arrange
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        PharmacyQueueStartPenerimaanCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            "APT-RCV-01", "Loket 1", LoketTypeEnum.PenerimaanResep, "ANT1", 1, "A001",
            LoketClaimStateEnum.InService, 1, DateTime.Now, rowVersion, true, "Mulai layan");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueStartPenerimaanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueStartPenerimaanCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.StartService(new PharmacyQueueStartServiceRequest("APT-RCV-01", rowVersion));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-RCV-01");
        capturedCmd.RowVersion.Should().Equal(rowVersion);
    }

    [Fact]
    public async Task Release_WhenValid_DispatchesCommand()
    {
        // Arrange
        var rowVersion = new byte[] { 1, 2, 3, 4 };
        PharmacyQueueReleasePenerimaanCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            "APT-RCV-01", "Loket 1", LoketTypeEnum.PenerimaanResep, "ANT1", 1, "A001",
            LoketClaimStateEnum.Idle, 1, DateTime.Now, rowVersion, true, "Release");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueReleasePenerimaanCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueReleasePenerimaanCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.Release(new PharmacyQueueReleaseRequest("APT-RCV-01", rowVersion));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-RCV-01");
        capturedCmd.RowVersion.Should().Equal(rowVersion);
    }

    [Fact]
    public async Task MapDemand_WhenValid_DispatchesCommand()
    {
        // Arrange
        PharmacyQueueMapDemandCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueMapDemandResponse("ANT1", 1, QueueDemandKindEnum.Resep, "KP001", DateTime.Now, true);

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueMapDemandCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueMapDemandResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueMapDemandCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.MapDemand(new PharmacyQueueMapDemandRequest(
            AntrianId: "ANT1",
            NoUrut: 1,
            DemandKind: QueueDemandKindEnum.Resep,
            DemandId: "KP001",
            DemandIds: ["KP001", "KP002"],
            PasienTrackerId: "TRK01"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.AntrianId.Should().Be("ANT1");
        capturedCmd.NoUrut.Should().Be(1);
        capturedCmd.DemandId.Should().Be("KP001");
        capturedCmd.DemandIds.Should().Contain(["KP001", "KP002"]);
        capturedCmd.PasienTrackerId.Should().Be("TRK01");
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task FlagPasienHadir_WhenValid_DispatchesCommand()
    {
        // Arrange
        PharmacyQueueFlagPasienHadirCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueFlagPasienHadirResponse(
            QueueDemandKindEnum.Resep, "KP001", "ANT1", 1, true, DateTime.Now, true);

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueFlagPasienHadirCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueFlagPasienHadirResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueFlagPasienHadirCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.FlagPasienHadir(new PharmacyQueueFlagPasienHadirRequest("ANT1", 1, QueueDemandKindEnum.Resep, "KP001"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.AntrianId.Should().Be("ANT1");
        capturedCmd.NoUrut.Should().Be(1);
        capturedCmd.DemandId.Should().Be("KP001");
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task CallSerah_WhenValid_DispatchesCommand()
    {
        // Arrange
        var rowVersion = new byte[] { 5, 6, 7, 8 };
        PharmacyQueueCallSerahCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueLoketCallResponse(
            "APT-SRH-01", "Loket 2", LoketTypeEnum.SerahObat, "ANT1", 1, "A001",
            LoketClaimStateEnum.Called, 1, DateTime.Now, rowVersion, true, "Call serah sukses");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueCallSerahCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueLoketCallResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueCallSerahCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CallSerah(new PharmacyQueueCallRequest("APT-SRH-01", "ANT1", 1, rowVersion));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.LoketKey.Should().Be("APT-SRH-01");
        capturedCmd.AntrianId.Should().Be("ANT1");
        capturedCmd.NoUrut.Should().Be(1);
        capturedCmd.ExpectedRowVersion.Should().Equal(rowVersion);
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task CompleteHandover_WhenValid_DispatchesCommand()
    {
        // Arrange
        PharmacyQueueCompleteHandoverCmd? capturedCmd = null;
        var expectedResponse = new PharmacyQueueCompleteHandoverResponse(
            "ANT1", 1, ApotekPhaseEnum.SELESAI, DateTime.Now, false, true, "Handover selesai");

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueCompleteHandoverCmd>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<PharmacyQueueCompleteHandoverResponse>, CancellationToken>((req, _) =>
                capturedCmd = (PharmacyQueueCompleteHandoverCmd)req)
            .ReturnsAsync(expectedResponse);

        // Act
        var result = await _sut.CompleteHandover(new PharmacyQueueCompleteHandoverRequest(
            AntrianId: "ANT1",
            NoUrut: 1,
            PenjualanId: "DU001",
            NoSep: "SEP123",
            IsPaymentOverridden: true,
            PaymentOverrideReason: "Override Darurat",
            PaymentOverridePharmacistId: "APT01",
            LoketKey: "APT-SRH-01"));

        // Assert
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.AntrianId.Should().Be("ANT1");
        capturedCmd.NoUrut.Should().Be(1);
        capturedCmd.PenjualanId.Should().Be("DU001");
        capturedCmd.NoSep.Should().Be("SEP123");
        capturedCmd.IsPaymentOverridden.Should().BeTrue();
        capturedCmd.PaymentOverrideReason.Should().Be("Override Darurat");
        capturedCmd.PaymentOverridePharmacistId.Should().Be("APT01");
        capturedCmd.LoketKey.Should().Be("APT-SRH-01");
        capturedCmd.UserId.Should().Be("PETUGAS_01");
    }

    [Fact]
    public async Task ListServicePoints_WhenValid_DispatchesQueryAndReturnsJSendOk()
    {
        // Arrange
        PharmacyQueueServicePointsQry? capturedQry = null;
        var list = new List<PharmacyServicePointResponse>
        {
            new("SP01", "DEPO01", "Non-Racikan", "A", true),
            new("SP02", "DEPO01", "Racikan", "B", true)
        };

        _mediator
            .Setup(x => x.Send(It.IsAny<PharmacyQueueServicePointsQry>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<IReadOnlyList<PharmacyServicePointResponse>>, CancellationToken>((req, _) =>
                capturedQry = (PharmacyQueueServicePointsQry)req)
            .ReturnsAsync(list);

        // Act
        var result = await _sut.ListServicePoints("DEPO01", null, true);

        // Assert
        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var jsend = ok.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().BeEquivalentTo(list);

        capturedQry.Should().NotBeNull();
        capturedQry!.DepoFarmasiId.Should().Be("DEPO01");
        capturedQry.ActiveOnly.Should().BeTrue();
    }

    #endregion

    #region 3. Standard Error Handling & Filter Tests

    [Theory]
    [InlineData(typeof(ApotekConcurrencyException), StatusCodes.Status409Conflict, "CONCURRENCY")]
    [InlineData(typeof(ApotekDomainException), StatusCodes.Status400BadRequest, "DOMAIN")]
    [InlineData(typeof(KeyNotFoundException), StatusCodes.Status404NotFound, "NOT_FOUND")]
    [InlineData(typeof(UnauthorizedAccessException), StatusCodes.Status401Unauthorized, "AUTH")]
    [InlineData(typeof(ArgumentException), StatusCodes.Status400BadRequest, "ARGUMENT")]
    [InlineData(typeof(InvalidOperationException), StatusCodes.Status400BadRequest, "INVALID_OPERATION")]
    public void ApotekExceptionFilter_HandlesAllErrorTypesWithStandardCodes(
        Type exceptionType, int expectedStatusCode, string expectedCode)
    {
        var filter = new ApotekExceptionFilter();
        Exception ex = exceptionType switch
        {
            _ when exceptionType == typeof(ApotekConcurrencyException) => new ApotekConcurrencyException("LOKET-1", 1),
            _ when exceptionType == typeof(ApotekDomainException) => new ApotekDomainException("Domain error"),
            _ when exceptionType == typeof(KeyNotFoundException) => new KeyNotFoundException("Item not found"),
            _ when exceptionType == typeof(UnauthorizedAccessException) => new UnauthorizedAccessException("Forbidden"),
            _ when exceptionType == typeof(ArgumentException) => new ArgumentException("Bad argument"),
            _ when exceptionType == typeof(InvalidOperationException) => new InvalidOperationException("Invalid state"),
            _ => new Exception("Generic")
        };

        var context = new ExceptionContext(
            new ActionContext
            {
                HttpContext = new DefaultHttpContext(),
                RouteData = new Microsoft.AspNetCore.Routing.RouteData(),
                ActionDescriptor = new Microsoft.AspNetCore.Mvc.Abstractions.ActionDescriptor()
            },
            []);
        context.Exception = ex;

        filter.OnException(context);

        context.ExceptionHandled.Should().BeTrue();
        var result = context.Result.Should().BeOfType<ObjectResult>().Subject;
        result.StatusCode.Should().Be(expectedStatusCode);

        var payload = result.Value!;
        payload.GetType().GetProperty("status")!.GetValue(payload).Should().Be("fail");
        payload.GetType().GetProperty("code")!.GetValue(payload).Should().Be(expectedCode);
    }

    #endregion
}
