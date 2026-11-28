using System.Net;
using System.Reflection;
using System.Security.Claims;
using System.Text.Json;
using Bilreg.Api.Configurations;
using Bilreg.Api.Controllers.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature;
using Bilreg.Application.BedUsageContext.KamarOperasiFeature.UseCases;
using Bilreg.Domain.BedUsageContext.KamarOperasiFeature;
using Bilreg.Infrastructure.BedUsageContext.KamarOperasiFeature;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Nuna.Lib.ActionResultHelper;
using Xunit;

namespace Bilreg.Test.BedUsageContext.KamarOperasiFeature;

public class PreOpClearanceControllerTest
{
    private readonly Mock<IMediator> _mediatorMock = new();
    private readonly PreOpClearanceController _sut;

    public PreOpClearanceControllerTest()
    {
        _sut = new PreOpClearanceController(_mediatorMock.Object);
    }

    [Fact]
    public void Controller_ShouldHaveRouteApiControllerAndAuthorizeAttributes()
    {
        var type = typeof(PreOpClearanceController);

        var routeAttr = type.GetCustomAttribute<RouteAttribute>(true);
        routeAttr.Should().NotBeNull();
        routeAttr!.Template.Should().Be("api/[controller]");

        type.GetCustomAttribute<ApiControllerAttribute>(true).Should().NotBeNull();
        type.GetCustomAttribute<AuthorizeAttribute>(true).Should().NotBeNull();
    }

    [Fact]
    public void Actions_ShouldHaveExpectedHttpMethodAndRouteAttributes()
    {
        var saveMethod = typeof(PreOpClearanceController).GetMethod(nameof(PreOpClearanceController.Save));
        saveMethod.Should().NotBeNull();
        var httpPostAttr = saveMethod!.GetCustomAttribute<HttpPostAttribute>();
        httpPostAttr.Should().NotBeNull();
        httpPostAttr!.Template.Should().Be("Save");

        var getMethod = typeof(PreOpClearanceController).GetMethod(nameof(PreOpClearanceController.Get));
        getMethod.Should().NotBeNull();
        var httpGetAttr = getMethod!.GetCustomAttribute<HttpGetAttribute>();
        httpGetAttr.Should().NotBeNull();
        httpGetAttr!.Template.Should().Be("Get");
    }

    [Fact]
    public async Task Save_GivenValidCommand_WhenSuccess_Returns200OkWithJSendOk()
    {
        // ARRANGE
        var checklist = new List<ChecklistItemInput>
        {
            new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "Sesuai"),
            new(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Cleared, "Tertandatangani")
        };

        var cmd = new OkPreOpClearanceSaveCommand(
            OrderOpId: "ORD-001",
            UserId: "user-01",
            ChecklistItems: checklist,
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01",
            FinalStatus: "CLEARED");

        var expectedResponse = new OkPreOpClearanceSaveResponse(
            ClearanceId: "CLR-001",
            ScheduleOpId: "SCH-001",
            OrderOpId: "ORD-001",
            RegId: "REG-001",
            NoUrut: 1,
            ClearanceStatus: nameof(PreOpClearanceStatusEnum.Cleared),
            OpCaseState: nameof(OpCaseStateEnum.PreOpCleared),
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01",
            CompletedAt: new DateTime(2026, 11, 25, 8, 0, 0),
            DokterOverrideId: string.Empty,
            AlasanOverride: string.Empty,
            AlasanNotCleared: string.Empty,
            ChecklistItems:
            [
                new PreOpChecklistItemDto(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "Sesuai"),
                new PreOpChecklistItemDto(PreOpChecklistItemType.InformedConsent, ChecklistItemStatusEnum.Cleared, "Tertandatangani")
            ]);

        _mediatorMock
            .Setup(x => x.Send(cmd, It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Save(cmd, CancellationToken.None);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(cmd, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Save_WhenUserClaimExists_OverridesUserIdWithClaim()
    {
        // ARRANGE
        const string claimUserId = "claim-preop-user";
        var user = new ClaimsPrincipal(new ClaimsIdentity([
            new Claim("UserId", claimUserId)
        ], "TestAuth"));

        _sut.ControllerContext = new ControllerContext
        {
            HttpContext = new DefaultHttpContext { User = user }
        };

        var cmd = new OkPreOpClearanceSaveCommand(
            OrderOpId: "ORD-001",
            UserId: "client-user",
            ChecklistItems: [new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Pending)],
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01");

        OkPreOpClearanceSaveCommand? capturedCmd = null;
        var expectedResponse = new OkPreOpClearanceSaveResponse(
            ClearanceId: "CLR-001",
            ScheduleOpId: "SCH-001",
            OrderOpId: "ORD-001",
            RegId: "REG-001",
            NoUrut: 1,
            ClearanceStatus: nameof(PreOpClearanceStatusEnum.PendingClearance),
            OpCaseState: nameof(OpCaseStateEnum.Scheduled),
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01",
            CompletedAt: new DateTime(3000, 1, 1),
            DokterOverrideId: string.Empty,
            AlasanOverride: string.Empty,
            AlasanNotCleared: string.Empty,
            ChecklistItems: [new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Pending, string.Empty)]);

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkPreOpClearanceSaveCommand>(), It.IsAny<CancellationToken>()))
            .Callback<IRequest<OkPreOpClearanceSaveResponse>, CancellationToken>((req, _) =>
                capturedCmd = req as OkPreOpClearanceSaveCommand)
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Save(cmd, CancellationToken.None);

        // ASSERT
        result.Should().BeOfType<OkObjectResult>();
        capturedCmd.Should().NotBeNull();
        capturedCmd!.UserId.Should().Be(claimUserId);
    }

    [Fact]
    public async Task Save_WhenPreOpChecklistIncompleteExceptionThrown_ThrowsPreOpChecklistIncompleteException()
    {
        // ARRANGE
        const string errorMessage = "Checklist Pre-Operative belum lengkap atau terdapat item FAILED.";
        var cmd = new OkPreOpClearanceSaveCommand(
            OrderOpId: "ORD-001",
            UserId: "user-01",
            ChecklistItems: [new(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Failed, "Tidak cocok")],
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01",
            FinalStatus: "CLEARED");

        _mediatorMock
            .Setup(x => x.Send(It.IsAny<OkPreOpClearanceSaveCommand>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new PreOpChecklistIncompleteException(errorMessage));

        // ACT
        var act = () => _sut.Save(cmd, CancellationToken.None);

        // ASSERT
        await act.Should().ThrowAsync<PreOpChecklistIncompleteException>()
            .WithMessage(errorMessage);
    }

    [Fact]
    public async Task Get_GivenOrderOpIdAndScheduleOpId_WhenSuccess_Returns200OkWithJSendOk()
    {
        // ARRANGE
        const string orderOpId = "ORD-001";
        const string scheduleOpId = "SCH-001";

        var expectedResponse = new OkPreOpClearanceGetResponse(
            ClearanceId: "CLR-001",
            ScheduleOpId: scheduleOpId,
            OrderOpId: orderOpId,
            RegId: "REG-001",
            NoUrut: 1,
            ClearanceStatus: nameof(PreOpClearanceStatusEnum.Cleared),
            VerifikatorPpaId: "PPA-01",
            DokterPjId: "DOK-01",
            CompletedAt: new DateTime(2026, 11, 25, 8, 0, 0),
            DokterOverrideId: string.Empty,
            AlasanOverride: string.Empty,
            AlasanNotCleared: string.Empty,
            ChecklistItems:
            [
                new PreOpChecklistItemDto(PreOpChecklistItemType.PatientIdentity, ChecklistItemStatusEnum.Verified, "OK")
            ]);

        _mediatorMock
            .Setup(x => x.Send(
                It.Is<OkPreOpClearanceGetQuery>(q => q.OrderOpId == orderOpId && q.ScheduleOpId == scheduleOpId),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(expectedResponse);

        // ACT
        var result = await _sut.Get(orderOpId, scheduleOpId, CancellationToken.None);

        // ASSERT
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.StatusCode.Should().Be(StatusCodes.Status200OK);
        var jsend = okResult.Value.Should().BeOfType<JSendOk>().Subject;
        jsend.data.Should().Be(expectedResponse);

        _mediatorMock.Verify(x => x.Send(
            It.Is<OkPreOpClearanceGetQuery>(q => q.OrderOpId == orderOpId && q.ScheduleOpId == scheduleOpId),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ErrorHandlerMiddleware_WhenUnauthorizedAccessExceptionWithAuthenticatedUser_Returns403Forbidden()
    {
        // ARRANGE
        var context = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("UserId", "user-01")], "TestAuth"))
        };
        context.Response.Body = new MemoryStream();

        const string errorMessage = "Dokter override tidak berwenang pada jadwal ini.";
        RequestDelegate next = _ => throw new UnauthorizedAccessException(errorMessage);
        var middleware = new ErrorHandlerMiddleware(next, NullLogger<ErrorHandlerMiddleware>.Instance);

        // ACT
        await middleware.Invoke(context);

        // ASSERT
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.Forbidden);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("AQ_FORBIDDEN");
    }

    [Fact]
    public async Task ErrorHandlerMiddleware_WhenInvalidOperationException_Returns400BadRequest()
    {
        // ARRANGE
        var context = new DefaultHttpContext();
        context.Response.Body = new MemoryStream();

        const string errorMessage = "Jadwal operasi masih berstatus DRAFT.";
        RequestDelegate next = _ => throw new InvalidOperationException(errorMessage);
        var middleware = new ErrorHandlerMiddleware(next, NullLogger<ErrorHandlerMiddleware>.Instance);

        // ACT
        await middleware.Invoke(context);

        // ASSERT
        context.Response.StatusCode.Should().Be((int)HttpStatusCode.BadRequest);
        context.Response.Body.Seek(0, SeekOrigin.Begin);
        using var reader = new StreamReader(context.Response.Body);
        var body = await reader.ReadToEndAsync();
        using var json = JsonDocument.Parse(body);
        json.RootElement.GetProperty("status").GetString().Should().Be("AQ_OPERATION_NOT_ALLOWED");
    }

    [Fact]
    public void DependencyInjection_WhenAddInfrastructureCalled_RegistersPreOpClearanceAndStartOpServicesAsScoped()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:ServerName"] = "localhost",
                ["Database:DbName"] = "test"
            })
            .Build();

        // ACT
        services.AddInfrastructure(configuration);

        // ASSERT
        services.Should().Contain(d =>
            d.ServiceType == typeof(IPreOpClearanceDal) &&
            d.ImplementationType == typeof(PreOpClearanceDal) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPreOpClearanceChecklistDal) &&
            d.ImplementationType == typeof(PreOpClearanceChecklistDal) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IPreOpClearanceRepo) &&
            d.ImplementationType == typeof(PreOpClearanceRepo) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IStartOpDal) &&
            d.Lifetime == ServiceLifetime.Scoped);

        services.Should().Contain(d =>
            d.ServiceType == typeof(IStartOpRepo) &&
            d.Lifetime == ServiceLifetime.Scoped);

        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var sp = scope.ServiceProvider;

        sp.GetRequiredService<IPreOpClearanceDal>().Should().BeOfType<PreOpClearanceDal>();
        sp.GetRequiredService<IPreOpClearanceChecklistDal>().Should().BeOfType<PreOpClearanceChecklistDal>();
        sp.GetRequiredService<IPreOpClearanceRepo>().Should().BeOfType<PreOpClearanceRepo>();
        sp.GetRequiredService<IStartOpDal>().Should().BeOfType<StartOpDal>();
        sp.GetRequiredService<IStartOpRepo>().Should().BeOfType<StartOpRepo>();
    }

    [Fact]
    public void DependencyInjection_WhenAddApplicationCalled_RegistersPublishTrackerOnPreOpClearedEvHandler()
    {
        // ARRANGE
        var services = new ServiceCollection();
        var configuration = new ConfigurationBuilder().Build();

        // ACT
        services.AddApplication(configuration);

        // ASSERT
        services.Should().Contain(d =>
            d.ServiceType == typeof(INotificationHandler<OkPreOpClearedEvent>) &&
            d.ImplementationType == typeof(PublishTrackerOnPreOpClearedEvHandler));
    }
}
